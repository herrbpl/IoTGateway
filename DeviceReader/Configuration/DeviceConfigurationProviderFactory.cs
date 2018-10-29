using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Configuration
{
    public class DeviceConfigurationProviderFactory : IDeviceConfigurationProviderFactory
    {
        private readonly Func<string, string, IDeviceConfigurationProvider> _delegate;

        public DeviceConfigurationProviderFactory(Func<string, string, IDeviceConfigurationProvider> _delegate)
        {
            this._delegate = _delegate ?? throw new ArgumentNullException();
        }

        public IDeviceConfigurationProvider Get(string providertype)
        {
            return _delegate(providertype, null);
        }

        public IDeviceConfigurationProvider Get(string providertype, string provideroptions)
        {
            return _delegate(providertype, provideroptions);
        }
    }
}
