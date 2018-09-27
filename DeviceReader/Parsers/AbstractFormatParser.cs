namespace DeviceReader.Parsers
{

    using DeviceReader.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class AbstractFormatParser<TOptions,TIn,TOut> : IFormatParser<TIn,TOut> where TOptions : new()
    {
        protected ILogger _logger;
        protected IConfigurationRoot _configroot;
        protected TOptions _options = default(TOptions);

        public AbstractFormatParser(ILogger logger, string optionspath, IConfigurationRoot configroot)
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
        }

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        public virtual Task<List<TOut>> ParseAsync(TIn input, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
