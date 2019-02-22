using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using DeviceReader.Models;

namespace DeviceReader.Tests.Models
{
    public class MeasurementInfoTests
    {
        private readonly ITestOutputHelper _output;
        public MeasurementInfoTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Test_MeasurementInfo_AddRemove()
        {
            MeasurementMetadata m1 = new MeasurementMetadata();

            m1["a"] = 6;
            m1["b"] = 7;
            m1["c"] = "this is a string";

            Assert.Equal(typeof(int), ((MeasurementMetadataRecord) m1["a"]).Type);
            Assert.Equal(typeof(int), ((MeasurementMetadataRecord)m1["b"]).Type);
            Assert.Equal(typeof(string), ((MeasurementMetadataRecord)m1["c"]).Type);

            m1["a"] = null;

            Assert.NotNull(m1["a"]);
            Assert.Equal(2, ((MeasurementMetadataRecord)m1["a"]).Count);
            Assert.Equal(typeof(int), ((MeasurementMetadataRecord)m1["a"]).Type);

            m1.Unset("a");

            Assert.Null(m1["a"]);
        }

        [Fact]
        public void Test_EnumeratorImmutability()
        {
            MeasurementMetadata m1 = new MeasurementMetadata();
            m1["a"] = 6;
            m1["b"] = 7;
            m1["c"] = "this is a string";

            foreach (var item in m1)
            {
                item.Count = 10;
            }

            Assert.Equal(1, ((MeasurementMetadataRecord)m1["a"]).Count);

        }
    }
    
}
