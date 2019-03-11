using DeviceReader.Diagnostics;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Protocols
{
    public abstract class AbstractProtocolReader<T> : IProtocolReader where T: new()
    {

        protected ILogger _logger;
        protected IConfiguration _configroot;
        protected T _options = default(T);

        public AbstractProtocolReader(ILogger logger, string optionspath, IConfiguration configroot)
        {
            _logger = logger;
            _configroot = configroot;
            LoadOptions(optionspath);
            Initialize();
        }
        
        public virtual Toptions LoadOptions<Toptions>(IConfiguration configuration, string optionspath) where Toptions : new()
        {
            var _result = new Toptions();
            if (optionspath != null)
            {
                IConfigurationSection cs = null;
                try
                {
                    cs = _configroot.GetSection(optionspath);
                    
                    cs.Bind(_result);
                }
                catch (Exception e)
                {
                    _logger.Warn($"No options section {optionspath} found in configurationroot or it has invalid data: {e}", () => { });
                }
            }
            return _result;
        }
        
        public virtual void LoadOptions(string optionspath)
        {            
            _options = LoadOptions<T>(_configroot, optionspath);
        }
        
        public virtual void Initialize() { }

        public virtual Task<string> ReadAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public virtual Task<string> ReadAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken)
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
        // ~AbstractProtocolReader() {
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
