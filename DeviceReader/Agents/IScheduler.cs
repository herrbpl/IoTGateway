using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Agents
{
    public enum SchedulerType
    {
        /// <summary>
        /// Wait between runs is constant and starts after previous run has finished. Time grain is millisecond.
        /// </summary>
        FREQUENCY_EXCLUSIVE = 1,
        /// <summary>
        /// Wait between runs is depending on how long previous task executed, time span is constant betweem two starttimes of consenquntial tasks. Time grain is millisecond.
        /// </summary>
        FREQUENCY_INCLUSIVE = 2,
        /// <summary>
        /// Cron scheduler is used to schedule tasks but only one task can run at one time. Time grain is minute.
        /// </summary>
        CRON      = 3
    }

    public interface IScheduler
    {        
        SchedulerType SchedulerType { get; }
        
        Task RunAsync(CancellationToken ct);
    }
}
