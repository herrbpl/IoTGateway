using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using DeviceReader.Diagnostics;
using DeviceReader.Devices;
using Newtonsoft.Json;

namespace DeviceReader.Protocols
{
    class HttpProtocolReader : IProtocolReader
    {
        private ILogger _logger;
        
        
        public HttpProtocolReader(ILogger logger)
        {
            _logger = logger;            
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
