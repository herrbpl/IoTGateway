using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Protocols;
using DeviceReader.Devices;

namespace DeviceReader.Devices
{
    public interface IDeviceAgent
    {
        bool IsRunning { get; }
        IDevice Device { get; }        
        Task ExecutingTask { get; }
        void Start();
        void Stop();
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
        
    }


    // https://blogs.msdn.microsoft.com/cesardelatorre/2017/11/18/implementing-background-tasks-in-microservices-with-ihostedservice-and-the-backgroundservice-class-net-core-2-x/
    // https://blogs.msdn.microsoft.com/pfxteam/2011/10/24/task-run-vs-task-factory-startnew/

    class DeviceAgent : IDeviceAgent, IDisposable
    {
        public bool IsRunning { get { return (_executingTask == null? false:
                    !(
                        _executingTask.Status == TaskStatus.Faulted || 
                        _executingTask.Status == TaskStatus.Canceled || 
                        _executingTask.Status == TaskStatus.RanToCompletion ||
                        _executingTask.Status == TaskStatus.Created

                    ) ); } }
        

        public IDevice Device => _device;

        public Task ExecutingTask => _executingTask;

        private CancellationTokenSource _cts;
        private ILogger _logger;
        private IDevice _device;
        private IDeviceAgentRunner _runner;
        private IDeviceAgentRunnerFactory _runnerFactory;

        private Task _executingTask;
        
        public DeviceAgent(ILogger logger, IDevice device, IDeviceAgentRunnerFactory runnerFactory)
        {            
            this._logger = logger;             
            this._device = device;
            this._runnerFactory = runnerFactory;
        }

        public void Start()
        {
            if (this.IsRunning)
            {
                _logger.Debug(string.Format("Device '{0}' already running", _device.Id), () => { });                
            }
            _runner = _runnerFactory.Create(this, _device); // maybe not needed (set and forget?)
            _cts = new CancellationTokenSource();
            _executingTask = Task.Factory.StartNew(() => _runner.Run(_cts.Token), CancellationToken.None);            
        }
       
        public void Stop()
        {
            if (IsRunning && _cts != null && !_cts.IsCancellationRequested)
            {
                try
                {
                    _logger.Debug(string.Format("Device '{0}' requesting stop", _device.Id), () => { });
                    _cts.Cancel();
                    // Not sure if this is neccessary
                    _executingTask.Wait();
                } catch(AggregateException ex)
                {
                    _logger.Error(string.Format("Error while stopping: {0}", ex), () => { }); 
                }
                                
                _cts.Dispose();
                _cts = null;
                _executingTask.Dispose();
                _executingTask = null;
                _runner = null;
            } else
            {
                _logger.Debug(string.Format("Stop requested but device '{0}' is not running", _device.Id), () => { });
            }
        }


        public  Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.IsRunning)
            {
                _logger.Debug(string.Format("Device '{0}' already running", _device.Id), () => { });
            }
            _runner = _runnerFactory.Create(this, _device); // maybe not needed (set and forget?)
            _cts = new CancellationTokenSource();
            //_executingTask = Task.Factory.StartNew(() => _runner.Run(_cts.Token), cancellationToken);
            _executingTask = Task.Factory.StartNew(async () => await _runner.RunAsync(_cts.Token), cancellationToken).Unwrap();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {

            // Stop called without start
            if (_executingTask == null)
            {
                _logger.Debug(string.Format("Stop requested but device '{0}' is not running", _device.Id), () => { });
                return;
            }

            if (IsRunning && _cts != null && !_cts.IsCancellationRequested)
            {
                try
                {
                    _logger.Debug(string.Format("Device '{0}' requesting stop", _device.Id), () => { });
                    _cts.Cancel();
                    // Not sure if this is neccessary
                    //_executingTask.Wait();
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
                _runner = null;
            }
            

        }
    }
}
