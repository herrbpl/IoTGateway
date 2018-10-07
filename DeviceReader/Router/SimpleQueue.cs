using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace DeviceReader.Router
{
   

    /// <summary>
    /// For testing time, add system default queue, later add some persistant queue. 
    /// NB! this implementation is not thread-safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SimpleQueue<T> : IQueue<T>
    {
        private Queue<T> _queue;
        private string _name;

        public bool IsEmpty => _queue.Count == 0;

        public int Count => _queue.Count;

        public T Dequeue()
        {
            return _queue.Dequeue();
        }

        public void Dispose()
        {
            if (_queue != null) _queue.Clear();
            _queue = null;
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

        public void Flush()
        {
            return;
        }

        public SimpleQueue(string queuename)
        {
            _queue = new Queue<T>();
            _name = queuename;            
        }


}
}
