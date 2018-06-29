using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Diagnostics;


namespace DeviceReader.Models
{
    public enum sourceProtocol
    {
        MES14 = 1,
        MES16 = 2,
        HTTPS = 3
    }

    

    /// <summary>
    /// Specifies where data is read from
    /// </summary>
    public class DeviceSource
    {
        public string SourceIPAddress { get; set; }
        public int SourcePort { get; set; }
        // How to load class based on config string?
        // Source Protocol 
        public sourceProtocol SourceProtocol { get; set; }
        public int ReadingFrequency { get; set; }
    }

    /// <summary>
    /// Device. Each device has its own cancellation token source so that its can be stopped independently of other tasks. 
    /// </summary>
    public class Device
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DeviceSource Source { get; set; }


        // on deserialization, constructor is not being run. 
        public Device(string id, string name, DeviceSource source)
        {
            this.Id = id;
            this.Name = name;
            this.Source = source;
        }
        
    }
}
