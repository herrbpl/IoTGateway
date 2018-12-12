using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeviceReader.Configuration
{
    /// <summary>
    /// Helps to load configuration sources. Refactored into separate interface/class from DeviceReader.Devices.Device for testing purposes.
    /// </summary>
    public interface IDeviceConfigurationLoader
    {
        /// <summary>
        /// Loads configuration based on configurationSource
        /// </summary>
        /// <param name="configurationSource">JToken containing configuration sources</param>
        /// <returns></returns>
        Task<Dictionary<string, string>> LoadConfigurationAsync(JToken configurationSource, Dictionary<string,string> replacements);
    }
}
