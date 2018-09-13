using System;

namespace ME14Server
{
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using DotNetty.Codecs;
    using DotNetty.Handlers.Logging;
    using DotNetty.Handlers.Tls;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using DotNetty.Common.Internal.Logging;
    using Microsoft.Extensions.Logging.Console;
    using CommandLine;

    class Program
    {
        class Options
        {

            

            [Option(Required = false, HelpText = "Server port to use, 5000 default", Default = 5000)]
            public int serverport { get; set; }

            [Option(Required = false, HelpText = "Use SSL for authentication")]
            public bool usessl { get; set; }

            [Option(Required = false, HelpText = "PFX File Path")]
            public string pfxfile { get; set; }

            [Option(Required = false, HelpText = "PFX File password")]
            public string pfxpassword { get; set; }
        }

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


        static async Task RunServerAsync(string[] args)
        {

            int serverport = 5000;
            string path = null;
            bool exitfromopts = false;
            X509Certificate2 tlsCertificate = null;

            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(opts =>
            {
                

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
            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));

            var bossGroup = new MultithreadEventLoopGroup(1);
            var workerGroup = new MultithreadEventLoopGroup();

            var STRING_ENCODER = new StringEncoder();
            var STRING_DECODER = new StringDecoder();
            var SERVER_HANDLER = new ME14ServerHandler();
                                                   
            // run server

            try
            {
                var bootstrap = new ServerBootstrap();
                bootstrap
                    .Group(bossGroup, workerGroup)
                    .Channel<TcpServerSocketChannel>()
                    .Option(ChannelOption.SoBacklog, 100)
                    .Handler(new LoggingHandler(LogLevel.INFO))
                    .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (tlsCertificate != null)
                        {
                            pipeline.AddLast(TlsHandler.Server(tlsCertificate));
                        }

                        //pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
                        pipeline.AddLast(STRING_ENCODER, STRING_DECODER, SERVER_HANDLER);
                    }));

                IChannel bootstrapChannel = await bootstrap.BindAsync(serverport);

                Console.ReadLine();

                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                Task.WaitAll(bossGroup.ShutdownGracefullyAsync(), workerGroup.ShutdownGracefullyAsync());
            }

            Console.ReadLine();
        }

        static void Main(string[] args) => RunServerAsync(args).Wait();
    }
}
