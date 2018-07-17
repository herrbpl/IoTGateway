using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using DeviceReader.Protocols;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.Parsers;
using DeviceReader.Models;

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

            
            builder.Register<IProtocolReaderFactory>(
                (c,p) => {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();
                    
                    Func<IDeviceAgent, IProtocolReader> rcode = (agent) => {

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

        }

        /// <summary>
        /// Register format parsers. 
        /// TODO: Load from config file/assembly/plugin dir
        /// </summary>
        /// <param name="builder"></param>
        public static void RegisterFormatParsers(this ContainerBuilder builder)
        {

            
            builder.RegisterType<DummyParser>().As<IFormatParser<string, List<Observation>>>().SingleInstance().WithMetadata<ParserMetadata>(
                m => m.For(am => am.FormatName, "dummy")
                );

            builder.Register<IFormatParserFactory<string, List<Observation>>>(
                (c, p) =>
                {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();

                    Func<IDeviceAgent, IFormatParser<string, List<Observation>>> rcode = (agent) =>
                    {
                        _logger.Debug("Calling inside func", () => { });
                        IEnumerable<Lazy<IFormatParser<string, List<Observation>>, ParserMetadata>> _formats = context.Resolve<IEnumerable<Lazy<IFormatParser<string, List<Observation>>, ParserMetadata>>>(
                            new TypedParameter(typeof(IDeviceAgent), agent)
                            );
                        IFormatParser<string, List<Observation>> formatParser = _formats.FirstOrDefault(pr => pr.Metadata.FormatName.Equals(agent.Device.Config.FormatParser))?.Value;
                        if (formatParser == null) throw new ArgumentException(string.Format("FormatParser {0} is not supported.", agent.Device.Config.FormatParser), "requestedProtocolReader");

                        return formatParser;
                    };
                    return new FormatParserFactory<string, List<Observation>>(_logger, rcode);
                }
                ).As<IFormatParserFactory<string, List<Observation>>>().SingleInstance();

           
        }
    }
}
