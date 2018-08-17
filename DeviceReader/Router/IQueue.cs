using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Router
{
    /// <summary>
    /// Queue for storing messages between executables.
    /// TODO: Add possibility to subscribe to enqueue event for quicker triggering of queue processing.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQueue<T> : IDisposable
    {
        T Peek();
        T Dequeue();
        void Enqueue(T item);
        bool IsEmpty { get; }
        int Count { get; }
        void Flush();
    }
}
