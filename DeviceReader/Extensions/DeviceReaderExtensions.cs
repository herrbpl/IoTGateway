using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using DeviceReader.Protocols;
using DeviceReader.Devices;
using DeviceReader.Diagnostics;
using DeviceReader.Parsers;
using DeviceReader.Models;
using DeviceReader.Router;
using DeviceReader.Agents;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace DeviceReader.Extensions
{

    public static class DeviceReaderExtensions
    {

        public static void RegisterDeviceReaderServices(this ContainerBuilder builder)
        {
            RegisterProtocolReaders(builder);
            RegisterFormatParsers(builder);

          

            RegisterRouterFactory(builder);
        }

        /// <summary>
        /// Registers protocol readers for DeviceAgentRunner.         
        /// </summary>
        /// <param name="builder"></param>
        private static void RegisterProtocolReaders(this ContainerBuilder builder)
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

                // Gets protocol reader for type
                    //Func<string, IConfigurationSection, IProtocolReader> rcode = (protocol, readerconfig) => {
                    Func<string, IConfigurationRoot, IProtocolReader> rcode = (protocol, readerconfig) => {

                        IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>> _protocols = context.Resolve<IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>>>(
                                //new TypedParameter(typeof(IConfigurationSection), readerconfig)
                                new TypedParameter(typeof(IConfigurationRoot), readerconfig)
                            );

                        IProtocolReader protocolReader = _protocols.FirstOrDefault(pr => pr.Metadata.ProtocolName.Equals(protocol))?.Value;
                        if (protocolReader == null) throw new ArgumentException(string.Format("ProtocolReader {0} is not supported.", protocol), "requestedProtocolReader");

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
        private static void RegisterFormatParsers(this ContainerBuilder builder)
        {            
            builder.RegisterType<DummyParser>().As<IFormatParser<string, Observation>>().SingleInstance().WithMetadata<ParserMetadata>(
                m => m.For(am => am.FormatName, "dummy")
                );

            builder.Register<IFormatParserFactory<string, Observation>>(
                (c, p) =>
                {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();

                    // Format parser factory function.
                    Func<string, IFormatParser<string, Observation>> rcode = (format) =>
                    {                        
                        IEnumerable<Lazy<IFormatParser<string, Observation>, ParserMetadata>> _formats = context.Resolve<IEnumerable<Lazy<IFormatParser<string, Observation>, ParserMetadata>>>(
                            new NamedParameter("format",(string) format)
                            );
                        IFormatParser<string, Observation> formatParser = _formats.FirstOrDefault(pr => pr.Metadata.FormatName.Equals(format))?.Value;
                        if (formatParser == null) throw new ArgumentException(string.Format("FormatParser {0} is not supported.", format), "format");

                        return formatParser;
                    };
                    return new FormatParserFactory<string, Observation>(_logger, rcode);
                }
                ).As<IFormatParserFactory<string, Observation>>().SingleInstance();           
        }

        /// <summary>
        /// Registers Router Factory.
        /// </summary>
        /// <param name="builder"></param>
        private static void RegisterRouterFactory(this ContainerBuilder builder)
        {

            // register queue implementation. Each deviceagent has one queue, shared by executable tasks in agent.
            builder.RegisterType<SimpleQueue<RouterMessage>>().As<IQueue<RouterMessage>>();

            builder.RegisterType<SimpleRouter>().As<IRouter>();

            // routes, temporary, later create from (optionally device) config 
            var routes = new RouteTable();
            routes.AddRoute("reader", "writer", null);
            //routes.AddRoute("filter", "writer", null);

            builder.RegisterInstance<RouteTable>(routes).SingleInstance();

            builder.Register<IRouterFactory>(
                (c, p) =>
                {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();

                    // function that creates new router.
                    Func<string, IRouter> rfactory = (agentname) =>
                    {

                        // function that creates new queue.
                        Func<string, IQueue<RouterMessage>> queueFactory = (queuename) =>
                        {
                            // get new queue instance. Or should get from pool. Question of implementation.                            
                            _logger.Debug(string.Format("Creating queue: '{0}'", queuename), () => { });
                            var q = context.Resolve<IQueue<RouterMessage>>(
                                new NamedParameter("queuename", (string)queuename)
                                );
                            return q;
                        };

                        
                        var router = context.Resolve<IRouter>(
                            new TypedParameter(typeof(string), agentname),
                            new TypedParameter(typeof(Func<string, IQueue<RouterMessage>>), queueFactory)
                        );
                        return router;
                    };
                    return new DefaultRouterFactory(_logger, rfactory);
                }).As<IRouterFactory>().SingleInstance();
        }
    }
}
