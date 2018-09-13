using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using DeviceReader.Diagnostics;
using DeviceReader.Devices;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Protocols
{

    public class HttpProtocolReaderOptions
    {
        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class HttpProtocolReader : AbstractProtocolReader<HttpProtocolReaderOptions>
    {                
        
        public HttpProtocolReader(ILogger logger, string optionspath, IConfigurationRoot configroot) :base(logger, optionspath, configroot)
        {
            if (_options != null)
            {
                Console.WriteLine($"{_options.Url}");
            }
        }

        /* need access to config, port, url, etc. Should give access to config? Device Agent? */
        public async Task<string> ReadAsync(CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new Stopwatch();
            
            // Begin timing.
            stopwatch.Start();

            try
            {

                //await Task.Delay(new Random().Next(1, 10) * 100,cancellationToken);
            } catch (Exception e) { }
            stopwatch.Stop();
            return string.Format("{0} HTTP PROTOCOL READER: {1} in {2} ms ", this.GetType().Namespace + "." + this.GetType().Name  , (string)DateTime.Now.ToLongDateString(), stopwatch.ElapsedMilliseconds);
        }

        public void Dispose()
        {
            _logger.Debug("Dispose called.", () => { });
            return;
        }

        public Task<string> ReadAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult<string>(JsonConvert.SerializeObject(parameters).ToString());
        }
    }
}
