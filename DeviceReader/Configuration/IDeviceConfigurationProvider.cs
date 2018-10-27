using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Configuration
{
    /// <summary>
    /// Get configuration for device
    /// var provider = factory.GetConfigurationProvider("someprovider", options); provider.GetConfiguration();
    /// or provider.GetConfiguration(parameter)
    /// IconfigurationBuilder.Add(_configurationSourceProviderFactory.Get("confprovider")
    /// This should return string.
    /// </summary>
    public interface IDeviceConfigurationProvider
    {        
        Task<TOut> GetConfigurationAsync<TIn, TOut>(TIn input);
    }

    public class DeviceConfigurationProviderMetadata
    {
        public string ProviderName { get; set; }      
        public Type OptionsType { get; set; }
        public string GlobalConfigurationKey { get; set; }
    }

    /// <summary>
    /// Get configuration for device
    /// </summary>
    public interface IDeviceConfigurationProviderOld<T>
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
