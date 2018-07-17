using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using DeviceReader.Devices;
using DeviceReader.Models;
using Newtonsoft;

namespace DeviceReader.Parsers
{
    class DummyParser : IFormatParser<string, List<Observation>>
    {
        ILogger _logger;
        IDeviceAgent _agent;
        public DummyParser(ILogger logger, IDeviceAgent agent)
        {
            _logger = logger;
            _agent = agent;
        }

        public void Dispose()
        {
            return;
        }

        // What to do in case of invalid input? Log and Silently dump message? Or throw?
        public async Task<List<Observation>> ParseAsync(string input, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new Stopwatch();
            var r = new Random();
            
            // Begin timing.
            stopwatch.Start();
            Observation o = new Observation
            {
                DeviceId = _agent.Device.Id
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
                        Value = r.NextDouble() +r.Next(0,10),
                        Unit = "DEGREES",
                        Measure = "AIR_TEMPERATURE"
                    },
                   new ObservationData {
                        Timestamp = DateTime.Now,
                        TagName = "SomeTag12",
                        Value = (string)input,
                        Unit = "NO_UNIT"
                    },

                }
            };
            try
            {

                await Task.Delay(new Random().Next(1, 5) * 10);
            }
            catch (Exception e) { }
            _logger.Debug(string.Format("DUMMY FORMAT PARSER, Input length: {0}, elapsed time {1}ms", input.Length, stopwatch.ElapsedMilliseconds), () => { });
            stopwatch.Stop();



            return new List<Observation>()
            {
                o
            };
        }
    }
}
