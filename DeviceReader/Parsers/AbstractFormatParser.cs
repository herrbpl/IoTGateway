namespace DeviceReader.Parsers
{

    using DeviceReader.Diagnostics;
    using DeviceReader.Models;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class AbstractFormatParser<TOptions,TIn,TOut> : IFormatParser<TIn,TOut> where TOptions : new()
    {
        protected ILogger _logger;
        protected IConfiguration _configroot;
        protected TOptions _options = default(TOptions);
        public int TimeZoneAdjust { get; set; }
        protected string DeviceName = null;

        public AbstractFormatParser(ILogger logger, string optionspath, IConfiguration configroot)
        {
            _logger = logger;

            _configroot = configroot;

            // defaults
            _options = new TOptions();

            if (optionspath != null)
            {
                IConfigurationSection cs = null;
                try
                {
                    cs = _configroot.GetSection(optionspath);
                    
                    cs.Bind(_options);
                }
                catch (Exception e)
                {
                    _logger.Warn($"No options section {optionspath} found in configurationroot or it has invalid data: {e}", () => { });
                }
            }
            DeviceName = (_configroot != null ? _configroot.GetValue<string>("name", "") : "");
            TimeZoneAdjust = (_configroot != null ? _configroot.GetValue<int>("timezoneadjust", 0) : 0);
        }


        public virtual Task<List<TOut>> ParseAsync(TIn input, CancellationToken cancellationToken)
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
