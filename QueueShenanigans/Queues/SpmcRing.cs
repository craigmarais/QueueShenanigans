using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace QueueShenanigans.Queues
{
    public unsafe class SpmcRing
    {
        public delegate void OnDequeueDelegate(ref Item value);
        public struct Item
        {
            public byte* Value;
            public int Length;
            public long Sequence;
        }

        [StructLayout(LayoutKind.Explicit, Size = 64)]
        private struct PaddedLong
        {
            [FieldOffset(0)] public long Value;
        }

        Item[] _buffer;

        long _mask;

        PaddedLong _head;
        PaddedLong _tail;

        public SpmcRing(long capacity)
        {
            if (capacity > 0 && (capacity & (capacity - 1)) > 0)
                throw new ArgumentException("capacity must be a power of 2");

            _mask = capacity - 1;
            _buffer = new Item[capacity];

            for (var i = 0; i < capacity; i++)
                _buffer[i].Sequence = i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryEnqueue(in byte* ptr, in int length)
        {
            var head = _head.Value;
            ref var cell = ref _buffer[(int)head & _mask];

            var seq = Volatile.Read(ref cell.Sequence); // Dequeue writes
            var dif = seq - head;

            // queue full
            if (dif != 0)
                return false;

            cell.Value = ptr;
            cell.Length = length;
            Volatile.Write(ref cell.Sequence, head + 1); // Dequeue reads

            _head.Value = head + 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(OnDequeueDelegate cb)
        {
            var buffer = _buffer;
            var mask = _mask;

            var tail = Volatile.Read(ref _tail.Value); // multi-consumer reads
            while (true)
            {
                ref var cell = ref buffer[(int)tail & mask];

                var seq = Volatile.Read(ref cell.Sequence); // Enqueue writes
                var dif = seq - (tail + 1);

                if (dif == 0)
                {
                    if (Interlocked.CompareExchange(ref _tail.Value, tail + 1, tail) == tail)
                    {
                        cb(ref buffer[(int)tail & mask]);
                        Volatile.Write(ref cell.Sequence, tail + mask + 1); // Enqueue reads
                        return true;
                    }
                }
                else if (dif < 0) // queue empty
                    return false;
                else // lost race
                    tail = Volatile.Read(ref _tail.Value);
            }
            return false;
        }
    }
}
