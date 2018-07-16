using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using DeviceReader.Protocols;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.Parsers;
using System.Linq;

namespace DeviceReader.Extensions
{

    public static class DeviceReaderExtensions
    {
        /// <summary>
        /// Registers protocol readers for DeviceAgentRunner.         
        /// </summary>
        /// <param name="builder"></param>
        public static void RegisterProtocolReaders(this ContainerBuilder builder)
        {
            
            // AUtoregister all implemented interfaces? Something better later than using simple text.
            builder.RegisterType<DummyProtocolReader>().As<IProtocolReader>().WithMetadata<ProtocolReaderMetadata>(
                m => m.For(am => am.ProtocolName, "dummy")
                );
            builder.RegisterType<HttpProtocolReader>().As<IProtocolReader>().WithMetadata<ProtocolReaderMetadata>(
                m => m.For(am => am.ProtocolName, "http")
                );


            /*

            // protocol reader factory resolver delegate, this is required parameter for factory creation
            GetProtocolReaderDelegate gprd = (requestedProtocolReader) => {

                //IComponentContext context = container.Resolve<IComponentContext>();
                IComponentContext context = cont;
                Console.WriteLine("IM HERE");
                IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>> _protocols = context.Resolve<IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>>>(
                    new TypedParameter(typeof(IDeviceAgent), requestedProtocolReader)
                    );
                IProtocolReader protocolReader = _protocols.FirstOrDefault(pr => pr.Metadata.ProtocolName.Equals(requestedProtocolReader.Device.Config.ProtocolReader))?.Value;
                if (protocolReader == null) throw new ArgumentException(string.Format("ProtocolReader {0} is not supported.", requestedProtocolReader), "requestedProtocolReader");
               
                return protocolReader;
            };
            */
            builder.Register<IProtocolReaderFactory>(
                (c,p) => {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();
                    
                    Func<IDeviceAgent, IProtocolReader> rcode = (agent) => {
                        _logger.Debug("Calling inside func", () => { });
                        IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>> _protocols = context.Resolve<IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>>>(
                            new TypedParameter(typeof(IDeviceAgent), agent)
                            );
                        IProtocolReader protocolReader = _protocols.FirstOrDefault(pr => pr.Metadata.ProtocolName.Equals(agent.Device.Config.ProtocolReader))?.Value;
                        if (protocolReader == null) throw new ArgumentException(string.Format("ProtocolReader {0} is not supported.", agent.Device.Config.ProtocolReader), "requestedProtocolReader");

                        return protocolReader;
                    };
                    return new ProtocolReaderFactory(_logger, rcode);
                }
                ).As<IProtocolReaderFactory>().SingleInstance();

            /*
            builder.RegisterType<ProtocolReaderFactory>().As<IProtocolReaderFactory>().SingleInstance().WithParameter(
                new TypedParameter(typeof(GetProtocolReaderDelegate), gprd)
                );
            */
        }
        public static void RegisterFormatParsers(this ContainerBuilder builder)
        {
            builder.RegisterType<DummyParser>().As<IFormatParser<string, string>>().SingleInstance().WithMetadata<ParserMetadata>(
                m => m.For(am => am.FormatName, "dummy")
                );

            builder.Register<IFormatParserFactory<string, string>>(
                (c, p) =>
                {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();

                    Func<IDeviceAgent, IFormatParser<string, string>> rcode = (agent) =>
                    {
                        _logger.Debug("Calling inside func", () => { });
                        IEnumerable<Lazy<IFormatParser<string, string>, ParserMetadata>> _formats = context.Resolve<IEnumerable<Lazy<IFormatParser<string, string>, ParserMetadata>>>(
                            new TypedParameter(typeof(IDeviceAgent), agent)
                            );
                        IFormatParser<string, string> formatParser = _formats.FirstOrDefault(pr => pr.Metadata.FormatName.Equals(agent.Device.Config.FormatParser))?.Value;
                        if (formatParser == null) throw new ArgumentException(string.Format("FormatParser {0} is not supported.", agent.Device.Config.FormatParser), "requestedProtocolReader");

                        return formatParser;
                    };
                    return new FormatParserFactory<string, string>(_logger, rcode);
                }
                ).As<IFormatParserFactory<string, string>>().SingleInstance();

            /*

            // format parser finder delegate
            Func<IDeviceAgent, IFormatParser<string, string>> ddd = (da) => {
                IComponentContext context = Container.Resolve<IComponentContext>();
                logger.Debug("GetProtocolReaderFactory.GetProtocolReaderDelegate called!", () => { });
                IEnumerable<Lazy<IFormatParser<string, string>, ParserMetadata>> _formats = context.Resolve<IEnumerable<Lazy<IFormatParser<string, string>, ParserMetadata>>>(
                    new TypedParameter(typeof(IDeviceAgent), da)
                    );
                IFormatParser<string, string> formatParser = _formats.FirstOrDefault(pr => pr.Metadata.FormatName.Equals(da.Device.Config.FormatParser))?.Value;
                if (formatParser == null) throw new ArgumentException(string.Format("FormatParser {0} is not supported.", da.Device.Config.FormatParser), "requestedProtocolReader");
                return formatParser;
            };

            // register format parser factory..
            builder.RegisterType<FormatParserFactory<string, string>>().As<IFormatParserFactory<string, string>>().SingleInstance().WithParameter(
               new TypedParameter(typeof(Func<IDeviceAgent, IFormatParser<string, string>>), ddd)
               );
            */
        }
    }
}
