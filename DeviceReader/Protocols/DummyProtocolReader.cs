using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using DeviceReader.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Globalization;

namespace DeviceReader.Protocols
{
    class DummyProtocolReader : IProtocolReader
    {

        
        private long messageno = 0;
        private IConfiguration _config;
        private string devicename;
        private int maxmessages;
        Stopwatch stopwatch;
        public DummyProtocolReader(ILogger logger, IConfiguration config)
        {
            _config = config;
            devicename = _config.GetValue<string>("name", "notfound");
            maxmessages = _config.GetValue<int>("executables:reader:maxmessages", 1000);
            Stopwatch stopwatch = new Stopwatch();
        }

        public async Task<string> ReadAsync(CancellationToken cancellationToken)
        {

            /*var o = new JObject();            
            o["MeasurementType"] = "Temperature";
            o["MeasurementName"] = "AirTemperature";
            o["Value"] = 12.43;
            o["MeasurementUnit"] = "C";
            //await Task.Delay(100, cancellationToken);
            */
            //await Task.Delay(0, cancellationToken);
            string result = null;
            if (messageno < maxmessages)
            {
                result = devicename + ":" + messageno.ToString() + ":" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                messageno++;
            }            
            return result;
        }

        public void Dispose()
        {
            return;
        }

        public Task<string> ReadAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult<string>(JsonConvert.SerializeObject(parameters).ToString());
        }
    }
    
}
