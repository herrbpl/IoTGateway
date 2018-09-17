using DeviceReader.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using Microsoft.Extensions.Logging.Console;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Logging;
using DotNetty.Buffers;
using System.Net.Security;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Protocols
{
    public class ME14ProtocolReaderOptions
    {
        public string HostName { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5000;
        public int TimeOut { get; set; } = 5;
    }

    public class ME14ProtocolReader: AbstractProtocolReader<ME14ProtocolReaderOptions>
    {
        public ME14ProtocolReader(ILogger logger, string optionspath, IConfigurationRoot configroot) : base(logger, optionspath, configroot)
        {

        }
    }
}
