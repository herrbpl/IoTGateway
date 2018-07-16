using System;
using System.Collections.Generic;
using System.Text;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;

namespace DeviceReader.Protocols
{

    public interface IProtocolReaderFactory
    {
        IProtocolReader GetProtocolReader(IDeviceAgent agent);     
    }

    public delegate IProtocolReader GetProtocolReaderDelegate(IDeviceAgent agent);

    
    public class ProtocolReaderFactory : IProtocolReaderFactory
    {
        private ILogger _logger;
        //GetProtocolReaderDelegate _getProtocolReader;

        Func<IDeviceAgent, IProtocolReader> _getProtocolReader;
        

        public ProtocolReaderFactory(ILogger logger, Func<IDeviceAgent, IProtocolReader> getProtocolReader)
        {
            _logger = logger;
            if (getProtocolReader == null) throw new ArgumentNullException("getProtocolReader");
            _getProtocolReader = getProtocolReader;
        }
        /*
        public ProtocolReaderFactory(ILogger logger, ICustomDependencyResolver customDependencyResolver)
        {
            _logger = logger;            
            _customDependencyResolver = customDependencyResolver;
        }
        */
        public IProtocolReader GetProtocolReader(IDeviceAgent agent)
        {
            _logger.Debug(string.Format("ProtocolReader asked: {0}", agent.Device.Id), () => { });
            IProtocolReader result = _getProtocolReader(agent);
            _logger.Debug(string.Format("ProtocolReader hash: {0}", result.GetHashCode()), () => { });
            return result;            
        }
        /*
        public IProtocolReader GetProtocolReader(IDeviceAgent agent)
        {
            _logger.Debug(string.Format("ProtocolReader asked: {0}", agent.Device.Id), () => { });
            IComponentContext context = container;
            IProtocolReader result = _getProtocolReader(agent);
            _logger.Debug(string.Format("ProtocolReader hash: {0}", result.GetHashCode()), () => { });
            return result;
        }
        */
    }
}
