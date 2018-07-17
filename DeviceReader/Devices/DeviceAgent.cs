using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Protocols;
using DeviceReader.Devices;
using DeviceReader.Models;

namespace DeviceReader.Devices
{
    /// <summary>
    /// Device Agent - starts and stops agent runtime. 
    /// <para>
    /// XXX: Should agent provide method directly for upstream messages or not? In any case, each agent should have its own identity for IoT Hub. 
    /// 
    ///     Alternative is to provide interface for message passing to output. 
    ///     IoTHub config is related to device, IoT runtime client to runner or agent?
    ///     Device connection/writer should be created when agent is created?
    ///     Perhaps this:
    ///     DeviceAgent
    ///         DeviceAgentReaderRunner - reads and writes to buffer/queue
    ///         DeviceAgentWriterRunner - connects to output, reads from queue, writes to output
    ///         DeviceQueue - data is written by readerrunner, read by writerrunner. Queue is created on start and removed on stop
    /// </para>
    /// <para>
    /// XXX: Is there need to start and stop reader and writer separately?    
    /// </para>
    /// </summary>        

    public interface IDeviceAgent
    {
        bool IsRunning { get; }
        IDevice Device { get; }
        IDeviceQueue<Observation> Queue { get; }
        // Required temporarily for waiting on all device agents. I think it should go to device manager where there would be await StopAll
        Task ExecutingTask { get; }              
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

        public IDeviceQueue<Observation> Queue => _queue;

        private CancellationTokenSource _cts;

        private ILogger _logger;
        private IDevice _device;
        private IDeviceAgentExecutable _readrunner;
        private IDeviceQueue<Observation> _queue; // later perhaps replace with internal messaging bus? When more complicated internals of agent is required..

        private Task _executingTask;

        // delegate for creating agent runner.. http://autofaccn.readthedocs.io/en/latest/resolve/relationships.html
        //Func<IDeviceAgent, IDeviceAgentExecutable> _deviceReaderFactory;
        //Func<IDeviceAgent, IDeviceAgentExecutable> _deviceWriterFactory;

        List<Func<IDeviceAgent,IDeviceAgentExecutable>> _deviceExecutableFactories;

        public DeviceAgent(ILogger logger, IDevice device, List<Func<IDeviceAgent, IDeviceAgentExecutable>>  deviceExecutableFactories)
        {
            this._logger = logger;
            this._device = device;
            if (deviceExecutableFactories == null) throw new ArgumentNullException("deviceExecutableFactories");
            this._deviceExecutableFactories = deviceExecutableFactories;
        }

        public DeviceAgent(ILogger logger, IDevice device, Func<IDeviceAgent, IDeviceAgentExecutable> deviceReaderFactory)
        {
            this._logger = logger;
            this._device = device;
            if (deviceReaderFactory == null) throw new ArgumentNullException("createAgentRunner");
            this._deviceExecutableFactories = new List<Func<IDeviceAgent, IDeviceAgentExecutable>>();
            this._deviceExecutableFactories.Add(deviceReaderFactory);
            //this._deviceReaderFactory = deviceReaderFactory;
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
                _logger.Debug(string.Format("Device '{0}' already running", _device.Id), () => { });
            }
            //_runner = _runnerFactory.Create(this, _device); // maybe not needed (set and forget?)
            //_readrunner = _deviceReaderFactory(this);
            _cts = new CancellationTokenSource();
            //_executingTask = Task.Factory.StartNew(() => _runner.Run(_cts.Token), cancellationToken);
            _executingTask = Task.Factory.StartNew(async () =>
            {
                try
                {
                    List<Task> tl = new List<Task>();
                    foreach (var factory in _deviceExecutableFactories)
                    {
                        var executable = factory(this);
                        tl.Add(executable.RunAsync(_cts.Token));
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
                    _logger.Error(string.Format("{0}",e), ()=> { });
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
                _logger.Debug(string.Format("Stop requested but device '{0}' is not running", _device.Id), () => { });
                return;
            }

            if (IsRunning && _cts != null && !_cts.IsCancellationRequested)
            {
                try
                {
                    _logger.Debug(string.Format("Device '{0}' requesting stop", _device.Id), () => { });
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
            

        }
    }
}
