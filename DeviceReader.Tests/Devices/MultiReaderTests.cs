using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.Models;
using DeviceReader.Parsers;
using DeviceReader.Protocols;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DeviceReader.Tests.Devices
{

    class MockProtocolReader : IProtocolReader
    {
        string _input;
        public MockProtocolReader(string input)
        {
            _input = input;
        }
        public void Dispose()
        {
            return;
        }

        public async Task<string> ReadAsync(CancellationToken cancellationToken)
        {
            return await ReadAsync(null, cancellationToken);
        }

        public async Task<string> ReadAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken)
        {            
            await Task.Delay(new Random().Next(1, 5) * 500, cancellationToken);
            
            return $"{DateTime.Now.ToLongDateString()} - {_input}";
        }
    }

    public class MockParser : IFormatParser<string, Observation>
    {

        public MockParser() {}

        public void Dispose()
        {
            return;
        }

        public int TimeZoneAdjust { get; set; }

        // What to do in case of invalid input? Log and Silently dump message? Or throw?
        public async Task<List<Observation>> ParseAsync(string input, CancellationToken cancellationToken)
        {
            
            var result = new List<Observation>();
            Observation o = new Observation
            {
                DeviceId = "dummy"
                   ,
                GeoPositionPoint = new ObservationLocation
                {
                    X = 24.0,
                    Y = 59.1,
                    Z = 12,
                    Srs = "EPSG:4326"
                },
                Data = new List<ObservationData>
                {
                   new ObservationData {
                        Timestamp = DateTime.Now,
                        TagName = "SomeTag1",
                        Value = input,
                        Unit = "DEGREES",
                        Measure = "AIR_TEMPERATURE"
                    },
                }
            };
            result.Add(o);
            await Task.Delay(new Random().Next(1, 5) * 10, cancellationToken);
            return result;
        }
    }

    public class MultiReaderTests
    {
        private readonly ITestOutputHelper output;
        LoggingConfig lg;
        ILogger logger;

        public MultiReaderTests(ITestOutputHelper output)
        {
            lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;
            output = output;
            Logger.DefaultLoggerFactory.AddProvider(new XUnitLoggerProvider(output));
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);
        }

        [Fact]
        public void MultiReader_TestMultiRead()
        {
            var multireader = new MultiReader<Observation>();

            for (int i = 0; i < 5; i++)
            {
                var reader = new MultiReaderRow<Observation>()
                {
                    FormatParser = new MockParser(),
                    ProtocolReader = new MockProtocolReader($"Test{i}")
                };

                multireader.AddReader<string>($"Test{i}", reader);
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = multireader.ReadAsync(CancellationToken.None).Result;
            stopwatch.Stop();
            logger.Debug($"Call time took {stopwatch.ElapsedMilliseconds} ms", () => { });
            Assert.Equal(5, result.Count);
            foreach (var item in result)
            {
                logger.Debug($"{item.Key} = { JsonConvert.SerializeObject(item.Value,Formatting.Indented) }",()=> { });
            }

        }

        [Fact]
        public void MultiReader_TestMultiRead_Operator()
        {
            var multireader = new MultiReader<Observation>();

            for (int i = 0; i < 5; i++)
            {
                var reader = new MultiReaderRow<Observation>()
                {
                    FormatParser = new MockParser(),
                    ProtocolReader = new MockProtocolReader($"Test{i}")
                };

                multireader.AddReader<string>($"Test{i}", reader);
            }

            MultiReaderRow<Observation>[] x = multireader;
            Assert.Equal(5, x.Length);
        }
    }
}
