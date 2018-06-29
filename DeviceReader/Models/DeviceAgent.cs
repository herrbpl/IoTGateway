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
        Starting = 3,
        Running = 4,
        Stopping = 5
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
        

        private DeviceStatus _operationalStatus;
        private CancellationTokenSource _cts;
        private ILogger _logger;
        private Device _device;
        private Func<CancellationToken, Task> _runDelegate;

        public DeviceAgent(ILogger logger, Device device, Func<CancellationToken, Task> runDelegate)
        {
            this._cts = new CancellationTokenSource();
            this._logger = logger;
            this._operationalStatus = DeviceStatus.None;            
            this._device = device;
            this._runDelegate = runDelegate;
            
        }

        public DeviceAgent(ILogger logger, Device device)        
            : this(logger, device, null)
        {

        }
        
        

        public async Task RunAsync()
        {
            
            
            if (this._runDelegate != null)
            {
                if (this._operationalStatus == DeviceStatus.None || this._operationalStatus == DeviceStatus.Stopped)
                {
                    _logger.Info(string.Format("Starting device {0}", this._device.Id), () => { });
                    this._operationalStatus = DeviceStatus.Starting;
                    await this._runDelegate(this._cts.Token);
                }
            }

        }

        public void Stop()
        {
            _logger.Info(string.Format("Stopping device {0}", this._device.Id), () => { });
            this._operationalStatus = DeviceStatus.Stopping;
            this._cts.Cancel();
        }
    }
}
