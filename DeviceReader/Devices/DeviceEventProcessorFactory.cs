using DeviceReader.Diagnostics;
using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Devices
{
    class DeviceEventProcessorFactory : IEventProcessorFactory
    {
        readonly ILogger _logger;
        readonly DeviceManager _deviceManager;

        public DeviceEventProcessorFactory(ILogger logger, DeviceManager deviceManager)
        {
            _logger = logger;
            _deviceManager = deviceManager;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new DeviceEventProcessor(_logger, _deviceManager);
        }
    }
}
