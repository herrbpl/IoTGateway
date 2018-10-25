using CommandLine;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Protocols.UMB
{    

    public class UMBServer
    {
       public  static async Task RunServerAsync(string listenaddress, int port, int timedelay)
        {

            IPAddress ipAddress;

            if (!IPAddress.TryParse(listenaddress, out ipAddress))
                ipAddress = Dns.GetHostEntry(listenaddress).AddressList[0];


            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, true));


            var group = new MultithreadEventLoopGroup();
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<SocketDatagramChannel>()
                    
                    .Handler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        channel.Pipeline.AddLast(new LoggingHandler("SRV-LSTN"));
                        channel.Pipeline.AddLast(new UMBServerHandler(timedelay));
                    }));

                IChannel boundChannel = await bootstrap.BindAsync(ipAddress, port);
                Console.WriteLine("Press any key to terminate the server.");
                Console.ReadLine();

                await boundChannel.CloseAsync();
            }
            finally
            {
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }

            Console.WriteLine("Server finished!");            
        }
    }
}
