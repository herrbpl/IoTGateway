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
        IDeviceConfig Config { get;  }
    }
    

    /// <summary>
    /// Device. Each device has its own cancellation token source so that its can be stopped independently of other tasks. 
    /// </summary>
    public class Device: IDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }        
        public IDeviceConfig Config { get => null; }



        // on deserialization, constructor is not being run. 
        public Device(string id, string name)
        {
            this.Id = id;
            this.Name = name;            
        }
        
    }
}
