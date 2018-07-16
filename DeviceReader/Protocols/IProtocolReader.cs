using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using DeviceReader.Devices;
using System;

namespace DeviceReader.Protocols
{
    /// <summary>
    /// Reading from device using protocol; Suppose have to supply config, like IP address, port or smth? It goes to constructor.
    /// Return raw data? char array? does data need to be read from beginning to end for format processing?
    /// </summary>
    public interface IProtocolReader: IDisposable
    {        
        Task<string> ReadAsync(CancellationToken cancellationToken);
    }    

    public class ProtocolReaderMetadata
    {
        public string ProtocolName { get; set; }
    }

  
}
