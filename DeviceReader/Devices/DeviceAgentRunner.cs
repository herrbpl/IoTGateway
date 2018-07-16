using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Protocols;
using DeviceReader.Parsers;

namespace DeviceReader.Devices
{    
    public interface IDeviceAgentRunner
    {
        void Run(CancellationToken ct);
        Task RunAsync(CancellationToken ct);
    }
    
    public class DefaultDeviceRunner : IDeviceAgentRunner
    {
        ILogger _logger;
        IDeviceAgent _deviceagent;
        IProtocolReaderFactory _protocolReaderFactory;
        IFormatParserFactory<string, string> _formatParserFactory;
        public DefaultDeviceRunner(ILogger logger, IDeviceAgent deviceagent, IProtocolReaderFactory protocolReaderFactory, IFormatParserFactory<string,string> formatParserFactory)
        {
            this._logger = logger;
            this._deviceagent = deviceagent;
            this._protocolReaderFactory = protocolReaderFactory;
            this._formatParserFactory = formatParserFactory;
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
                IProtocolReader _protocolReader = _protocolReaderFactory.GetProtocolReader(_deviceagent);
                IFormatParser<string, string> _parser = _formatParserFactory.GetFormatParser(_deviceagent);
                try
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.Debug(string.Format("Device {0} runner stop requested.", _deviceagent.Device.Id), () => { });
                        //ct.ThrowIfCancellationRequested();                    
                        break;
                    }
                    _logger.Debug(string.Format("Device {0} tick", _deviceagent.Device.Id), () => { });
                    
                    var result = _protocolReader.ReadAsync(ct).Result;
                    result =  _parser.ParseAsync(result, ct).Result;

                    _logger.Info(string.Format("Device {0} reads: {1}", _deviceagent.Device.Id, result), () => { });

                   
                    Task.Delay(_deviceagent.Device.Config.PollFrequency, ct).Wait();
                    
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
                finally
                {
                    _protocolReader.Dispose();
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

                IProtocolReader _protocolReader = _protocolReaderFactory.GetProtocolReader(_deviceagent);
                IFormatParser<string, string> _parser = _formatParserFactory.GetFormatParser(_deviceagent);
                try
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.Debug(string.Format("Device {0} runner stop requested.", _deviceagent.Device.Id), () => { });
                        
                        break;
                    }
                    _logger.Debug(string.Format("Device {0} tick", _deviceagent.Device.Id), () => { });


                    var result = await _protocolReader.ReadAsync(ct);
                    result = await _parser.ParseAsync(result, ct);
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
                finally
                {
                    _protocolReader.Dispose();
                }
            }
            _logger.Debug(string.Format("Device {0} runner stopped", _deviceagent.Device.Id), () => { });
        }
    }

}
