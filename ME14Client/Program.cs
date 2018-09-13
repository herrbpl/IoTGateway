using System;

namespace ME14Client
{

    using CommandLine;
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

    class Options
    {

        [Option(Required = false, HelpText = "Server address to use, 127.0.0.1 default", Default = "127.0.0.1")]
        public string serveraddress { get; set; }

        [Option(Required = false, HelpText = "Server port to use, 5000 default", Default = 5000)]
        public int serverport { get; set; }

        [Option(Required = false, HelpText = "Use SSL for authentication")]
        public bool usessl { get; set; }

        [Option(Required = false, HelpText = "PFX File Path")]
        public string pfxfile { get; set; }

        [Option(Required = false, HelpText = "PFX File password")]
        public string pfxpassword { get; set; }
    }
    /// <summary>
    /// Example program
    /// TODO: Check how to use logging frameworks and integrate so logging framework can be selected.
    /// </summary>
    class Program
    {

        public static string ProcessDirectory
        {
            get
            {
#if NETSTANDARD1_3
                return AppContext.BaseDirectory;
#else
                return AppDomain.CurrentDomain.BaseDirectory;
#endif
            }
        }

        public static IByteBuffer[] ME14Delimiters()
        {
            return new[]
            {
                Unpooled.WrappedBuffer(new[] { (byte)'\r', (byte)'\n' }),
                //Unpooled.WrappedBuffer(new[] { (byte)'\r', (byte)'\n' }),
                Unpooled.WrappedBuffer(new[] { (byte)'\n' }),
                Unpooled.WrappedBuffer(new[] { (byte)'>' }),
                Unpooled.WrappedBuffer(new[] { (byte)(7) }),

            };
        }

        static async Task RunClientAsync(string[] args)
        {

            int serverport = 5000;
            IPAddress serveraddress = IPAddress.Parse("127.0.0.1");
            string targetHost = null; 
            string path = null;
            bool exitfromopts = false;
            X509Certificate2 tlsCertificate = null;

            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(opts =>
            {

                serveraddress = IPAddress.Parse( opts.serveraddress);

                if (opts.usessl)
                {
                    if (opts.pfxfile == null) throw new ArgumentException($"PFX File path not specified for SSL option");
                    if (opts.pfxpassword == null) throw new ArgumentException($"PFX password not specified for SSL option");

                    path = opts.pfxfile;

                    if (!File.Exists(path))
                    {
                        path = Path.Combine(Program.ProcessDirectory, opts.pfxfile);
                        if (!File.Exists(path))
                        {
                            throw new ArgumentException($"File not found: '{path}'");
                        }
                    }

                    tlsCertificate = new X509Certificate2(path, opts.pfxpassword);

                    targetHost = tlsCertificate.GetNameInfo(X509NameType.DnsName, false);

                }
                serverport = opts.serverport;



            }).WithNotParsed<Options>((errors) => {
                Console.WriteLine("Invalid program arguments:");
                foreach (var err in errors)
                {
                    Console.WriteLine(err.ToString());
                }
                exitfromopts = true;
            });

            if (exitfromopts) return;


            //ExampleHelper.SetConsoleLogger();
            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => (level >= Microsoft.Extensions.Logging.LogLevel.Debug), false));

            /*var bossGroup = new MultithreadEventLoopGroup(1);
            var workerGroup = new MultithreadEventLoopGroup();
            */
            var group = new MultithreadEventLoopGroup();

            var STRING_ENCODER = new StringEncoder();
            var STRING_DECODER = new StringDecoder();

            var tcs = new TaskCompletionSource<int>();

            var CLIENT_HANDLER = new ME14ClientHandler(tcs);

            // run client

            try
            {
                

                var bootstrap = new Bootstrap();
                
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        if (tlsCertificate != null)
                        {
                            pipeline.AddLast(new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        }

                        //pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));


                        pipeline.AddLast(new LoggingHandler(LogLevel.INFO));
                        pipeline.AddLast(new DelimiterBasedFrameDecoder(8192,false, ME14Delimiters()  ));
                        pipeline.AddLast(STRING_ENCODER, STRING_DECODER, CLIENT_HANDLER);
                    }));



               


                try
                {
                    IChannel bootstrapChannel = await bootstrap.ConnectAsync(new IPEndPoint(serveraddress, serverport));
                    // should wait for handler-started exit..
                    Task.WaitAny(tcs.Task, Task.Delay(10000));
                    
                    await bootstrapChannel.CloseAsync();
                } catch (TaskCanceledException e) { }
                  catch (OperationCanceledException e) { }
                  catch (AggregateException e)
                {
                    Console.WriteLine(e);
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                Console.WriteLine("Client completed");

                

                
            }
            finally
            {
                group.ShutdownGracefullyAsync().Wait(1000);
            }

            Console.ReadLine();
        }

        static void Main(string[] args) => RunClientAsync(args).Wait();
    }
}
