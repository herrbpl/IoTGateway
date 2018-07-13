using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Protocols;

namespace DeviceReader.Devices
{
    //public delegate Task AgentRunnerDelegate(CancellationToken ct);

    public interface IDeviceAgentRunner
    {
        void Run(CancellationToken ct);
        Task RunAsync(CancellationToken ct);
    }

    public interface IDeviceAgentRunnerFactory
    {
        IDeviceAgentRunner Create(IDeviceAgent agent, IDevice device);
    }

    public class DeviceAgentRunnerFactory : IDeviceAgentRunnerFactory
    {
        ILogger _logger;
        IProtocolReaderFactory _protocolReaderFactory;
        public DeviceAgentRunnerFactory(ILogger logger, IProtocolReaderFactory protocolReaderFactory)
        {
            _logger = logger;
            _protocolReaderFactory = protocolReaderFactory;
        }
        /// <summary>
        /// Creates runner according to device config.
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public IDeviceAgentRunner Create(IDeviceAgent agent, IDevice device)
        {
            // create/use objects for runner based on device config
            // protocol reader
            // format parser
            // output client <- tricky, might need registering with IoT Hub. Expectation is that device is already registered. 
            // or should client go to agent ? or client itself ? In case we have push client, then agent is not needed, only list of clients...
            
            _logger.Debug(string.Format("Creating new runner for {0}", device.Id), () => { });

            return new DefaultDeviceRunner(_logger, agent, _protocolReaderFactory.GetProtocolReader(device.Config.Protocol));
        }

        
    }

    public class DefaultDeviceRunner : IDeviceAgentRunner
    {
        ILogger _logger;
        IDeviceAgent _deviceagent;
        IProtocolReader _protocolReader;
        public DefaultDeviceRunner(ILogger logger, IDeviceAgent deviceagent, IProtocolReader protocolReader)
        {
            this._logger = logger;
            this._deviceagent = deviceagent;
            this._protocolReader = protocolReader;
        }
        public void Run(CancellationToken ct)
        {
            if (ct.IsCancellationRequested == true)
            {
                _logger.Warn(string.Format("Device {0} runner stopped before it started", _deviceagent.Device.Id), () => { });
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
            _logger.Debug(string.Format("Device {0} runner stopped", _deviceagent.Device.Id), () => { });
        }

        public async Task RunAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested == true)
            {
                _logger.Warn(string.Format("Device {0} runner stopped before it started", _deviceagent.Device.Id), () => { });
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
                    //await Task.Delay(3000, ct);
                    var result = await _protocolReader.ReadAsync(ct);
                    _logger.Info(string.Format("Device {0} reads: {1}", _deviceagent.Device.Id, result), () => { });
                    await Task.Delay(_deviceagent.Device.Config.PollFrequency * 1000, ct);
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
            _logger.Debug(string.Format("Device {0} runner stopped", _deviceagent.Device.Id), () => { });
        }
    }

}
