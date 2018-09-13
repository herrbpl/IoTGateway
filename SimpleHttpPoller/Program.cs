using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Authentication;
using DeviceReader.Diagnostics;
using Autofac;
using Microsoft.Extensions.Configuration;

namespace SimpleHttpPoller
{

    class Options
    {

        [Option(Required = true, HelpText = "URL to test")]
        public string url { get; set; }

        [Option(Required = false, HelpText = "username for basic authentication")]
        public string username { get; set; }

        [Option(Required = false, HelpText = "passdword for basic authentication")]
        public string password { get; set; }       
    }


    class Program
    {

        static ILogger logger;
        //static IDeviceAgentRunnerFactory runnerFactory;

        private static IContainer Container; // { get; set; }

        static string AgentConfigTemplate = $@"
{{
    'name': 'httpagenttest',
    'executables': {{ 
        'reader': {{            
            'format':'dummy',
            'protocol':'http',
            'protocol_config': #CONFIG#,
            'frequency': 1           
        }},
        'writer': {{            
            'frequency': 10000
        }}
    }},
    'routes': {{
        'reader': {{ 
            'writer': {{
                'target': 'writer',
                'evaluator': ''
            }}
        }}                         
    }}
}}
";

        static void Main(string[] args)
        {

            Options options = new Options();
            string configString = "";
           
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(opts =>
            {

                options.url = opts.url;
                options.username = opts.username;
                options.password = opts.password;

                string configString2 = $@"{{
    'Url': '{opts.url}',
    'Username': '{opts.username}',
    'Password': '{opts.password}'
}}";

                configString = AgentConfigTemplate.Replace("#CONFIG#", configString2);

                
            }).WithNotParsed<Options>((errors) => {
                Console.WriteLine("Errors while running:");
                foreach(var err in errors)
                {
                    Console.WriteLine(err.ToString());
                }
            });


            Console.WriteLine(configString);


            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var confbuilder = new ConfigurationBuilder()
                //.SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true);
            //.AddEnvironmentVariables();
            /*

            try
            {

                // https://stackoverflow.com/questions/42746190/https-request-fails-using-httpclient
                var handler = new HttpClientHandler();
                handler.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    Console.WriteLine("Got cert {0}", cert.ToString());
                    return true;

                };

                HttpClient client = new HttpClient(handler);

                var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", opts.username, opts.password));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                HttpResponseMessage response = client.GetAsync(opts.url).Result;

                Console.WriteLine("Status: {0}", response.StatusCode);
                foreach (var header in response.Headers)
                {
                    Console.WriteLine("{0}: {1}", header.Key, header.Value);
                }

                string content = "";
                if (response.Content != null)
                {
                    content = response.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(content);
                }
            }
            catch (AggregateException e)
            {
                Console.WriteLine("Error while executing {0}", e.Message);
                foreach (var ex in e.Flatten().InnerExceptions)
                {
                    Console.WriteLine(ex.InnerException);
                }
            }

            catch (Exception e)
            {
                Console.WriteLine("Error while executing {0}", e.Message);
            }
            */

            Console.ReadLine();
        }

        static async Task<string> PollStation(string url, string username, string password)
        {
            var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", username, password));

            return "";
        }

    }
}
