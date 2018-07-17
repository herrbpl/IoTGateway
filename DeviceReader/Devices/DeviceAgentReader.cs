using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Protocols;
using DeviceReader.Parsers;
using DeviceReader.Models;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DeviceReader.Devices
{    
    
    /// <summary>
    /// TODO: Add output storage, save run timestamp, calculate parameters for fetch (TimeBegin, TimeEnd). // should we get history data too or just current/latest data point..?
    /// I think initially we only poll current data. No need to get historical (unless it is explicitly demanded)
    /// TODO: try/catch for protocol/Iformatparser creation
    /// XXX: Should device client run even when there is no agent running?
    /// </summary>
    public class DeviceAgentReader : IDeviceAgentExecutable
    {
        ILogger _logger;
        IDeviceAgent _deviceagent;
        IProtocolReaderFactory _protocolReaderFactory;
        IFormatParserFactory<string, List<Observation>> _formatParserFactory;
        public DeviceAgentReader(ILogger logger, IDeviceAgent deviceagent, IProtocolReaderFactory protocolReaderFactory, IFormatParserFactory<string, List<Observation>> formatParserFactory)
        {
            this._logger = logger;
            this._deviceagent = deviceagent;
            this._protocolReaderFactory = protocolReaderFactory;
            this._formatParserFactory = formatParserFactory;            
        }
        

        public async Task RunAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested == true)
            {
                _logger.Warn(string.Format("Device {0} Reader stopped before it started", _deviceagent.Device.Id), () => { });
                ct.ThrowIfCancellationRequested();
            }
            while (true)
            {

                IProtocolReader _protocolReader = _protocolReaderFactory.GetProtocolReader(_deviceagent);
                IFormatParser<string, List<Observation>> _parser = _formatParserFactory.GetFormatParser(_deviceagent);
                try
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.Debug(string.Format("Device {0} Reader stop requested.", _deviceagent.Device.Id), () => { });
                        
                        break;
                    }
                    _logger.Debug(string.Format("Device {0} tick", _deviceagent.Device.Id), () => { });


                    var result = await _protocolReader.ReadAsync(ct);
                    

                    var listResults = await _parser.ParseAsync(result, ct);
                    string res = JsonConvert.SerializeObject(listResults, Formatting.Indented, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                    _logger.Info(string.Format("Device {0} reads: {1}", _deviceagent.Device.Id, res), () => { });                    

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
                } catch (Exception e)
                {
                    _logger.Error(string.Format("Error while running: {0}", e), () => { });
                }

                finally
                {
                    _protocolReader.Dispose();
                }
            }
            _logger.Debug(string.Format("Device {0} Reader stopped", _deviceagent.Device.Id), () => { });
        }
    }

}
