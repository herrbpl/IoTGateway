using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DeviceReader.Protocols
{
    /// <summary>
    /// Reading from device using protocol; Suppose have to supply config, like IP address, port or smth? It goes to constructor.
    /// Return raw data? char array? does data need to be read from beginning to end for format processing?
    /// </summary>
    public interface IProtocolReader
    {
        Task<JObject> ReadAsync(CancellationToken cancellationToken);
    }    
}
