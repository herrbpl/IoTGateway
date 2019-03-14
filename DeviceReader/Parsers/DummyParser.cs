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
    public class DummyParser : IFormatParser<string, Observation>
    {
        ILogger _logger;
        
        public DummyParser(ILogger logger)
        {
            _logger = logger;            
        }

        public void Dispose()
        {
            return;
        }

        public int TimeZoneAdjust { get; set; }

        // What to do in case of invalid input? Log and Silently dump message? Or throw?
        public async Task<List<Observation>> ParseAsync(string input, CancellationToken cancellationToken)
        {
            //Stopwatch stopwatch = new Stopwatch();
            //var r = new Random();
            var result = new List<Observation>();
            // Begin timing.
            //stopwatch.Start();
            if (input != null)
            {
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
                /*
                try
                {

                    //await Task.Delay(new Random().Next(1, 5) * 10);
                    await Task.Delay(0, cancellationToken);
                }
                catch (Exception e) { }
                //_logger.Debug(string.Format("DUMMY FORMAT PARSER, Input length: {0}, elapsed time {1}ms", input.Length, stopwatch.ElapsedMilliseconds), () => { });
                //stopwatch.Stop();
                */
            }


            return result;
        }
    }
}
