using System;
using System.Collections.Generic;
using System.Text;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;

namespace DeviceReader.Protocols
{
    // currently just a dummy. Probably moving to autofac later?
    class ProtocolReaderFactory : IProtocolReaderFactory
    {
        private ILogger _logger;

        static IDictionary<SourceProtocol, IProtocolReader> SingletonReaders = new Dictionary<SourceProtocol, IProtocolReader>();

        public ProtocolReaderFactory(ILogger logger)
        {
            _logger = logger;
        }
        public IProtocolReader GetProtocolReader(SourceProtocol protocol)
        {
            if (SingletonReaders.ContainsKey(protocol))
            {
                return SingletonReaders[protocol];
            } else
            {
                IProtocolReader n = new HttpProtocolReader();
                SingletonReaders.Add(protocol, n);
                return n;
            }
        }
    }
}
