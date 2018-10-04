using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DeviceReader.WebService
{
    public class Program
    {
        public static void Main(string[] args)
        {

            // config file.

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
        /*
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
        */
        
    }
}
