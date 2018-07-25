using System.Threading;
using System.Threading.Tasks;
using System.Collections;

using Newtonsoft.Json.Linq;
using DeviceReader.Devices;
using System;
using System.Collections.Generic;

namespace DeviceReader.Protocols
{
    /// <summary>
    /// Reading from device using protocol; 
    /// Since each agent should connect independently, probably using https poll, it should not be used as singleton
    /// </summary>
    public interface IProtocolReader: IDisposable
    {          
        Task<string> ReadAsync(CancellationToken cancellationToken);
        Task<string> ReadAsync(IDictionary<string, string> parameters, CancellationToken cancellationToken);
    }    

    public class ProtocolReaderMetadata
    {
        public string ProtocolName { get; set; }
    }

  
}
