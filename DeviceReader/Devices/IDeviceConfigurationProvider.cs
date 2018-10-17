using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Devices
{
    /// <summary>
    /// Get configuration for device
    /// </summary>
    public interface IDeviceConfigurationProvider<T>
    {
        /// <summary>
        /// Gets configuration for device with deviceId, given input 
        /// </summary>
        /// <param name="deviceId">Device ID to get configuration for</param>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<string> GetConfigurationAsync(string deviceId, T input);

        /// <summary>
        /// Gets configuration for device with deviceId
        /// </summary>
        /// <param name="deviceId">Device ID to get configuration for</param>
        /// <returns></returns>
        Task<string> GetConfigurationAsync(string deviceId);
    }
}
