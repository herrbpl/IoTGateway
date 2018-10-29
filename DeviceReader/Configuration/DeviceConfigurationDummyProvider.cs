using DeviceReader.Diagnostics;
using DeviceReader.Extensions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Configuration
{
    public class DeviceConfigurationDummyProviderOptions
    {
        public string NamePlaceholder { get; set; } = "#NAME#";
        public string DefaultConfig { get; set; } = "{ 'configtype': 'dummy', 'name': '#NAME#' }";
    }

    public class DeviceConfigurationDummyProvider: DeviceConfigurationProviderBase<DeviceConfigurationDummyProviderOptions> 
    {

        
        //public DeviceConfigurationDummyProvider(string options):base( options){}
        public DeviceConfigurationDummyProvider(DeviceConfigurationDummyProviderOptions options) : base(options) { }

        public override async Task<TOut> GetConfigurationAsync<TIn, TOut>(TIn input)
        {
            TOut result = default(TOut);

            var configstr = _options.DefaultConfig.Replace(_options.NamePlaceholder,  input.ToString().Replace("'", "\\'"));


            if (typeof(TOut) == typeof(string))
            {
                result = (TOut)(object)configstr;
            }
            else if (typeof(TOut) == typeof(IConfiguration))
            {
                ConfigurationBuilder cb = new ConfigurationBuilder();
                cb.AddJsonString(configstr);
                var cfg = cb.Build();
                result = (TOut)(object)cfg;
            }
            else { throw new InvalidCastException(); }
            return result;
        }
    }
}
