using System;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using CommandLine;

namespace EventHubConsumerTest
{


    class Options
    {

        [Option(Required = true, HelpText = "config file to use")]
        public string config { get; set; }

        
    }

    class Program
    {
        static string EventHubConnectionString;
        static string EventHubName;
        static string EventHubConsumerGroupName;
        static string StorageContainerName;
        static string StorageAccountName;
        static string StorageAccountKey;
        static string StorageConnectionString;
        


        static void Main(string[] args)
        {

            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(opts =>
            {

                try
                {

                    var confbuilder = new ConfigurationBuilder()
                        //.SetBasePath(env.ContentRootPath)
                        .AddJsonFile(opts.config, optional: false, reloadOnChange: true);
                        
                    //.AddEnvironmentVariables();
                    var Configuration = confbuilder.Build();


                    EventHubConnectionString = Configuration["eventhub_connectionstring"];
                    EventHubName = Configuration["eventhub_hubname"];
                    EventHubConsumerGroupName = Configuration["eventhub_consumergroupname"];
                    StorageContainerName = Configuration["az_storage_container"];
                    StorageAccountName = Configuration["az_storage_accountname"];
                    StorageAccountKey = Configuration["az_storage_accountkey"];
                    

                    StorageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", StorageAccountName, StorageAccountKey);

                    MainAsync(args).GetAwaiter().GetResult();

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
            }).WithNotParsed<Options>((errors) => {
                Console.WriteLine("Errors while running:");
                foreach (var err in errors)
                {
                    Console.WriteLine(err.ToString());
                }
            });

            Console.ReadLine();
           
        }

        private static async Task MainAsync(string[] args)
        {
            Console.WriteLine("Registering EventProcessor...");
            

            var eventProcessorHost = new EventProcessorHost(
                EventHubName,
                EventHubConsumerGroupName,
                EventHubConnectionString,
                StorageConnectionString,
                StorageContainerName);

            // Registers the Event Processor Host and starts receiving messages
            await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>();

            Console.WriteLine("Receiving. Press ENTER to stop worker.");
            Console.ReadLine();

            // Disposes of the Event Processor Host
            await eventProcessorHost.UnregisterEventProcessorAsync();
        }

    }


    public class SimpleEventProcessor : IEventProcessor
    {
        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");
            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            return Task.CompletedTask;
        }


        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");
                var jsonstr = JsonConvert.SerializeObject(eventData, Formatting.Indented);
                Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Event: '{jsonstr}'");
            }

            return context.CheckpointAsync();
        }
    }
}
