using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
namespace DeviceReader.Configuration
{
    public interface IDeviceConfigurationProviderFactory
    {
        // should we provide configuration to source provider? Like config options or something?
        // say we want to have azure table config provider - need connectionstring. 
        // in short, how should factory initialize source provider?
        
        /// <summary>
        /// Gets Device Configuration Provider with default provider config.
        /// </summary>
        /// <param name="providertype"></param>
        /// <returns></returns>
        IDeviceConfigurationProvider Get(string providertype);

        /// <summary>
        /// Gets Device Configuration Provider with default provider config.
        /// </summary>
        /// <param name="providertype">Provider Type</param>
        /// <returns></returns>
        IDeviceConfigurationProvider Get(string providertype, string provideroptions );
    }
}
