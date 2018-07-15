using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;

namespace DeviceReader.Parsers
{
    class DummyParser : IFormatParser<string, string>
    {
        ILogger _logger;
        public DummyParser(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<string> ParseAsync(string input, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new Stopwatch();

            // Begin timing.
            stopwatch.Start();

            try
            {

                await Task.Delay(new Random().Next(1, 5) * 10);
            }
            catch (Exception e) { }
            _logger.Debug(string.Format("DUMMY FORMAT PARSER, Input length: {0}, elapsed time {1}ms", input.Length, stopwatch.ElapsedMilliseconds), () => { });
            stopwatch.Stop();
            return input;
        }
    }
}
