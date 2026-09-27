using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace QueueShenanigans.Queues
{
    public unsafe class SpscRing
    {
        public delegate void OnDequeueDelegate(ref Item value);
        public struct Item
        {
            public byte* Value;
            public int Length;
        }

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        struct PaddedLong
        {
            public long Value;
        }

        Item[] _buffer;

        long _capacity;
        long _mask;

        PaddedLong _head;
        PaddedLong _tail;

        public SpscRing(long capacity)
        {
            if (capacity > 0 && (capacity & (capacity - 1)) > 0)
                throw new ArgumentException("capacity must be a power of 2");

            _capacity = capacity;
            _mask = capacity - 1;
            _buffer = new Item[capacity];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Enqueue(in byte* ptr, int length)
        {
            var head = _head.Value;
            var tail = Volatile.Read(ref _tail.Value);

            // queue is full
            if (head - tail >= _capacity)
                return false;

            _buffer[(int)head & _mask].Value = ptr;
            _buffer[(int)head & _mask].Length = length;

            Volatile.Write(ref _head.Value, head + 1);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(OnDequeueDelegate cb)
        {
            var tail = _tail.Value;
            var head = Volatile.Read(ref _head.Value);

            // queue is empty
            if (head <= tail)
                return false;

            cb(ref _buffer[(int)tail & _mask]);

            Volatile.Write(ref _tail.Value, tail + 1);
            return true;
        }

    }
}
