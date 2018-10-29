using System;
using System.Collections.Generic;
using System.Text;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Parsers
{
    public interface IFormatParserFactory<TInput, TOutput>
    {
        IFormatParser<TInput, TOutput> GetFormatParser(string format, string configRootPath, IConfiguration config);
        IFormatParser<TInput, TOutput> GetFormatParser(string format);
    }

    
    public class FormatParserFactory<TInput, TOutput> : IFormatParserFactory<TInput, TOutput>
    {       

        public virtual IFormatParser<TInput, TOutput> GetFormatParser(string format, string configRootPath, IConfiguration config)
       {            
            var _parser = _getFormatParserDelegate(format, configRootPath, config);
            _logger.Debug(string.Format("FormatParser hash: {0}", _parser.GetHashCode()), () => { });
            return _parser;            
        }

        public IFormatParser<TInput, TOutput> GetFormatParser(string format)
        {
            return GetFormatParser(format, null, null);
        }

        private ILogger _logger;
        private Func<string, string, IConfiguration, IFormatParser<TInput, TOutput>> _getFormatParserDelegate;

        public FormatParserFactory (ILogger logger, Func<string, string, IConfiguration, IFormatParser<TInput, TOutput>> getFormatParserDelegate) {
            _logger = logger;
            _getFormatParserDelegate = getFormatParserDelegate;
        }
    
    }

    

}
