using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cron;

namespace DeviceReader.Agents
{
    public class Scheduler : IScheduler
    {
        public  const int MINIMAL_FREQUENCY_ALLOWED = 200;
        private Func<CancellationToken, Task> _taskFunc = (ct) => Task.CompletedTask;
        private Func<bool> _canExecute = () => true;
        private Func<Exception, bool> _exceptionCallback = (e) => false;

        private Task _task = null;
        private int _waitTime;
        private SchedulerType _schedulerType;
        private CronSchedule _cronSchedule;
        private object _lock = new object();

        public Scheduler(string schedule, Func<CancellationToken, Task> taskFunc, Func<bool> canExecute, Func<Exception, bool> exceptionCallback)
        {
            if (taskFunc != null) _taskFunc = taskFunc;
            if (canExecute != null) _canExecute = canExecute;            
            if (exceptionCallback != null) _exceptionCallback = exceptionCallback;

            InitializeSchedulerType(schedule);            
        }

        void InitializeSchedulerType(string input)
        {            
            var schedule = input.Trim().ToUpper();
            int waittime;
            // if is integer
            if (int.TryParse(schedule, out waittime)) {
                _waitTime = waittime;
                _schedulerType = SchedulerType.FREQUENCY_EXCLUSIVE;
            } else if (schedule.Length > 1)
            {
                if (schedule[0] == 'E')
                {
                    schedule = schedule.Substring(1, schedule.Length - 1);
                    if (!int.TryParse(schedule, out waittime)) throw new ArgumentException($"Invalid schedule '{input}'");
                    _waitTime = Math.Max(Math.Abs(waittime), MINIMAL_FREQUENCY_ALLOWED); 
                    _schedulerType = SchedulerType.FREQUENCY_EXCLUSIVE;
                } else if (schedule[0] == 'I')
                {
                    schedule = schedule.Substring(1, schedule.Length - 1);
                    if (!int.TryParse(schedule, out waittime)) throw new ArgumentException($"Invalid schedule '{input}'");
                    _waitTime = Math.Max(Math.Abs(waittime), MINIMAL_FREQUENCY_ALLOWED);
                    _schedulerType = SchedulerType.FREQUENCY_INCLUSIVE;
                } else
                {
                    CronSchedule cronSchedule = new CronSchedule(schedule);
                    if (!cronSchedule.IsValid(schedule)) throw new ArgumentException($"Invalid schedule '{input}'");
                    _cronSchedule = cronSchedule;
                    _schedulerType = SchedulerType.CRON;
                }
            }
        }

        public SchedulerType SchedulerType => _schedulerType;

        public async Task RunAsync(CancellationToken ct)
        {
            if (_schedulerType == SchedulerType.CRON)
            {
                await RunAsyncCron(ct);
            } else
            {
                await RunAsyncInterval(ct);
            }
        }


        public async Task RunAsyncCron(CancellationToken ct)
        {
            var _last = DateTime.Now;

            if (ct.IsCancellationRequested == true)
            {
                //ct.ThrowIfCancellationRequested();
                return;
            }

            

            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    //ct.ThrowIfCancellationRequested();
                    return;
                }
                try
                {
                    if (DateTime.Now.Minute != _last.Minute)
                    {

                        _last = DateTime.Now;
                        lock (_lock)
                        {
                            if (_cronSchedule.IsTime(DateTime.Now) && _canExecute())
                            {
                                if (_task == null || _task.Status != TaskStatus.Running)
                                {

                                    _task = new Task(
                                        () =>
                                        {
                                            var x = _taskFunc(ct);
                                            x.Wait();
                                        }
                                        , ct, TaskCreationOptions.LongRunning);
                                    _task.Start();
                                }
                                else
                                {
                                    var e = new TaskSchedulerException("Previous task run not finished");
                                    if (_exceptionCallback(e))
                                    {
                                        throw e;
                                    }
                                }
                            }
                        }
                    }

                    await Task.Delay(5000, ct);
                }
                catch (TaskCanceledException e) { }
                catch (OperationCanceledException e) { }
                catch (AggregateException e)
                {
                    if (!(e.InnerException is TaskCanceledException))
                    {
                        if (_exceptionCallback(e)) throw (e);
                    }
                }
                catch (Exception e)
                {
                    if (_exceptionCallback(e)) throw (e);
                }
            }
        }

        public async Task RunAsyncInterval(CancellationToken ct)
        {
            if (ct.IsCancellationRequested == true)
            {                
                //ct.ThrowIfCancellationRequested();
                return;
            }

            int waittime = 0;
            

            while (true)
            {
                if (ct.IsCancellationRequested)
                {                    
                    //ct.ThrowIfCancellationRequested();
                    return;
                }
                try
                {
                    Stopwatch stopwatch = new Stopwatch();
                    
                    // Execute runtime
                    if (_canExecute() ) // to avoid cases where some executables have not yet started..
                    {
                        try
                        {
                            if (_task != null)
                            {
                                throw new TaskSchedulerException("Previous task run not finished");
                            }
                            else
                            {
                                stopwatch.Restart();
                                _task = _taskFunc(ct);
                                await _task;
                                stopwatch.Stop();
                            }

                        }                        
                        catch (Exception e)
                        {
                            if (_exceptionCallback(e))
                            {
                                throw e;
                            }                            
                        }
                        _task = null;
                    }

                    if (SchedulerType == SchedulerType.FREQUENCY_EXCLUSIVE)
                    {
                        waittime = _waitTime;
                    } else
                    {
                        waittime = _waitTime - (int)stopwatch.ElapsedMilliseconds;
                        if (waittime < 0) waittime = 0;
                    }
                    if (waittime > 0)
                    {
                        await Task.Delay(_waitTime, ct).ConfigureAwait(false);
                    }

                }
                catch (TaskCanceledException e) { }
                catch (OperationCanceledException e) { }                
                catch (AggregateException e)
                {
                    if (!(e.InnerException is TaskCanceledException)) {
                        if (_exceptionCallback(e)) throw (e);
                    }
                }
                catch (Exception e) {
                    if (_exceptionCallback(e)) throw (e);
                }

            }
        }
    }
}
