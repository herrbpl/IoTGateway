using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Agents
{
    public interface IWriter
    {
        bool Connected { get; }
        Task SendAsync(byte[] message, Dictionary<string, string> properties);
    }
}
