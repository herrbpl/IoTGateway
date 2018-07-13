using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DeviceReader.Protocols
{
    class HttpProtocolReader : IProtocolReader
    {
        public async Task<string> ReadAsync(CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new Stopwatch();

            // Begin timing.
            stopwatch.Start();

            try
            {

                await Task.Delay(new Random().Next(1, 10) * 100);
            } catch (Exception e) { }
            stopwatch.Stop();
            return string.Format("HTTP PROTOCOL READER: {0} in {1} ms ", (string)DateTime.Now.ToLongDateString(), stopwatch.ElapsedMilliseconds);
        }
    }
}
