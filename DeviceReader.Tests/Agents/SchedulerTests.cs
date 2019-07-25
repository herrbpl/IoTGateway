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

        private async Task Run (CancellationToken ct, int waitTime)
        {
            logger.Info($"Starting to increase count", () => { });
            lock (_lock) {
                counter++;                
            }
            logger.Info($"Executing run, count {counter}", () => { });        
            if (waitTime > 0)
            {
                await Task.Delay(waitTime);
            }
        }

        [Fact]
        void SequentialTimer_Should_Execute_Interval()
        {
            counter = 0;
            IScheduler scheduler = new Scheduler("5000",  (ct) => Run(ct,0), null, (e) => { logger.Error($"{e}", () => { }); return false; } );
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(15050);
            
            scheduler.RunAsync(cancellationTokenSource.Token).Wait();
            
            Assert.InRange<int>(counter, 3, 4);
        }


        [Fact]
        void CronScheduler_Should_Execute_2_times()
        {
            counter = 0;
            IScheduler scheduler = new Scheduler("* * * * *", (ct) => Run(ct, 2000), null, (e) => { logger.Error($"{e}", () => { }); return false; });
            logger.Info($"Type of scheduler is '{scheduler.SchedulerType}'", () => { });
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(70000);

            scheduler.RunAsync(cancellationTokenSource.Token).Wait();

            Assert.Equal(2, counter);
        }


        [Fact]
        public void Should_Choose_Scheduler()
        {
            List<Tuple<string, SchedulerType, Type>> cases = new List<Tuple<string, SchedulerType, Type>>()
            {
                new Tuple<string, SchedulerType, Type>("1000", SchedulerType.FREQUENCY_EXCLUSIVE, null),
                new Tuple<string, SchedulerType, Type>("E1000", SchedulerType.FREQUENCY_EXCLUSIVE, null),
                new Tuple<string, SchedulerType, Type>("I1000", SchedulerType.FREQUENCY_INCLUSIVE, null),
                new Tuple<string, SchedulerType, Type>("e1000", SchedulerType.FREQUENCY_EXCLUSIVE, null),
                new Tuple<string, SchedulerType, Type>("i1000", SchedulerType.FREQUENCY_INCLUSIVE, null),
                new Tuple<string, SchedulerType, Type>(" e1000 ", SchedulerType.FREQUENCY_EXCLUSIVE, null),
                new Tuple<string, SchedulerType, Type>(" i1000 ", SchedulerType.FREQUENCY_INCLUSIVE, null),
                new Tuple<string, SchedulerType, Type>("* * * * *", SchedulerType.CRON, null),
                new Tuple<string, SchedulerType, Type>("InvalidCronString", SchedulerType.FREQUENCY_EXCLUSIVE, typeof(ArgumentException))
            };

            


            foreach (var item in cases)
            {
                logger.Info($"{item.Item1}", () => { });
                if (typeof(Exception).IsAssignableFrom(item.Item3))
                {
                    Assert.Throws(item.Item3, () =>
                    {
                        IScheduler scheduler = new Scheduler(item.Item1, (ct) => Run(ct, 0), null, (e) => { logger.Error($"{e}", () => { }); return true; });
                    });
                } else
                {
                    IScheduler scheduler = new Scheduler(item.Item1, (ct) => Run(ct, 0), null, (e) => { logger.Error($"{e}", () => { }); return true; });
                    Assert.Equal(item.Item2, scheduler.SchedulerType);
                }

            }
        }

    }
}
