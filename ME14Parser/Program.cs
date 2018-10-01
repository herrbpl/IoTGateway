using DeviceReader.Diagnostics;
using DeviceReader.Models;
using DeviceReader.Parsers;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ME14Parser
{
    class Program
    {

        public const string KEY_AGENT_EXECUTABLE_ROOT = "executables:reader";
        public const string KEY_AGENT_FORMAT_CONFIG = KEY_AGENT_EXECUTABLE_ROOT + ":format_config";

        static void Main(string[] args)
        {


            LoggingConfig lg = new LoggingConfig();
            ILogger logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);


            var dict = new Dictionary<string, string>
            {
                {KEY_AGENT_FORMAT_CONFIG+":SchemaPath", "notexistingfile.json"}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(dict)
                .Build();            

            //IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, KEY_AGENT_FORMAT_CONFIG, config);
            IFormatParser<string, Observation> dummyparser = new DeviceReader.Parsers.ME14Parser(logger, null, null);

            try
            {
                var result = dummyparser.ParseAsync("This is a string", CancellationToken.None).Result;
                foreach (var item in result)
                {
                    Console.WriteLine(item.DeviceId);
                }
            } catch (Exception e)
            {
                //Console.WriteLine(e);
            }
            
                        
            Console.ReadLine();
        }
    }
}
