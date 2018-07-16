using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;


namespace DeviceReader.Devices
{

    public interface IDevice
    {
        string Id { get; }
        DeviceConfig Config { get; set; }
    }
    

    /// <summary>
    /// Device. Each device has its own cancellation token source so that its can be stopped independently of other tasks. 
    /// </summary>
    public class Device: IDevice
    {
        DeviceConfig _deviceConfig;

        public string Id { get; set; }
        public string Name { get; set; }        
        public DeviceConfig Config { get => _deviceConfig; set => _deviceConfig = value; }
        


        // on deserialization, constructor is not being run. 
        public Device(string id, string name)
        {
            this.Id = id;
            this.Name = name;
            this._deviceConfig = new DeviceConfig
            {
                DeviceId = id
                , Direction = SourceDirection.PULL
                , Port = 5000
                , Host = "localhost"
                , ProtocolReader = "http"
                , PollFrequency = 3
                , IotHubConnectionString = ""
                , UserName ="user"
                , Password = "password"
                , FormatParser = "dummy"
            };
        }
        
    }
}
