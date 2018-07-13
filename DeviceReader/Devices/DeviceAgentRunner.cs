using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;

namespace DeviceReader.Devices
{
    public interface IDeviceAgentRunner
    {
        void Run(CancellationToken ct);
    }

    public interface IDeviceAgentRunnerFactory
    {
        IDeviceAgentRunner Create(IDeviceAgent agent, IDevice device);
    }

    public class DeviceAgentRunnerFactory : IDeviceAgentRunnerFactory
    {
        ILogger _logger;
        public DeviceAgentRunnerFactory(ILogger logger)
        {
            _logger = logger;
        }

        public IDeviceAgentRunner Create(IDeviceAgent agent, IDevice device)
        {
            return new DefaultDeviceRunner(_logger, agent);
        }
    }

    public class DefaultDeviceRunner : IDeviceAgentRunner
    {
        ILogger _logger;
        IDeviceAgent _deviceagent;

        public DefaultDeviceRunner(ILogger logger, IDeviceAgent deviceagent)
        {
            this._logger = logger;
            this._deviceagent = deviceagent;
        }
        public void Run(CancellationToken ct)
        {
            if (ct.IsCancellationRequested == true)
            {
                _logger.Info(string.Format("Device {0} runner stopped before it started", _deviceagent.Device.Id), () => { });
                ct.ThrowIfCancellationRequested();
            }
            while (true)
            {
                try
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.Debug(string.Format("Device {0} runner stop requested.", _deviceagent.Device.Id), () => { });
                        //ct.ThrowIfCancellationRequested();                    
                        break;
                    }
                    _logger.Debug(string.Format("Device {0} tick", _deviceagent.Device.Id), () => { });
                    Task.Delay(3000, ct).Wait();
                }
                catch (TaskCanceledException e) { }
                catch (OperationCanceledException e) { }
                catch (AggregateException e)
                {
                    if (e.InnerException is TaskCanceledException)
                    {

                    }
                    else
                    {
                        _logger.Error(string.Format("Error while stopping: {0}", e), () => { });
                    }
                }
            }
            _logger.Info(string.Format("Device {0} runner stopped", _deviceagent.Device.Id), () => { });
        }

    }

}
