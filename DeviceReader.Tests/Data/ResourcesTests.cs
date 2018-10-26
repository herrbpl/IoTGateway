using DeviceReader.Data;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using System.Runtime.CompilerServices;

namespace DeviceReader.Tests.Data
{
    public class ResourcesTests
    {
        [Fact]
        public void ResourceNamePrefix_Test()
        {
            Assert.Equal("DeviceReader.Data.", Resources.RESOURCE_PREFIX);
        }

        [Fact]
        public void ResourcesExistence_Test()
        {
            Assert.True(Resources.Exists("me14_observations.json"));
            Assert.True(Resources.Exists("VaisalaXML-Parameter-Datatype-map.json"));
            Assert.True(Resources.Exists("vaisala_v3_common.xsd"));
            Assert.True(Resources.Exists("vaisala_v3_observation.xsd"));
        }

        [Fact]
        public void ResourcesNotExistenceException_Test()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var result = StringResources.Resources["NotExistingResource"];
            });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var result = BinaryResources.Resources["NotExistingResource"];
            });
        }
    }
}
