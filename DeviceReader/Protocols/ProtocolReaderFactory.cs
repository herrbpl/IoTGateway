using System;
using System.Collections.Generic;
using System.Text;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Protocols
{
    // TODO: remove any requirement for type setting for protocols/parsers. In fact, remove any dependency on IDeviceAgent
    // TODO: Message passing to uniform

    public interface IProtocolReaderFactory
    {
        //IProtocolReader GetProtocolReader(string protocol, IConfigurationSection config);
        IProtocolReader GetProtocolReader(string protocol, IConfigurationRoot config);
    }

    

    
    public class ProtocolReaderFactory : IProtocolReaderFactory
    {
        private ILogger _logger;        
        //Func<string, IConfigurationSection, IProtocolReader> _getProtocolReader;
        Func<string, IConfigurationRoot, IProtocolReader> _getProtocolReader;

        //public ProtocolReaderFactory(ILogger logger, Func<string, IConfigurationSection, IProtocolReader> getProtocolReader)
        public ProtocolReaderFactory(ILogger logger, Func<string, IConfigurationRoot, IProtocolReader> getProtocolReader)
        {
            _logger = logger;
            if (getProtocolReader == null) throw new ArgumentNullException("getProtocolReader");
            _getProtocolReader = getProtocolReader;
        }

        //public IProtocolReader GetProtocolReader(string protocol, IConfigurationSection config)
        public IProtocolReader GetProtocolReader(string protocol, IConfigurationRoot config)
        {                        
            IProtocolReader result = _getProtocolReader(protocol, config);            
            return result;            
        }
    
    }
}
