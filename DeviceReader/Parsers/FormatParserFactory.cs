using System;
using System.Collections.Generic;
using System.Text;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;

namespace DeviceReader.Parsers
{
    public interface IFormatParserFactory<TInput, TOutput>
    {
        IFormatParser<TInput, TOutput> GetFormatParser(string format);
    }

    
    public class FormatParserFactory<TInput, TOutput> : IFormatParserFactory<TInput, TOutput>
    {       

        public virtual IFormatParser<TInput, TOutput> GetFormatParser(string format)
       {            
            var _parser = _getFormatParserDelegate(format);
            _logger.Debug(string.Format("FormatParser hash: {0}", _parser.GetHashCode()), () => { });
            return _parser;            
        }
        private ILogger _logger;
        private Func<string, IFormatParser<TInput, TOutput>> _getFormatParserDelegate;

        public FormatParserFactory (ILogger logger, Func<string, IFormatParser<TInput, TOutput>> getFormatParserDelegate) {
            _logger = logger;
            _getFormatParserDelegate = getFormatParserDelegate;
        }
    
    }

    

}
