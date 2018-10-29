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

        #region IDisposable Support
        protected bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DeviceConfigurationProviderBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
