using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceReader.Devices
{
    public interface IStorageAdapter
    {
        Task<Dictionary<string, string>> ListAsync(string queryString);
    }
    // Dummy storage
    class StorageAdapter : IStorageAdapter
    {
        private Dictionary<string, string> devices; // deviceId=> config 

        public StorageAdapter()
        {
            this.devices = new Dictionary<string, string>();
        }

        public async Task<Dictionary<string, string>> ListAsync(string queryString)
        {
            // just return 
            return devices;
        }

    }
}
