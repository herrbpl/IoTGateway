using DeviceReader.Diagnostics;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Protocols
{
    class VaisalaHttpProtocolReader: HttpProtocolReader
    {
        public VaisalaHttpProtocolReader(ILogger logger, string optionspath, IConfiguration configroot) : base(logger, optionspath, configroot) { }

        override public async Task<string> ReadAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            if (parameters == null)
                parameters = new Dictionary<string, string>();


            if (parameters.ContainsKey("returnHierarchy")) parameters.Remove("returnHierarchy");
            if (parameters.ContainsKey("start")) parameters.Remove("start");
            if (parameters.ContainsKey("stop")) parameters.Remove("stop");

            // Add timespan 1 hour, with stop = now and start = now - 1h
            // is Vaisala using UTC or local time zone?
            DateTime dt = DateTime.Now;
            parameters.Add("start", dt.AddHours(-1).ToString("s"));
            parameters.Add("stop", dt.ToString("s"));
            parameters.Add("returnHierarchy", "true");

            var result = await base.ReadAsync(parameters, cancellationToken);
            return result;
        }
    }
}
