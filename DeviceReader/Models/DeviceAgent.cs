using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;

namespace DeviceReader.Models
{
    public enum DeviceStatus
    {
        None = 1,
        Stopped = 2,       
        Running = 3
        
    }

    public interface IDeviceAgent
    {
        DeviceStatus OperationalStatus { get; }
        Task RunAsync();
        void Stop();
    }
    class DeviceAgent : IDeviceAgent
    {
        public DeviceStatus OperationalStatus => _operationalStatus;
        public CancellationTokenSource CancellationTokenSource { get => this._cts;  } 

        private DeviceStatus _operationalStatus;
        private CancellationTokenSource _cts;
        private ILogger _logger;
        private Device _device;
        private Func<DeviceAgent, Task> _runDelegate;

        public DeviceAgent(ILogger logger, Device device, Func<DeviceAgent, Task> runDelegate)
        {
            this._cts = new CancellationTokenSource();
            this._logger = logger;
            this._operationalStatus = DeviceStatus.None;            
            this._device = device;
            if (runDelegate != null)
            {
                this._runDelegate = runDelegate;
            } else
            {
                this._runDelegate = DefaultRunner;


            }
            
        }

        public DeviceAgent(ILogger logger, Device device)        
            : this(logger, device, null)
        {

        }
        
        protected async Task DefaultRunner(DeviceAgent agent)
        {
            while (!agent.CancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    // Check config change
                    // execute protocol reader
                    // execute parser/formatter
                    // execute filter
                    // post to output/iot Hub
                    await Task.Delay(1000, agent.CancellationTokenSource.Token);
                } catch (OperationCanceledException ex)
                {
                    _logger.Info("Cancellation requested!", () => { });
                }
            }
        }

        public async Task RunAsync()
        {                        
            if (this._runDelegate != null)
            {
                if (this._operationalStatus == DeviceStatus.None || this._operationalStatus == DeviceStatus.Stopped)
                {
                    _logger.Info(string.Format("Starting device {0}", this._device.Id), () => { });
                    this._operationalStatus = DeviceStatus.Running;
                    await this._runDelegate(this);                    
                }
            }

        }

        public void Stop()
        {
            _logger.Info(string.Format("Stopping device {0}", this._device.Id), () => { });
            this._operationalStatus = DeviceStatus.Stopped;
            this._cts.Cancel();            
        }
    }
}
