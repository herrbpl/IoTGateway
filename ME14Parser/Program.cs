using DeviceReader.Diagnostics;
using DeviceReader.Models;
using DeviceReader.Parsers;
using System;
using System.Diagnostics;
using System.Threading;

namespace ME14Parser
{
    class Program
    {


        static void Main(string[] args)
        {
            LoggingConfig lg = new LoggingConfig();
            ILogger logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);
            IFormatParser<string, Observation> dummyparser = new DummyParser(logger);
            var result = dummyparser.ParseAsync("This is a string", CancellationToken.None).Result;

            foreach (var item in result)
            {
                Console.WriteLine(item.DeviceId);
            }
            
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }
}
