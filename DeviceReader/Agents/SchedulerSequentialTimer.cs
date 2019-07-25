using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeviceReader.Agents
{
    public class SchedulerSequentialTimer : IScheduler
    {
        public  const int MINIMAL_FREQUENCY_ALLOWED = 200;
        private Func<CancellationToken, Task> _taskFunc = (ct) => Task.CompletedTask;
        private Func<bool> _canExecute = () => true;
        private Func<Exception, bool> _exceptionCallback = (e) => false;

        private Task _task = null;
        private int _waitTime;

        public SchedulerSequentialTimer(int waitTime, Func<CancellationToken, Task> taskFunc, Func<bool> canExecute, Func<Exception, bool> exceptionCallback)
        {
            if (taskFunc != null) _taskFunc = taskFunc;
            if (canExecute != null) _canExecute = canExecute;            
            if (exceptionCallback != null) _exceptionCallback = exceptionCallback;
            _waitTime = Math.Max(Math.Abs(waitTime), MINIMAL_FREQUENCY_ALLOWED); // do not allow less than 200ms frequency. Should we throw ?
        }
        
        public async Task RunAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested == true)
            {                
                ct.ThrowIfCancellationRequested();
                return;
            }

            while (true)
            {
                if (ct.IsCancellationRequested)
                {                    
                    ct.ThrowIfCancellationRequested();
                    return;
                }
                try
                {

                    // Execute runtime
                    if (_canExecute() && _task == null) // to avoid cases where some executables have not yet started..
                    {
                        try
                        {

                            _task = _taskFunc(ct);
                            _task.Wait();

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
                    
                    await Task.Delay(_waitTime, ct).ConfigureAwait(false);

                }                
                catch (Exception e) {}

            }
        }
    }
}
