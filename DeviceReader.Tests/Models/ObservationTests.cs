using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using DeviceReader.Models;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.IO;
using DeviceReader.Abstractions;
using System.Diagnostics;

namespace DeviceReader.Tests.Models
{
    public class ObservationTests
    {
        private readonly ITestOutputHelper _output;
        public ObservationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private Observation GetObservation()
        {
            return new Observation
            {
                DeviceId = "test",
                Data = new List<ObservationData>
                {
                   new ObservationData {
                      TagName = "first",
                      Value = 1
                   },
                   new ObservationData {
                      TagName = "second",
                      Value = 2
                   },
                   new ObservationData {
                      TagName = "third",
                      Value = "c"
                   }
                }
            };
        }

        [Fact]
        public void TestObservation_TestMerge()
        {
            var o1 = GetObservation();
            var o2 = GetObservation();
            Observation o3;

            // regular merge - o1 and o2 are not changed
            o1.Merge(o2, new Abstractions.MergeOptions { MergeConflicAction = Abstractions.MergeConflicAction.KeepFirst, PrefixFirst = "", PrefixSecond = "o2" });
            Assert.Equal(3, o1.Data.Count);
            Assert.Equal(3, o2.Data.Count);

            // regular merge
            o3 = o1.Merge(o2, new Abstractions.MergeOptions { MergeConflicAction = Abstractions.MergeConflicAction.KeepFirst, PrefixFirst = "", PrefixSecond = "o2" });
            Assert.Equal(6, o3.Data.Count);

            // reinitialize
            o1 = GetObservation();
            o2 = GetObservation();

            // no change as there is name conflict
            o1.Merge(o2, new Abstractions.MergeOptions { MergeConflicAction = Abstractions.MergeConflicAction.KeepFirst, PrefixFirst = "", PrefixSecond = "" });
            Assert.Equal(3, o1.Data.Count);
            Assert.Equal(3, o2.Data.Count);

            o1 = GetObservation();
            o2 = GetObservation();

            // second value should be used
            o2.Data[0].Value = -100;
            o3 = o1.Merge(o2, new Abstractions.MergeOptions { MergeConflicAction = Abstractions.MergeConflicAction.KeepSecond, PrefixFirst = "", PrefixSecond = "" });
            Assert.Equal(3, o1.Data.Count);
            Assert.Equal(3, o2.Data.Count);
            Assert.Equal(3, o3.Data.Count);
            Assert.Equal(-100, o3.Data[0].Value);

            // test if throws on name conflict
            Assert.Throws<MergeConflictException>(
                () =>
                {
                    o3 = o1.Merge(o2,
                new Abstractions.MergeOptions { MergeConflicAction = Abstractions.MergeConflicAction.Throw, PrefixFirst = "", PrefixSecond = "" });
                });

            // test if observations have diffent length of params
            o1 = GetObservation();
            o2 = GetObservation();
            o1.Data.Add(new ObservationData
            {
                TagName = "additionalTag",
                Value = 123
            });
            o3 = o1.Merge(o2, new Abstractions.MergeOptions { MergeConflicAction = Abstractions.MergeConflicAction.KeepFirst, PrefixFirst = "", PrefixSecond = "o2" });
            Assert.Equal(7, o3.Data.Count);

            // test if observations have diffent length of params
            o1 = GetObservation();
            o2 = GetObservation();
            o2.Data.Add(new ObservationData
            {
                TagName = "additionalTag",
                Value = 123
            });
            o3 = o1.Merge(o2, new Abstractions.MergeOptions { MergeConflicAction = Abstractions.MergeConflicAction.KeepFirst, PrefixFirst = "", PrefixSecond = "o2" });
            Assert.Equal(7, o3.Data.Count);


        }


        [Fact]
        public void TestObservation_TestPerformance()
        {
            var o1 = GetObservation();
            var o2 = GetObservation();
            for (int i = 0; i < 100000; i++)
            {
                o1.Data.Add(new ObservationData
                {
                    TagName = $"Tag_no_{i}",
                    Value = 1 * i
                });
                o1.Data.Add(new ObservationData
                {
                    TagName = $"Tag_no_{i}",
                    Value = 2 * i
                });

            }
            Observation o3;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            o3 = o1.Merge(o2);
            stopwatch.Stop();
            _output.WriteLine($"Time lapsed {stopwatch.ElapsedMilliseconds} ms");
            stopwatch.Reset();
            stopwatch.Start();
            o3 = o1.Merge(o2, new MergeOptions { PrefixSecond = "o2"});
            stopwatch.Stop();
            _output.WriteLine($"Time lapsed {stopwatch.ElapsedMilliseconds} ms");

        }
    }
}
