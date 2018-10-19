using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CommandLine;

namespace DeviceReader.WebService
{

    class StartOptions
    {

        [Option(Required = false, HelpText = "Only dump config and exit", Default = false)]
        public bool configdump { get; set; }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            
            bool configOnly = false;
            bool exitFromOptions = false;

            Parser.Default.ParseArguments<StartOptions>(args).WithParsed<StartOptions>(opts =>
            {
                configOnly = opts.configdump;

            }).WithNotParsed<StartOptions>((errors) =>
            {
                Console.WriteLine("Invalid program arguments:");
                foreach (var err in errors)
                {
                    Console.WriteLine(err.ToString());
                }
                exitFromOptions = true;
            });

            if (exitFromOptions) return;

            // config file.
            /// very important!
            /// This implementation currently has memory leak which is related to dotnetty issue
            /// https://github.com/Azure/DotNetty/issues/344
            /// So its not my bad code that leaks it..
            Environment.SetEnvironmentVariable("io.netty.allocator.type", "unpooled");
            // CreateWebHostBuilder(args).Build().Run();
            // we need config for port information

            // TODO: This part should be separate class?

            // OK this might not be needed?
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var confbuilder = new ConfigurationBuilder()
                //.SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("IOTGW_")
                .AddCommandLine(args);
            
            var config = confbuilder.Build();

            var configcheck = CheckConfig(config);

            if (configOnly || configcheck.Count > 0)
            {
                Console.WriteLine("Configuration:");
                foreach (var item in config.AsEnumerable())
                {                    
                    if (configcheck.ContainsKey(item.Key))
                    {
                        Console.WriteLine($"Error: {item.Key} {configcheck[item.Key]}");
                        configcheck.Remove(item.Key);
                    }
                    Console.WriteLine($"{item.Key} = {item.Value}");
                }

                foreach (var item in configcheck)
                {
                    Console.WriteLine($"Error: {item.Key} {configcheck[item.Key]}");                    
                }

                return;
            }

            /*
           Kestrel is a cross-platform HTTP server based on libuv,
           a cross-platform asynchronous I/O library.
           https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers
           */
            var host = new WebHostBuilder()
                .UseKestrel(options => { options.AddServerHeader = false;

                    // example of using configuration to configure Kestrel. Probably easiest to leave to defaults.
                    options.Configure(config);

                })
                // example of how to configure application and pass config instance down to Startup class, instead of creating confguration in Startup.
                .ConfigureAppConfiguration((hostingContext, appconfig) => {
                    
                    appconfig.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables("IOTGW_")
                    .AddCommandLine(args);
                })
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();

        }

        // check that important parts of configuration are existing
        private static Dictionary<string,string> CheckConfig(IConfigurationRoot config)
        {
            var result = new Dictionary<string, string>();

            List<string> requiredConfigValues = new List<string>()
            {
                "DeviceManager:DeviceManagerId",
                "DeviceManager:IotHub:ConnectionString",
                "DeviceManager:EventHub:ConnectionString",
                "DeviceManager:EventHub:HubName",
                "DeviceManager:EventHub:ConsumerGroup",
                "DeviceManager:EventHub:AzureStorageContainer",
                "DeviceManager:EventHub:AzureStorageAccountName",
                "DeviceManager:EventHub:AzureStorageAccountKey",
                "DeviceConfigProvider:Type",
                "DeviceConfigProvider:Config:ConnectionString",
                "DeviceConfigProvider:Config:ConfigTableName"
            };

            var lookup = config.AsEnumerable().ToDictionary((i) => { return i.Key; });
            foreach (var item in requiredConfigValues)
            {

                if (!lookup.ContainsKey(item))
                {
                    result.Add(item, "is missing!");
                } else if (lookup[item].Value == null || lookup[item].Value == "") 
                {
                    result.Add(item, "has no value defined!");
                }
            }
            return result;
        }

        /*
public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
   WebHost.CreateDefaultBuilder(args)
       .UseStartup<Startup>();
*/

    }
}
