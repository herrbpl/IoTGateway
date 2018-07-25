using System;
using System.Collections.Generic;
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


        //public IDevice Device => _device;

        public Task ExecutingTask => _executingTask;

        //public IDeviceQueue<Observation> Queue => _queue;

        public IRouter Router { get => _router; }

        public string Name { get {               
                return _config.GetValue<string>("name", null);                
            }
        }

        public IConfigurationRoot Configuration { get => _config; }

        private CancellationTokenSource _cts;

        private ILogger _logger;
        //private IDevice _device;        
        //private IDeviceQueue<Observation> _queue;
        private IRouterFactory _routerFactory;
        private IRouter _router;
        private Task _executingTask;

        private IConfigurationRoot _config;
    
        private Dictionary<string, Func<IAgent,IAgentExecutable>> _deviceExecutableFactories;
        //private Func<IDeviceAgent, IDeviceQueue<Observation>> _deviceQueueFactory;

        // Should create agent using config (as opposed to hardconding dependency to IDevice...).

        //public Agent(ILogger logger, IDevice device, IRouterFactory routerFactory, List<Func<IAgent, IDeviceAgentExecutable>> deviceExecutableFactories)
        public Agent(ILogger logger, IConfigurationRoot config, IRouterFactory routerFactory, Dictionary<string,Func<IAgent, IAgentExecutable>>  deviceExecutableFactories)
        {
            this._logger = logger;
            //this._device = device;

            this._config = config ?? throw new ArgumentNullException("config");
            this._deviceExecutableFactories = deviceExecutableFactories ?? throw new ArgumentNullException("deviceExecutableFactories");
            if (this.Name == null) throw new ArgumentException("Config does not specify a name for agent");
            this._routerFactory = routerFactory;            
        }
       
        /// <summary>
        /// Starts agent executables. 
        /// TODO: Add (configurable) executable failure strategy - Should we stop when any of executing tasks fail? Currently, stop all.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.IsRunning)
            {
                _logger.Debug(string.Format("Agent '{0}' already running", this.Name), () => { });
            }
           
            _cts = new CancellationTokenSource();

           
            _executingTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    // for better handling of persitance, maybe in future, use something else instead of name
                    // TODO: should check for duplicate executable names - only singletons allowed.

                    _router = _routerFactory.Create(this.Name);
                    
                    List<Task> tl = new List<Task>();
                    foreach (var factory in _deviceExecutableFactories)
                    {
                        IAgentExecutable executable = null;
                        try
                        {
                            executable = factory.Value(this);
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
                    
                    // If one of executing tasks fail, stop all. Alternative is to restart task or continue with rest of tasks. It may be important when there are dependencies.

                    await Task.WhenAny(tl.ToArray()).ContinueWith((t)=> {
                        _logger.Warn("One executable finished, stopping all", () => { });
                        if (!_cts.IsCancellationRequested)
                        {
                            _cts.Cancel(); 
                        }
                    }
                    
                    );
                    await Task.WhenAll(tl.ToArray());
                } catch (Exception e)
                {
                    if (!(e is OperationCanceledException))
                        _logger.Error(string.Format("{0}",e), ()=> { });
                } finally
                {
                    if (_router != null)
                    {
                        _router.Dispose();
                        _router = null;
                    }
                }
            }
            ,  cancellationToken ).Unwrap();
            return Task.CompletedTask;
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
                    _logger.Debug(string.Format("Agent '{0}' requesting stop", this.Name), () => { });
                    _cts.Cancel();
                    
                }
                catch (AggregateException ex)
                {
                    _logger.Error(string.Format("Error while stopping: {0}", ex), () => { });
                }
                finally
                {
                    await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite,
                                                          cancellationToken));
                   
                }
            }           
        }

        public void Dispose()
        {
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
                _router.Dispose();
                _router = null;
            }


        }
    }
}
