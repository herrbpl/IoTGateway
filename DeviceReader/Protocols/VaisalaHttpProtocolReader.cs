using DeviceReader.Diagnostics;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Protocols
{
    public class VaisalaHttpProtocolReaderOptions: HttpProtocolReaderOptions
    {        
        /// <summary>
        /// How much to adjust time different to UTC, in minutes
        /// </summary>
        public int TimeZoneAdjust { get; set; } = 0;
    }

    class VaisalaHttpProtocolReader: HttpProtocolReader
    {
        //protected  VaisalaHttpProtocolReaderOptions _options;
        protected VaisalaHttpProtocolReaderOptions Options { get => (VaisalaHttpProtocolReaderOptions)_options; }

        public VaisalaHttpProtocolReader(ILogger logger, string optionspath, IConfiguration configroot) : 
            base(logger, optionspath, configroot) {
        }

        /// <summary>
        /// Overload to specify options type we need
        /// </summary>
        /// <param name="optionspath"></param>
        public override void LoadOptions(string optionspath)
        {
            _options = LoadOptions<VaisalaHttpProtocolReaderOptions>(_configroot, optionspath);
        }

        override public async Task<string> ReadAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            if (parameters == null)
                parameters = new Dictionary<string, string>();


            if (parameters.ContainsKey("returnHierarchy")) parameters.Remove("returnHierarchy");
            if (parameters.ContainsKey("start")) parameters.Remove("start");
            if (parameters.ContainsKey("stop")) parameters.Remove("stop");

            // Add timespan 1 hour, with stop = now and start = now - 1h
            DateTime dt = DateTime.UtcNow;
            // is Vaisala using UTC or local time zone?
            // if poller timezone is different from RWS, then this also needs to be considered.
            
            if (Options.TimeZoneAdjust != 0)
            {
                dt = dt.AddMinutes(Options.TimeZoneAdjust);
            }
            
            parameters.Add("start", dt.AddHours(-1).ToString("s"));
            parameters.Add("stop", dt.ToString("s"));
            parameters.Add("returnHierarchy", "true");

            var result = await base.ReadAsync(parameters, cancellationToken);
            return result;
        }
    }
}
