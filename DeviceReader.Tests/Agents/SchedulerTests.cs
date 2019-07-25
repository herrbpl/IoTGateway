using DeviceReader.Agents;
using DeviceReader.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;


namespace DeviceReader.Tests.Agents
{    
    public class SchedulerTests
    {
        private readonly ITestOutputHelper output;
        ILogger logger;
        public SchedulerTests(ITestOutputHelper output)
        {
            LoggingConfig lg = new LoggingConfig();
            lg.LogLevel = LogLevel.Debug;

            Logger.DefaultLoggerFactory.AddProvider(new XUnitLoggerProvider(output));
            logger = new Logger(Process.GetCurrentProcess().Id.ToString(), lg);
        }
        object _lock = new object();
        int counter = 0;

        private async Task Run (CancellationToken ct)
        {
            logger.Info($"Starting to increase count", () => { });
            lock (_lock) {
                counter++;                
            }
            logger.Info($"Executing run, count {counter}", () => { });            
        }

        [Fact]
        void SequentialTimer_Should_Execute_Interval()
        {
            counter = 0;
            IScheduler scheduler = new SchedulerSequentialTimer(5000,  (ct) => Run(ct), null, (e) => { logger.Error($"{e}", () => { }); return false; } );
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(15050);
            try
            {
                scheduler.RunAsync(cancellationTokenSource.Token).Wait();
            } catch( Exception e) { }
            Assert.InRange<int>(counter, 3, 4);
        }
    }
}
