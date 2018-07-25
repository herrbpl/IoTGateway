using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace DeviceReader.Protocols
{
    class DummyProtocolReader : IProtocolReader
    {        
      
        //public DummyProtocolReader(ILogger logger, IConfigurationSection config)

        public async Task<string> ReadAsync(CancellationToken cancellationToken)
        {
            var o = new JObject();            
            o["MeasurementType"] = "Temperature";
            o["MeasurementName"] = "AirTemperature";
            o["Value"] = 12.43;
            o["MeasurementUnit"] = "C";
            await Task.Delay(100, cancellationToken);
            return o.ToString();
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
