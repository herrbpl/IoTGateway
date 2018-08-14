using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Router
{

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
