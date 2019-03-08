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
using System.Diagnostics;
using System.Threading;

namespace DeviceReader.Protocols
{
    public class ME14ProtocolReaderOptions
    {
        public string HostName { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5000;
        public int TimeOut { get; set; } = 5;
        public bool Debug { get; set; } = false;
        
        /// <summary>
        /// Indicates that simple query format should be used (@stationid MES 14). History format is not supported then.
        /// </summary>
        public bool Simple { get; set; } = false;

        /// <summary>
        /// Message number to retrieve ("01".."04")        
        /// </summary>
        public string MessageId { get; set; } = "01";        
    }

    enum ME14RetOptions
    {
        MES14 = 1,
        HIST
    }

    /// <summary>
    /// This implementation currently has memory leak which is related to dotnetty issue
    /// https://github.com/Azure/DotNetty/issues/344
    /// So its not my bad code that leaks it..
    /// </summary>
    public class ME14ProtocolReader: AbstractProtocolReader<ME14ProtocolReaderOptions>
    {

        private string ME14Result = "";
        private SingleThreadEventLoop group = null;

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

        public static IByteBuffer[] ME14SimpleDelimiters()
        {
            return new[]
            {
                Unpooled.WrappedBuffer(new[] { (byte)'\r', (byte)'\n' }),
                //Unpooled.WrappedBuffer(new[] { (byte)'\r', (byte)'\n' }),                                
            };
        }

        public ME14ProtocolReader(ILogger logger, string optionspath, IConfiguration configroot) : base(logger, optionspath, configroot)
        {
            // Initialize 
            this.group = new SingleThreadEventLoop();
        }
       

        override public async Task<string> ReadAsync(CancellationToken cancellationToken)
        {            
            return await ReadAsync(null, cancellationToken);
        }

        private void setResult(string input)
        {
            ME14Result = input;
            input = null;
        }

        override public async Task<string> ReadAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            Stopwatch stopwatch = new Stopwatch();
            // Begin timing.
            stopwatch.Start();

            // get station id
            string messageId = (parameters != null && parameters.ContainsKey("StationId") ? parameters["StationId"] : _options.MessageId);


            var tcs = new TaskCompletionSource<int>();
          
                try {
                    IPAddress ipAddress;

                    if (!IPAddress.TryParse(_options.HostName, out ipAddress))
                        ipAddress = Dns.GetHostEntry(_options.HostName).AddressList[0];

                    var STRING_ENCODER = new StringEncoder();
                    var STRING_DECODER = new StringDecoder();
                    IByteBuffer[] DELIMITERS = null;
                    SimpleChannelInboundHandler<string> CLIENT_HANDLER = null;

                    if (_options.Simple)
                    {
                        CLIENT_HANDLER = new ME14SimpleProtocolReaderHandler(tcs, messageId, setResult);
                        DELIMITERS = ME14SimpleDelimiters();
                    }
                    else
                    {
                           CLIENT_HANDLER = new ME14ProtocolReaderHandler(tcs, ME14RetOptions.MES14, messageId, setResult);
                    }
                

                    var bootstrap = new Bootstrap();

                    bootstrap
                        .Group(group)
                        .Channel<TcpSocketChannel>()
                        .Option(ChannelOption.TcpNodelay, true)
                        .Option(ChannelOption.Allocator, UnpooledByteBufferAllocator.Default)
                        .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                        {
                            IChannelPipeline pipeline = channel.Pipeline;
                       
                        /*
                        if (debug)
                        {
                            pipeline.AddLast(new LoggingHandler(LogLevel.INFO));
                        }*/
                            pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, false, ME14Delimiters()));
                            pipeline.AddLast(STRING_ENCODER, STRING_DECODER, CLIENT_HANDLER);
                        }));

                    IChannel bootstrapChannel = await bootstrap.ConnectAsync(new IPEndPoint(ipAddress, _options.Port));
                    // should wait for handler-started exit..
                    //Task.WaitAny(tcs.Task, Task.Delay(_options.TimeOut * 1000));
                    await Task.WhenAny(tcs.Task, Task.Delay(_options.TimeOut * 1000));
                    await bootstrapChannel.CloseAsync();
                    bootstrapChannel = null;
                    bootstrap = null;
                    CLIENT_HANDLER = null;
                    STRING_DECODER = null;
                    STRING_ENCODER = null;
                    ipAddress = null;
                }
                catch (TaskCanceledException e) { }
                catch (OperationCanceledException e) { }
                catch (AggregateException e)
                {
                    _logger.Debug(e.ToString(), () => { });
                }
                catch (Exception e)
                {
                    //_logger.Error(e.ToString(), () => { });                    
                    throw e;
                }
           
            stopwatch.Stop();
            stopwatch = null;
            return ME14Result;
            
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    group.ShutdownGracefullyAsync().Wait(100);
                    group = null;
                }
                
            }
            base.Dispose(disposing);
        }

    }
}
