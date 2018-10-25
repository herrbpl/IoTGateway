using CommandLine;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Net;
using System.Threading.Tasks;
namespace DeviceReader.Protocols.UMB
{
    class UMBClient
    {
        public static async Task RunClientAsync(string address, int port, ushort to, ushort from, byte cmd, byte[] payload, uint timeout)
        {

            IPAddress ipAddress;

            if (!IPAddress.TryParse(address, out ipAddress))
                ipAddress = Dns.GetHostEntry(address).AddressList[0];

            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));

            var group = new SingleThreadEventLoop();

            //var DatagramEncoder = new DatagramPacketEncoder();
            try
            {
                var bootstrap = new Bootstrap();
                var client = new UdpChannelHandlerClient();

                bootstrap
                    .Group(group)
                    .Channel<SocketDatagramChannel>()
                    .Handler(new ActionChannelInitializer<SocketDatagramChannel>(channel =>
                    {
                        
                        channel.Pipeline.AddLast(new LoggingHandler(LogLevel.INFO));
                        channel.Pipeline.AddLast(client);
                    }));

                //IChannel bootstrapChannel = await bootstrap.ConnectAsync(new IPEndPoint(ipAddress, port));

                IChannel bootstrapChannel = await bootstrap.BindAsync(IPEndPoint.MinPort);
                //IChannel bootstrapChannel = await bootstrap.BindAsync();

                Console.WriteLine("Starting client activity");

                // send command. 
                //var result = await client.SendRequest(from, to, cmd, payload);

                var frame = new Frame(new FrameAddress(from), new FrameAddress(to), cmd, payload);

                // wait for answer
                var buf = Unpooled.CopiedBuffer(frame.Data);
                //var buf = Unpooled.CopiedBuffer("MMUUUUUU!!", Encoding.UTF8);

                await bootstrapChannel.WriteAndFlushAsync(
                    new DatagramPacket(buf, new IPEndPoint(ipAddress, port)));



                //Console.WriteLine($"Result is {result}");

                await Task.Delay((int)timeout);

                await bootstrapChannel.CloseAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e}");
            }
            finally
            {
                group.ShutdownGracefullyAsync().Wait(100);
            }
            Console.ReadLine();
        }
    }
}
