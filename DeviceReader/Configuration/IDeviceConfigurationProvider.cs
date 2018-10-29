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
    public interface IDeviceConfigurationProvider: IDisposable
    {        
        Task<TOut> GetConfigurationAsync<TIn, TOut>(TIn input);
    }

    public class DeviceConfigurationProviderMetadata
    {
        public string ProviderName { get; set; }      
        public Type OptionsType { get; set; }
        public string GlobalConfigurationKey { get; set; }
    }    
}
