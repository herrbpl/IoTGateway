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
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Binder;
using Microsoft.Extensions.Options;
using Autofac.Core;
using System.Threading.Tasks;

namespace DeviceReader.Extensions
{

    public static class DeviceReaderExtensions
    {

        public static void RegisterDeviceReaderServices(this ContainerBuilder builder, IConfigurationRoot appConfiguration)
        {
            RegisterProtocolReaders(builder);
            RegisterFormatParsers(builder);
            RegisterRouterFactory(builder);

            // need some validation for configuration
            DeviceManagerConfig dmConfig = new DeviceManagerConfig();
                        
            appConfiguration.GetSection("DeviceManager").Bind(dmConfig);

            //string connectionStr = appConfiguration.GetValue<string>("iothubconnectionstring", "");
            //string deviceManagerId = appConfiguration.GetValue<string>("devicemanagerid", "");

            //RegisterDeviceManager(builder, dmConfig, connectionStr, deviceManagerId);
            RegisterDeviceManager(builder, dmConfig);
            RegisterAgentFactory(builder);

        }

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

            builder.RegisterType<ME14ProtocolReader>().As<IProtocolReader>().WithMetadata<ProtocolReaderMetadata>(
                m => m.For(am => am.ProtocolName, "me14")
                );


            builder.Register<IProtocolReaderFactory>(
                (c,p) => {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();

                // Gets protocol reader for type
                    //Func<string, IConfigurationSection, IProtocolReader> rcode = (protocol, readerconfig) => {
                    Func<string, string, IConfigurationRoot, IProtocolReader> rcode = (protocol, rootpath, readerconfig) => {

                        IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>> _protocols = context.Resolve<IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>>>(
                                //new TypedParameter(typeof(IConfigurationSection), readerconfig)
                                new TypedParameter(typeof(string), rootpath),
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
        public static void RegisterFormatParsers(this ContainerBuilder builder)
        {            
            builder.RegisterType<DummyParser>().As<IFormatParser<string, Observation>>().SingleInstance().WithMetadata<ParserMetadata>(
                m => m.For(am => am.FormatName, "dummy")
                );

            builder.RegisterType<VaisalaXMLFormatParser>().As<IFormatParser<string, Observation>>().SingleInstance().WithMetadata<ParserMetadata>(
                m => m.For(am => am.FormatName, "vaisalaxml")
                );

            builder.Register<IFormatParserFactory<string, Observation>>(
                (c, p) =>
                {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();

                    // Format parser factory function.
                    Func<string, string, IConfigurationRoot, IFormatParser<string, Observation>> rcode = (format, rootpath, parserconfig) =>
                    {                        
                        IEnumerable<Lazy<IFormatParser<string, Observation>, ParserMetadata>> _formats = context.Resolve<IEnumerable<Lazy<IFormatParser<string, Observation>, ParserMetadata>>>(
                           // new NamedParameter("format",(string) format)
                            new TypedParameter(typeof(string), rootpath),
                            new TypedParameter(typeof(IConfigurationRoot), parserconfig)
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
        public static void RegisterRouterFactory(this ContainerBuilder builder)
        {

            // register queue implementation. Each deviceagent has one queue, shared by executable tasks in agent.
            builder.RegisterType<SimpleQueue<RouterMessage>>().As<IQueue<RouterMessage>>();

            builder.RegisterType<SimpleRouter>().As<IRouter>();

            // routes, temporary, later create from (optionally device) config 
            /*var routes = new RouteTable();
            routes.AddRoute("reader", "writer", null);
            */
            //routes.AddRoute("filter", "writer", null);

            //builder.RegisterInstance<RouteTable>(routes).SingleInstance();

            builder.Register<IRouterFactory>(
                (c, p) =>
                {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();

                    // function that creates new router.
                    Func<string, RouteTable, IRouter> rfactory = (agentname, routeTable) =>
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
                            new TypedParameter(typeof(RouteTable), routeTable),
                            new TypedParameter(typeof(Func<string, IQueue<RouterMessage>>), queueFactory)
                        );
                        return router;
                    };
                    return new DefaultRouterFactory(_logger, rfactory);
                }).As<IRouterFactory>().SingleInstance();
        }

        /// <summary>
        /// Registers device manager
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionString">IoT Hub owner Connection string</param>
        // private static void RegisterDeviceManager(this ContainerBuilder builder, DeviceManagerConfig config, string connectionString, string deviceManagerId)
        public static void RegisterDeviceManager(this ContainerBuilder builder, DeviceManagerConfig config)
        {
            builder.Register<IDeviceManager>(
              (c, p) =>
              {
                  ILogger _logger = c.Resolve<ILogger>();
                  IAgentFactory _agentFactory = c.Resolve<IAgentFactory>();

                  //DeviceManager dm = new DeviceManager(_logger, _agentFactory, config, connectionString, deviceManagerId);
                  DeviceManager dm = new DeviceManager(_logger, _agentFactory, config);
                  return dm;
              }).As<IDeviceManager>().SingleInstance();
        }


        /// <summary>
        /// Registers Agent Factory
        /// </summary>
        /// <param name="builder"></param>
        public static void RegisterAgentFactory(this ContainerBuilder builder)
        {
            // Agent Executables registration
            builder.RegisterType<DeviceAgentReader>().Keyed<IAgentExecutable>("reader");

            // register Agent writer executable
            builder.RegisterType<DeviceAgentWriter>().Keyed<IAgentExecutable>("writer");

            // register Agent
            builder.RegisterType<Agent>().As<IAgent>();


            // Register Device Agent Factory
            builder.Register<IAgentFactory>(
                (c, p) =>
                {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();
                    //IDeviceManager dm = c.Resolve<IDeviceManager>();

                    // function that creates new agent based on config given.
                    Func<string, IAgent> agentfunc = (agentconfig) =>
                    {                        
                        // create configuration from json string..
                        IConfigurationBuilder cb = new ConfigurationBuilder();

                        cb.AddJsonString(agentconfig);
                        var cbc = cb.Build();

                        var agentName = cbc.GetValue<string>("name");

                        if (!cbc.GetChildren().Any(cs => cs.Key == "executables"))
                        {
                            throw new ArgumentException("Config invalid, no 'executables' section");
                        }

                        // List of executable creation functions
                        Dictionary<string, Func<IAgent, IAgentExecutable>> agentExecutablesList = new Dictionary<string, Func<IAgent, IAgentExecutable>>();

                        // Test if executables section exists.                            
                        foreach (var item in cbc.GetSection("executables").GetChildren())
                        {
                            if (!context.IsRegisteredWithKey<IAgentExecutable>(item.Key))
                            {
                                throw new ArgumentException($"Invalid executable specification: '{item.Key}'");
                            }

                            // Function which returns agent executable
                            Func<IAgent, IAgentExecutable> aef = (dev) =>
                            {
                                IAgentExecutable r = context.ResolveKeyed<IAgentExecutable>(item.Key,
                                    new TypedParameter(typeof(IAgent), dev),
                                    new NamedParameter("name", item.Key),
                                    new ResolvedParameter(
                                            (pi, ctx) => pi.ParameterType == typeof(IDevice),
                                            (pi, ctx) => 
                                            {                                                
                                                IDeviceManager dm2 = ctx.Resolve<IDeviceManager>();
                                                // get IDevice from IDeviceManager by name
                                                // Since ResolvedParameter does not offer async method, it is run synchronous. This will become a bottleneck. 
                                                // TODO: There must be a way to load all devices in batch mode or smth.
                                                IDevice device = dm2.GetDevice<IDevice>(dev.Name);
                                                return device;
                                            }
                                        ),
                                     new ResolvedParameter(
                                            (pi, ctx) => pi.ParameterType == typeof(IWriter),
                                            (pi, ctx) =>
                                            {
                                                IDeviceManager dm2 = ctx.Resolve<IDeviceManager>();
                                                // get IDevice from IDeviceManager by name
                                                // Since ResolvedParameter does not offer async method, it is run synchronous. This will become a bottleneck. 
                                                // TODO: There must be a way to load all devices in batch mode or smth.
                                                _logger.Debug("Creating IWriter from device", () => { });
                                                IWriter device = dm2.GetDevice<IWriter>(dev.Name);
                                                return device;
                                            }
                                        )
                                    );
                                return r;
                            };

                            // add executable for agent
                            agentExecutablesList.Add(item.Key, aef);
                        }

                        // route table for agent

                        RouteTable rt = new RouteTable();

                        // check routes, if no routing table given, build one from executables. 
                        if (!cbc.GetChildren().Any(cs => cs.Key == "routes"))
                        {
                            // do nothing, empty routing table. Messages do not go anywhere
                        }
                        else
                        {
                            // build route table. Probably could build this using options binding but doint int oldfashionedly here.

                            
                            foreach (var source in cbc.GetSection("routes").GetChildren())
                            {
                                foreach (var target in source.GetChildren())
                                {
                                    var t = target.GetValue<string>("target");
                                    var e = target.GetValue<string>("evaluator");
                                    rt.AddRoute(source.Key, t, e);
                                }
                            }                            

                        }
                        
                        _logger.Debug(string.Format("Route table created:\n------------------------\n{0}------------------------", rt.ToString()), () => { });

                        // get router.
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
                            new TypedParameter(typeof(string), agentName),
                            new TypedParameter(typeof(RouteTable), rt),
                            new TypedParameter(typeof(Func<string, IQueue<RouterMessage>>), queueFactory)
                        );


                        var agent = context.Resolve<IAgent>(
                            new TypedParameter(typeof(IConfigurationRoot), cbc),
                            new TypedParameter(typeof(IRouter), router),
                            new TypedParameter(typeof(Dictionary<string, Func<IAgent, IAgentExecutable>>), agentExecutablesList)
                            );
                        return agent;
                    };

                    return new AgentFactory(_logger, agentfunc);
                }).As<IAgentFactory>().SingleInstance();
        }
    }
}
