using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace DeviceReader.Devices
{
    
    public interface IDeviceQueue<T>: IDisposable
    {
        
        T Peek();
        T Dequeue();
        void Enqueue(T item);
        bool IsEmpty { get; }
    }

    /// <summary>
    /// For testing time, add system default queue, later add some persistant queue. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleQueue<T> : IDeviceQueue<T>
    {
        private Queue<T> _queue;
        

        public bool IsEmpty => throw new NotImplementedException();

        public T Dequeue()
        {
            return _queue.Dequeue();
        }

        public void Dispose()
        {
            return;
        }

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
        }

        public T Peek()
        {
            return _queue.Peek();
        }

        public SimpleQueue()
        {
            _queue = new Queue<T>();            
        }


}
}
