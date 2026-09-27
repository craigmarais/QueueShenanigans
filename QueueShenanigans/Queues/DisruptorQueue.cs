using Disruptor;
using Disruptor.Dsl;
using System.Runtime.CompilerServices;

namespace QueueShenanigans.Queues
{
    public class DataEvent<T>
    {
        public T Data { get; set; }
    }

    public class DataEventHandler<T>(Action<T> handler, int length) : IEventHandler<DataEvent<T>>
    {
        public void OnEvent(DataEvent<T> data, long sequence, bool endOfBatch)
        {
            handler(data.Data);
        }
    }

    public class DataEventFactory<T>
    {
        public DataEvent<T> Create()
        {
            return new DataEvent<T>();
        }
    }

    public class DisruptorQueue<T> where T : unmanaged
    {
        readonly DataEventFactory<T> _factory;
        readonly Disruptor<DataEvent<T>> _queue;
        RingBuffer<DataEvent<T>> _buffer;

        public DisruptorQueue(Action<T> callback, int maxBufferSize)
        {
            _factory = new DataEventFactory<T>();
            _queue = new Disruptor<DataEvent<T>>(_factory.Create, maxBufferSize, TaskScheduler.Default, ProducerType.Multi, new BusySpinWaitStrategy());
            _queue.HandleEventsWith(new DataEventHandler<T>(callback, maxBufferSize));
        }

        public void Start()
        {
            _buffer = _queue.Start();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T data)
        {
            var seq = _buffer.Next();
            try
            {
                _buffer[seq].Data = data;
            }
            finally
            {
                _buffer.Publish(seq);
            }
        }
    }
}
