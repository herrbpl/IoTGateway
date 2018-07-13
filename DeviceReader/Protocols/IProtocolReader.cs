using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using DeviceReader.Devices;

namespace DeviceReader.Protocols
{
    /// <summary>
    /// Reading from device using protocol; Suppose have to supply config, like IP address, port or smth? It goes to constructor.
    /// Return raw data? char array? does data need to be read from beginning to end for format processing?
    /// </summary>
    public interface IProtocolReader
    {
        Task<string> ReadAsync(CancellationToken cancellationToken);
    }    


    public interface IProtocolReaderFactory
    {
        IProtocolReader GetProtocolReader(SourceProtocol protocol);        // should we get reader based on class or something else? 
    }
}
