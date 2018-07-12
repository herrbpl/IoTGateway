using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Protocols;
using DeviceReader.Devices;

namespace DeviceReader.Models
{

    public interface IDeviceAgentRunner
    {
        void Run(CancellationToken ct);
    }
    public class DefaultDeviceRunner: IDeviceAgentRunner
    {
        ILogger _logger;
        IDevice _device;
        public DefaultDeviceRunner(ILogger logger, IDevice device)
        {
            this._logger = logger;
            this._device = device;
        }
        public void Run(CancellationToken ct)
        {
            if (ct.IsCancellationRequested == true)
            {
                _logger.Info(string.Format("Device {0} runner stopped before it started", _device.Id), () => { });
                ct.ThrowIfCancellationRequested();
            }
            while (true)
            {                
                try
                {
                    if (ct.IsCancellationRequested)
                    {                        
                        _logger.Debug(string.Format("Device {0} runner stop requested.", _device.Id), () => { });
                        //ct.ThrowIfCancellationRequested();                    
                        break;
                    }
                    _logger.Debug(string.Format("Device {0} tick", _device.Id), () => { });
                    Task.Delay(3000, ct).Wait();
                } catch (TaskCanceledException e) { }
                catch (OperationCanceledException e) { }
                catch (AggregateException e) {
                    if (e.InnerException is TaskCanceledException)
                    {

                    }
                    else
                    {
                        _logger.Error(string.Format("Error while stopping: {0}", e), () => { });
                    }
                }
            }
            _logger.Info(string.Format("Device {0} runner stopped", _device.Id), () => { });
        }
       
    }


    public interface IDeviceAgent
    {
        bool IsRunning { get; }
        IDevice Device { get; }
        void Start();
        void Stop();
    }

    class DeviceAgent : IDeviceAgent
    {
        public bool IsRunning { get { return (_executingTask == null? false:
                    !(
                        _executingTask.Status == TaskStatus.Faulted || 
                        _executingTask.Status == TaskStatus.Canceled || 
                        _executingTask.Status == TaskStatus.RanToCompletion ||
                        _executingTask.Status == TaskStatus.Created

                    ) ); } }

        public CancellationTokenSource CancellationTokenSource { get => this._cts;  }

        public IDevice Device => _device;

        private CancellationTokenSource _cts;
        private ILogger _logger;
        private IDevice _device;
        private IDeviceAgentRunner _runner;

        private Task _executingTask;
        
        public DeviceAgent(ILogger logger, IDevice device, IDeviceAgentRunner runner)
        {            
            this._logger = logger;             
            this._device = device;
            this._runner = runner;
        }

        public void Start()
        {
            if (this.IsRunning)
            {
                _logger.Info(string.Format("Device '{0}' already running", _device.Id), () => { });                
            }
            _cts = new CancellationTokenSource();
            _executingTask = Task.Factory.StartNew(() => _runner.Run(_cts.Token), _cts.Token);            
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
                    _logger.Error(string.Format("Error while stopping: {0}", ex.Flatten()), () => { }); 
                }
                
                //_executingTask.Dispose();
                _cts.Dispose();
                _cts = null;
                _executingTask.Dispose();
                _executingTask = null;
            } else
            {
                _logger.Debug(string.Format("Stop requested but device '{0}' is not running", _device.Id), () => { });
            }
        }
    }
}
