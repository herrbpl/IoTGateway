using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Devices
{
    /// <summary>
    /// Device Agent Executable. Agent Executable can do different things, mostly reading (polling) from input and writing to output (IoT Hub).
    /// </summary>
    public interface IDeviceAgentExecutable
    {
        /// <summary>
        /// Runs Agent Executable. 
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RunAsync(CancellationToken ct);
    }
}
