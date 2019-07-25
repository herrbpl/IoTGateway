using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Agents
{
    public interface IScheduler
    {        
        Task RunAsync(CancellationToken ct);
    }
}
