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
    // TODO: Refactor configRootPath and IConfigurationRoot into one structure. 

    public interface IProtocolReaderFactory
    {
        //IProtocolReader GetProtocolReader(string protocol, IConfigurationSection config);
        /// <summary>
        /// Creates protocol reader for Protocol and passes configuration along with pointer where protocol specific configuration is.
        /// </summary>
        /// <param name="protocol">Protocol reader to initialize</param>
        /// <param name="configRootPath">Where configuration for protocol starts</param>
        /// <param name="config">IConfigurationRoot structure</param>
        /// <returns></returns>
        IProtocolReader GetProtocolReader(string protocol, string configRootPath, IConfigurationRoot config);
    }

    

    
    public class ProtocolReaderFactory : IProtocolReaderFactory
    {
        private ILogger _logger;        
        //Func<string, IConfigurationSection, IProtocolReader> _getProtocolReader;
        Func<string, string, IConfigurationRoot, IProtocolReader> _getProtocolReader;

        //public ProtocolReaderFactory(ILogger logger, Func<string, IConfigurationSection, IProtocolReader> getProtocolReader)
        public ProtocolReaderFactory(ILogger logger, Func<string, string, IConfigurationRoot, IProtocolReader> getProtocolReader)
        {
            _logger = logger;
            if (getProtocolReader == null) throw new ArgumentNullException("getProtocolReader");
            _getProtocolReader = getProtocolReader;
        }

        //public IProtocolReader GetProtocolReader(string protocol, IConfigurationSection config)
        public IProtocolReader GetProtocolReader(string protocol, string configRootPath, IConfigurationRoot config)
        {                        
            IProtocolReader result = _getProtocolReader(protocol, configRootPath, config);            
            return result;            
        }
    
    }
}
