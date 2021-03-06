﻿using System;
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
using Autofac.Core;
using DeviceReader.Configuration;
using Newtonsoft.Json;

namespace DeviceReader.Extensions
{
    /// <summary>
    /// Extensions for ContainerBuilder. 
    /// TODO: move specific implementation registrations outside of this class library. So user can decide which one to use.
    /// </summary>
    public static class DeviceReaderExtensions
    {
        internal const string KEY_GLOBAL_APP_CONFIG = "GlobalAppConfig";
        internal const string KEY_DEVICE_CONFIGURATION_PROVIDERS = "DeviceConfigurationProviders:";
        internal const string KEY_DEVICE_CONFIGURATION_DEFAULT = "DeviceConfigurationProviderDefault";


        public static void RegisterDeviceReaderServices(this ContainerBuilder builder, IConfiguration appConfiguration)
        {
            // Register application configuration intance.
            builder.RegisterInstance<IConfiguration>(appConfiguration).Keyed<IConfiguration>(KEY_GLOBAL_APP_CONFIG).SingleInstance();

            RegisterDeviceConfigurationProviders(builder);
            RegisterProtocolReaders(builder);
            RegisterFormatParsers(builder);
            RegisterRouterFactory(builder);            
            RegisterDeviceManager(builder);
            RegisterAgentFactory(builder);
            

        }

       

        public static void RegisterDeviceConfigurationProviders(this ContainerBuilder builder)
        {

            // Register configuration loader
            builder.RegisterType<DeviceConfigurationLoader>().As<IDeviceConfigurationLoader>();

            // Register dummy configuration provider.
            // for this, extra method should be.
            builder.RegisterType<DeviceConfigurationDummyProvider>().
                As<IDeviceConfigurationProvider>().
                WithMetadata<DeviceConfigurationProviderMetadata>(
                   m => m.
                        For(am => am.ProviderName, "dummy").
                        For(am => am.OptionsType, typeof(DeviceConfigurationDummyProviderOptions)).
                        For(am => am.GlobalConfigurationKey, KEY_DEVICE_CONFIGURATION_PROVIDERS+":dummy")

                   ).
                ExternallyOwned();

            builder.RegisterType<DeviceConfigurationAzureTableProvider>().
               As<IDeviceConfigurationProvider>().
               WithMetadata<DeviceConfigurationProviderMetadata>(
                  m => m.
                       For(am => am.ProviderName, "azuretable").
                       For(am => am.OptionsType, typeof(DeviceConfigurationAzureTableProviderOptions)).
                       For(am => am.GlobalConfigurationKey, KEY_DEVICE_CONFIGURATION_PROVIDERS + ":azuretable")

                  ).
               ExternallyOwned();

            builder.Register<IDeviceConfigurationProviderFactory>(
                (c,p) => {

                    IComponentContext context = c.Resolve<IComponentContext>();

                    // function that creates configuration providers
                    Func<string, string, IDeviceConfigurationProvider> func = (provider, providerconfig) => {

                        // here we should also resolve for appconfig to get configurationprovider defaults;
                        // to create new config object we can try:
                        // https://stackoverflow.com/questions/981330/instantiate-an-object-with-a-runtime-determined-type
                        //

                        // resolve options type.
                        // create new option if providerconfig is null
                        // try to load app configuration and bind option from app configuration
                        // if that fails, pass null.

                        ILogger _logger = context.Resolve<ILogger>();
                        object configOptions = null;
                        IConfiguration appconfig = null;

                        if (context.IsRegisteredWithKey<IConfiguration>(KEY_GLOBAL_APP_CONFIG))
                        {
                            appconfig = context.ResolveKeyed<IConfiguration>(KEY_GLOBAL_APP_CONFIG);
                            if (appconfig == null)
                            {
                                _logger.Warn($"Application configuration instance not registered with DI, cannot look up configuration.", () => { });
                            }
                        }

                        
                        // try to look up if at least provider exist

                        var meta = context.Resolve<IEnumerable<Lazy<IDeviceConfigurationProvider, DeviceConfigurationProviderMetadata>>>(
                                    new NamedParameter("options", null)
                                        ).FirstOrDefault(pr => pr.Metadata.ProviderName.Equals(provider))?.Metadata;

                        // if meta is null and appconfig is null, we can give up or try to provide dummy. It is probably cleaner to throw..
                                                
                        if (meta == null)
                        {
                            _logger.Warn($"Specified provider {provider} does not exist, trying to use default.", () => { });
                            if (appconfig == null) {
                                _logger.Error($"Specified provider {provider} does not exist and cannot look up configuration", () => { }) ;
                                throw new ArgumentException("provider");
                            } else
                            {
                                // try to look up default
                                
                                var defaultprovider = appconfig.GetValue<string>(KEY_DEVICE_CONFIGURATION_DEFAULT);

                                if (defaultprovider == null)
                                {
                                    _logger.Error($"Specified provider {provider} does not exist and default provider is not specified", () => { });
                                    throw new ArgumentException("provider");
                                }
                                
                                meta = context.Resolve<IEnumerable<Lazy<IDeviceConfigurationProvider, DeviceConfigurationProviderMetadata>>>(
                                    new NamedParameter("options", null)
                                        ).FirstOrDefault(pr => pr.Metadata.ProviderName.Equals(defaultprovider))?.Metadata;

                                if (meta != null)
                                {
                                    _logger.Warn($"Using default provider '{defaultprovider}' instead of '{provider}'", () => { });
                                    provider = defaultprovider;
                                }

                            }
                        }

                        if (meta != null && meta.OptionsType != null)
                        {
                            configOptions = Activator.CreateInstance(meta.OptionsType);



                            if (providerconfig == null)
                            {
                                // check if registration exists
                                if (appconfig != null)
                                {
                                    try
                                    {
                                        var cs = appconfig.GetSection(KEY_DEVICE_CONFIGURATION_PROVIDERS +  meta.ProviderName);
                                        //&var cs = appconfig.GetSection(meta.GlobalConfigurationKey);
                                        cs.Bind(configOptions);
                                    }
                                    catch (Exception e)
                                    {
                                        _logger.Warn($"No options section {KEY_DEVICE_CONFIGURATION_PROVIDERS + meta.ProviderName} found in configuration or it has invalid data: {e}", () => { });
                                    }
                                } 
                            } else
                            {
                                    // try to fill object with deserialization
                                    JsonConvert.DeserializeObject(providerconfig, meta.OptionsType);
                            }
                        }

                        IEnumerable<Lazy<IDeviceConfigurationProvider, DeviceConfigurationProviderMetadata>> _configproviders
                        = context.Resolve<IEnumerable<Lazy<IDeviceConfigurationProvider, DeviceConfigurationProviderMetadata>>>(
                            new NamedParameter("options", configOptions)
                                /*
                                // try if provider is given
                                new ResolvedParameter(
                                      (pi, ctx) => (pi.ParameterType == typeof(string) && providerconfig != null), // what is actu
                                      (pi, ctx) => { return providerconfig; }
                                      ),
                                // try with object
                                new ResolvedParameter(
                                      (pi, ctx) => (providerconfig == null && configOptions != null), // what is actu
                                      (pi, ctx) => { return configOptions; }
                                      ),
                                new ResolvedParameter(
                                      (pi, ctx) => (providerconfig == null && configOptions == null), // what is actu
                                      (pi, ctx) => { return null; }
                                      )
                                */
                              );

                            // how can we create correct object type?
                            IDeviceConfigurationProvider configurationProvider = _configproviders.FirstOrDefault(pr => pr.Metadata.ProviderName.Equals(provider))?.Value;
                            if (configurationProvider == null) throw new ArgumentException($"Configuration provider {provider} is not supported.", "provider");

                            return configurationProvider;
                    };

                    return new DeviceConfigurationProviderFactory(func);
                }).As<IDeviceConfigurationProviderFactory>().SingleInstance();
        }

        /// <summary>
        /// Registers protocol readers for DeviceAgentRunner.         
        /// </summary>
        /// <param name="builder"></param>
        public static void RegisterProtocolReaders(this ContainerBuilder builder)
        {
            // TODO: check if need to be externally owned.
            // AUtoregister all implemented interfaces? Something better later than using simple text.
            builder.RegisterType<DummyProtocolReader>().As<IProtocolReader>().WithMetadata<ProtocolReaderMetadata>(
                m => m.For(am => am.ProtocolName, "dummy")
                );
            builder.RegisterType<HttpProtocolReader>().As<IProtocolReader>().WithMetadata<ProtocolReaderMetadata>(
                m => m.For(am => am.ProtocolName, "http")
                );

            builder.RegisterType<VaisalaHttpProtocolReader>().As<IProtocolReader>().WithMetadata<ProtocolReaderMetadata>(
                m => m.For(am => am.ProtocolName, "vaisalahttp")
                );

            builder.RegisterType<ME14ProtocolReader>().As<IProtocolReader>().WithMetadata<ProtocolReaderMetadata>(
                m => m.For(am => am.ProtocolName, "me14")
                );


            builder.Register<IProtocolReaderFactory>(
                (c, p) => {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();

                    // Gets protocol reader for type
                    //Func<string, IConfigurationSection, IProtocolReader> rcode = (protocol, readerconfig) => {
                    Func<string, string, IConfiguration, IProtocolReader> rcode = (protocol, rootpath, readerconfig) => {

                        IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>> _protocols = context.Resolve<IEnumerable<Lazy<IProtocolReader, ProtocolReaderMetadata>>>(
                                //new TypedParameter(typeof(IConfigurationSection), readerconfig)
                                new TypedParameter(typeof(string), rootpath),
                                new TypedParameter(typeof(IConfiguration), readerconfig)
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

            builder.RegisterType<ME14Parser>().As<IFormatParser<string, Observation>>().SingleInstance().WithMetadata<ParserMetadata>(
               m => m.For(am => am.FormatName, "me14")
               );

            builder.Register<IFormatParserFactory<string, Observation>>(
                (c, p) =>
                {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();

                    // Format parser factory function.
                    Func<string, string, IConfiguration, IFormatParser<string, Observation>> rcode = (format, rootpath, parserconfig) =>
                    {                        
                        IEnumerable<Lazy<IFormatParser<string, Observation>, ParserMetadata>> _formats = context.Resolve<IEnumerable<Lazy<IFormatParser<string, Observation>, ParserMetadata>>>(
                           // new NamedParameter("format",(string) format)
                            new TypedParameter(typeof(string), rootpath),
                            new TypedParameter(typeof(IConfiguration), parserconfig)
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

            // register queue implementation. Each deviceagent executable has one queue.
            builder.RegisterType<SimpleQueue<RouterMessage>>().As<IQueue<RouterMessage>>().ExternallyOwned();

            builder.RegisterType<SimpleRouter>().As<IRouter>().ExternallyOwned();
            
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
        public static void RegisterDeviceManager(this ContainerBuilder builder)
        {
            builder.RegisterType<DeviceManager>().As<IDeviceManager>().SingleInstance();
        }


        /// <summary>
        /// Registers Agent Factory
        /// </summary>
        /// <param name="builder"></param>
        public static void RegisterAgentFactory(this ContainerBuilder builder)
        {

            // register Agent inbound message receiver (Pushed) executable
            builder.RegisterType<DeviceAgentZero>().Keyed<IAgentExecutable>("zeroagent").ExternallyOwned();

            // Agent Executables registration
            builder.RegisterType<DeviceAgentReader>().Keyed<IAgentExecutable>("reader").ExternallyOwned();

            // register Agent writer executable
            builder.RegisterType<DeviceAgentWriter>().Keyed<IAgentExecutable>("writer").ExternallyOwned();

            // register Agent inbound message receiver (Pushed) executable
            builder.RegisterType<DeviceAgentPushReceiver>().Keyed<IAgentExecutable>("pushreceiver").ExternallyOwned();

            // register Agent
            builder.RegisterType<Agent>().As<IAgent>().ExternallyOwned();


            // Register Device Agent Factory
            builder.Register<IAgentFactory>(
                (c, p) =>
                {
                    ILogger _logger = c.Resolve<ILogger>();
                    IComponentContext context = c.Resolve<IComponentContext>();
                    //var ocontext = c.Resolve<ILifetimeScope>();

                    //IDeviceManager dm = c.Resolve<IDeviceManager>();

                    // function that creates new agent based on config given.
                    Func<IConfiguration, IAgent> agentfunc = (agentconfig) =>
                    {
                        IAgent agent = null;


                        // create configuration from json string..
                        /*
                        IConfigurationBuilder cb = new ConfigurationBuilder();

                        cb.AddJsonString(agentconfig);
                        var cbc = cb.Build();
                        */
                        var cbc = agentconfig;
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

                            // get agent executable type.

                            var executabletype = item.GetValue<string>("type");
                            if (executabletype == null)
                            {
                                throw new ArgumentException($"Missing executable type: '{executabletype}'");
                            }

                            if (!context.IsRegisteredWithKey<IAgentExecutable>(executabletype))
                            {
                                throw new ArgumentException($"Invalid executable specification: '{executabletype}'");
                            }
                           
                            // Function which returns agent executable
                            Func<IAgent, IAgentExecutable> aef = (dev) =>
                            {
                                
                                //IAgentExecutable r = context.ResolveKeyed<IAgentExecutable>(item.Key,
                                IAgentExecutable r = context.ResolveKeyed<IAgentExecutable>(executabletype,                                    
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

                        //var agent = context.Resolve<IAgent>(
                        agent = context.Resolve<IAgent>(
                            new TypedParameter(typeof(IConfiguration), cbc),
                            new TypedParameter(typeof(IRouter), router),
                            new TypedParameter(typeof(Dictionary<string, Func<IAgent, IAgentExecutable>>), agentExecutablesList)
                            );
                            
                        agentExecutablesList = null;
                       
                        return agent;
                    };

                    return new AgentFactory(_logger, agentfunc);
                }).As<IAgentFactory>().SingleInstance();
        }
    }
}
