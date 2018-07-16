using System;
using System.Collections.Generic;
using System.Text;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;

namespace DeviceReader.Parsers
{
    public interface IFormatParserFactory<TInput, TOutput>
    {
        IFormatParser<TInput, TOutput> GetFormatParser(IDeviceAgent agent);
    }

    

    public class FormatParserFactory<TInput, TOutput> : IFormatParserFactory<TInput, TOutput>
    {       

        public virtual IFormatParser<TInput, TOutput> GetFormatParser(IDeviceAgent agent)
       {
            _logger.Debug(string.Format("FormatParser asked: {0}", agent.Device.Id), () => { });
            var _parser = _getFormatParserDelegate(agent);
            _logger.Debug(string.Format("FormatParser hash: {0}", _parser.GetHashCode()), () => { });
            return _parser;            
        }
        private ILogger _logger;
        private Func<IDeviceAgent, IFormatParser<TInput, TOutput>> _getFormatParserDelegate;

        public FormatParserFactory (ILogger logger, Func<IDeviceAgent, IFormatParser<TInput, TOutput>> getFormatParserDelegate) {
            _logger = logger;
            _getFormatParserDelegate = getFormatParserDelegate;
        }
    
    }

    

}
