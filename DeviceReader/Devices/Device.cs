using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace DeviceReader.Devices
{

    public interface IDevice
    {
        string Id { get; }
        Task SendData(string data);
    }
    

    /// <summary>
    /// Device. Each device has its own IoT Hub client. 
    /// </summary>
    public class Device: IDevice
    {
        

        public string Id { get; private set; }

        private readonly IDeviceManager _deviceManager;
        private DeviceClient _deviceClient;

        // on deserialization, constructor is not being run. 
        public Device(string id, IDeviceManager deviceManager, DeviceClient deviceClient)
        {
            Id = id;
            _deviceManager = deviceManager;
            _deviceClient = deviceClient;
            _deviceClient.OpenAsync();
        }
        public async Task SendData(string data)
        {
            var telemetryDataPoint = new
            {
                eventtime = DateTime.UtcNow,
                deviceid = Id,
                message = data

            };
            var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
            var message = new Message(Encoding.ASCII.GetBytes(messageString));

            await _deviceClient.SendEventAsync(message);
        }

     
        
    }
}
