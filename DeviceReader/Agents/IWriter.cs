using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Agents
{
    public interface IWriter
    {
        bool Connected { get; }

        /// <summary>
        /// Sends data to upstream. 
        /// </summary>
        /// <param name="data">Data to be sent, most often JSON string. It is caller responsibility to ensure upstream understand format.</param>
        /// <param name="properties">Properties to include with message.</param>
        /// <returns></returns>
        Task SendAsync(string data, Dictionary<string, string> properties);
    }
}
