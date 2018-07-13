using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DeviceReader.Protocols
{
    class DummyProtocolReader : IProtocolReader
    {

        public async Task<string> ReadAsync(CancellationToken cancellationToken)
        {
            var o = new JObject();
            Console.WriteLine("Protocol Reader executing");
            o["MeasurementType"] = "Temperature";
            o["MeasurementName"] = "AirTemperature";
            o["Value"] = 12.43;
            o["MeasurementUnit"] = "C";
            await Task.Delay(100, cancellationToken);
            return o.ToString();
        }
    }
    
}
