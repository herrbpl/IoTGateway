using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeviceReader.Models;

namespace DeviceReader
{
    public interface IDeviceRunner
    {
        Task RunAsync();
        void Stop(); 
    }
    /// <summary>
    /// Runs device agents. 
    /// </summary>
    class DeviceRunner : IDeviceRunner
    {
        private Dictionary<string, IDeviceAgent> _agents;
        private CancellationTokenSource _cts;

        public DeviceRunner()
        {
            _agents = new Dictionary<string, IDeviceAgent>();
            _cts = new CancellationTokenSource();
        }

        public async Task RunAsync()
        {
            while (!this._cts.Token.IsCancellationRequested)
            {
                
                await Task.Delay(TimeSpan.FromSeconds(2), this._cts.Token);
            }
        }

        public void Stop()
        {
            this._cts.Cancel();
        }
    }
}
