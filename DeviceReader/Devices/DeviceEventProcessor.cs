using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using DeviceReader.Diagnostics;
//using Microsoft.ServiceBus.Messaging;

namespace DeviceReader.Devices
{
    /// <summary>
    /// For testing, see https://stackoverflow.com/questions/38105679/eventhub-partitioncontext-class-design 
    /// https://blogs.msdn.microsoft.com/servicebus/2015/01/16/event-processor-host-best-practices-part-1/
    /// </summary>
    class DeviceEventProcessor : IEventProcessor
    {
        readonly DeviceManager _deviceManager;
        readonly ILogger _logger;

        public DeviceEventProcessor(ILogger logger, DeviceManager deviceManager)
        {
            _logger = logger;
            _deviceManager = deviceManager;
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            _logger.Info($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.", () => { });
            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            _logger.Info($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'", () => { });
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            _logger.Info($"Error on Partition: {context.PartitionId}, Error: {error.Message}", () => { });
            return Task.CompletedTask;
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {                
                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                try
                {
                    await _deviceManager.OnDeviceEvent(eventData);
                } catch (Exception e)
                {
                    _logger.Error($"Error while processing message: {e}", () => { });
                }                                
            }            
            await context.CheckpointAsync();
        }
        
        
    }
}
