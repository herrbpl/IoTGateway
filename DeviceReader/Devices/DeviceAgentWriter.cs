using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Protocols;
using DeviceReader.Parsers;
using DeviceReader.Models;


namespace DeviceReader.Devices
{
    class DeviceAgentWriter : IDeviceAgentExecutable
    {
        ILogger _logger;
        IDeviceAgent _deviceagent;

        public DeviceAgentWriter(ILogger logger, IDeviceAgent deviceagent)
        {
            this._logger = logger;
            this._deviceagent = deviceagent;            
        }
        public async Task RunAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested == true)
            {
                _logger.Warn(string.Format("Device {0} Writer stopped before it started", _deviceagent.Device.Id), () => { });
                ct.ThrowIfCancellationRequested();
            }
            while (true)
            {                
                try
                {
                    if (ct.IsCancellationRequested)
                    {
                        _logger.Debug(string.Format("Device {0} Writer stop requested.", _deviceagent.Device.Id), () => { });

                        break;
                    }
                    _logger.Debug(string.Format("Device {0} Writer tick", _deviceagent.Device.Id), () => { });                    

                    await Task.Delay(1000, ct);
                    throw new Exception("TestException");

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
                } catch( Exception e)
                {
                    _logger.Error(string.Format("Error while running: {0}", e), () => { });
                    //throw e;
                    break;
                }
               
            }
            _logger.Debug(string.Format("Device {0} Writer stopped", _deviceagent.Device.Id), () => { });
        }
    
    }
}
