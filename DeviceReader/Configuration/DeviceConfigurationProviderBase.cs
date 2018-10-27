using DeviceReader.Diagnostics;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace DeviceReader.Configuration
{
    public abstract class DeviceConfigurationProviderBase<TOptions> : IDeviceConfigurationProvider where TOptions: new()
    {
        
        protected TOptions _options = default(TOptions);
        public TOptions Options { get => _options; set => _options = value; }
      
        protected DeviceConfigurationProviderBase(TOptions options)
        {
            _options = options;
        }

        /*
        protected DeviceConfigurationProviderBase(string options)
        {
            SetOptions(options); // throws if cannot serialize;
        }
        */

        protected void SetOptions(string options)
        {
            if (options == null)
            {
                _options = new TOptions();
            }
            else
            {                
                _options = JsonConvert.DeserializeObject<TOptions>(options);                
            }
        }

        public virtual async Task<TOut> GetConfigurationAsync<TIn, TOut>(TIn input)
        {
            throw new NotImplementedException();
        }
    }
}
