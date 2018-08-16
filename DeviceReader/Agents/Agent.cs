using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Router;
using Microsoft.Extensions.Configuration;


namespace DeviceReader.Agents
{

    // https://blogs.msdn.microsoft.com/cesardelatorre/2017/11/18/implementing-background-tasks-in-microservices-with-ihostedservice-and-the-backgroundservice-class-net-core-2-x/
    // https://blogs.msdn.microsoft.com/pfxteam/2011/10/24/task-run-vs-task-factory-startnew/


    class Agent : IAgent, IDisposable
    {
        public bool IsRunning { get { return (_executingTask == null ? false :
                    !(
                        _executingTask.Status == TaskStatus.Faulted ||
                        _executingTask.Status == TaskStatus.Canceled ||
                        _executingTask.Status == TaskStatus.RanToCompletion ||
                        _executingTask.Status == TaskStatus.Created

                    )); } }

        

        public Task ExecutingTask => _executingTask;

        public AgentStatus Status { get => _agentStatus; set => OnStatusChange(value, null); }

        public IRouter Router { get => _router; }

        public string Name { get {               
                return _config.GetValue<string>("name", null);                
            }
        }

        public IConfigurationRoot Configuration { get => _config; }

        private CancellationTokenSource _cts;

        private ILogger _logger;        
        //private IRouterFactory _routerFactory;
        private IRouter _router;
        private Task _executingTask;
        private AgentStatus _agentStatus;

        private ConcurrentDictionary<string, IAgentExecutable> _executables;


        private Stopwatch _sw;
        public long StoppingTime { get; set; }

        public DateTime StopStartTime { get; set; }

        public DateTime StopStopTime { get; set; }

        

        private IConfigurationRoot _config;
    
        private Dictionary<string, Func<IAgent,IAgentExecutable>> _deviceExecutableFactories;

        private AgentStatusChangeEvent<AgentStatus> _onStatusChange;

        //public Agent(ILogger logger, IConfigurationRoot config, IRouterFactory routerFactory, Dictionary<string, Func<IAgent, IAgentExecutable>> deviceExecutableFactories)
        public Agent(ILogger logger, IConfigurationRoot config, IRouter router, Dictionary<string,Func<IAgent, IAgentExecutable>>  deviceExecutableFactories)
        {
            this._logger = logger;            

            this._config = config ?? throw new ArgumentNullException("config");
            this._deviceExecutableFactories = deviceExecutableFactories ?? throw new ArgumentNullException("deviceExecutableFactories");
            if (this.Name == null) throw new ArgumentException("Config does not specify a name for agent");
            //this._routerFactory = routerFactory;
            this._router = router;
            _executables = new ConcurrentDictionary<string, IAgentExecutable>();
            _agentStatus = AgentStatus.Stopped;
            _sw = new Stopwatch();
        }
       
        /// <summary>
        /// Starts agent executables. 
        /// TODO: Add (configurable) executable failure strategy - Should we stop when any of executing tasks fail? Currently, stop all.
        /// TODO: Find a way to signal moment when all agents are created and do not start tasks until all tasks have been created. Alternative is to create executables in write reverse order (output first)
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public  Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.IsRunning)
            {
                _logger.Debug(string.Format("Agent '{0}' already running", this.Name), () => { });
            }

            //_cts = new CancellationTokenSource();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _cts.Token.Register(async () => {
                //await OnStatusChange(AgentStatus.Stopping, null);
                Status = AgentStatus.Stopping;
                _sw.Restart();
                StopStartTime = DateTime.Now;
            });

            //OnStatusChange(AgentStatus.Starting, null);
            Status = AgentStatus.Starting;

            _executingTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    // for better handling of persitance, maybe in future, use something else instead of name

                    //_router = _routerFactory.Create(this.Name);
                    _router.Clear();
                    
                    List<Task> tl = new List<Task>();
                    foreach (var factory in _deviceExecutableFactories)
                    {
                        IAgentExecutable executable = null;
                        try
                        {
                            executable = factory.Value(this);
                            _executables.GetOrAdd(executable.Name, executable);
                        }
                        catch (Exception e)
                        {

                            _logger.Error(string.Format("Agent {0}: Created executable '{1}' failed:{2}", this.Name, factory.Key, e), () => { });
                            if (!_cts.IsCancellationRequested)
                            {
                                _cts.Cancel();
                            }
                        }

                        if (executable != null)
                        {
                            _router.AddQueue(executable.Name);

                            _logger.Debug(string.Format("Agent {0}: Created executable '{1}'", this.Name, executable.Name), () => { });
                            tl.Add(executable.RunAsync(_cts.Token));
                        }
                        
                    }

                    //await OnStatusChange(AgentStatus.Running, null);
                    Status = AgentStatus.Running;
                    // If one of executing tasks fail, stop all. Alternative is to restart task or continue with rest of tasks. It may be important when there are dependencies.


                    await
                        Task.WhenAny(tl.ToArray()).ContinueWith((t) =>
                        {
                            if (!_cts.IsCancellationRequested)
                            {
                                _logger.Info("One executable finished, stopping all", () => { });
                                _cts.Cancel();
                            }
                        }
                        );
                    /*
                    .ContinueWith((t) =>
                    {
                        Task.WaitAll(tl.ToArray());
                    }).ContinueWith((t) => {
                        _logger.Debug("Cleaning up _executables - in startasync", () => { });
                        foreach (var item in _executables) 
                        {
                            try
                            {
                                item.Value.Dispose();
                            } catch (Exception e) { }
                        }
                        _executables.Clear();

                    });
                    
                    */
                    await Task.WhenAll(tl.ToArray());
                    this.StoppingTime = _sw.ElapsedMilliseconds;
                    StopStopTime = DateTime.Now;
                    _sw.Stop();
                } catch (Exception e)
                {
                    if (!(e is OperationCanceledException))
                        _logger.Error(string.Format("{0}",e), ()=> { });
                } finally
                {
                    _router.Clear();
                    if (_executables.Count > 0)
                    {
                        _logger.Debug("Cleaning up _executables - in startasync", () => { });
                        foreach (var item in _executables)
                        {
                            try
                            {
                                item.Value.Dispose();
                            }
                            catch (Exception e) { }
                        }
                        _executables.Clear();
                    }
                    //await OnStatusChange(AgentStatus.Stopped, null);
                    Status = AgentStatus.Stopped;
                }
            }
            ,  cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default) .Unwrap();
            return Task.CompletedTask;
            //return;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {

            // Stop called without start
            if (_executingTask == null)
            {
                _logger.Debug(string.Format("Stop requested but agent '{0}' is not running", this.Name), () => { });
                return;
            }

            if (IsRunning && _cts != null && !_cts.IsCancellationRequested)
            {
                try
                {
                    _logger.Info(string.Format("Agent '{0}' requesting stop", this.Name), () => { });
                     await Task.Run( () => { _cts.Cancel(); }, cancellationToken) ;
                    
                }
                catch (AggregateException ex)
                {
                    _logger.Error(string.Format("Error while stopping: {0}", ex), () => { });
                }
                /*
                finally
                {

                    //await _executingTask;
                    _router.Clear();
                    _logger.Debug("Cleaning up _executables - in stopasync", () => { });
                    foreach (var item in _executables)
                    {
                        try
                        {
                            item.Value.Dispose();
                        }
                        catch (Exception e) { }
                    }
                    _executables.Clear();

                }*/
            }           
        }

        public void Dispose()
        {
            _logger.Debug($"Disposing agent {Name}", () => { });
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
            if (_executingTask != null && IsRunning)
            {
                if (IsRunning) _executingTask.Wait();
                _executingTask.Dispose();
                _executingTask = null;                
            }
            
            if (_router != null)
            {
                _router.Clear();
                _router.Dispose();
                _router = null;
            }
            if (_executables.Count > 0)
            {
                _logger.Debug("Cleaning up _executables - in agent dispose", () => { });
                foreach (var item in _executables)
                {
                    try
                    {
                        item.Value.Dispose();
                    }
                    catch (Exception e) { }
                }
                _executables.Clear();
            }
        }



        private async Task OnStatusChange(AgentStatus status, object context)
        {
            _agentStatus = status;

            if (_onStatusChange != null)
            {
                await Task.Run(() => { _onStatusChange(status, context); });
            }
        }

        public void SetAgentStatusHandler(AgentStatusChangeEvent<AgentStatus> onstatuschange)
        {
            _onStatusChange = onstatuschange;
        }
    }
}
