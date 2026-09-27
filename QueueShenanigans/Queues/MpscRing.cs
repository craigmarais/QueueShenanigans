using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace QueueShenanigans.Queues
{
    public class MpscRing
    {
        public delegate void OnDequeueDelegate(ref Item value);
        public unsafe struct Item
        {
            public byte* Value;
            public int Length;
            public long Sequence;
        }
        [StructLayout(LayoutKind.Sequential, Size = 64)]
        struct PaddedLong
        {
            public long Value;
        }

        [StructLayout(LayoutKind.Sequential, Size = 64)]
        struct PaddedInt
        {
            public int Value;
        }

        readonly Item[] _buffer;
        readonly int _mask;
        SpinWait _spinner = new ();

        PaddedLong _head;
        PaddedLong _tail;

        public MpscRing(int capacity)
        {
            if (capacity < 2 || (capacity & (capacity - 1)) != 0)
                throw new ArgumentOutOfRangeException("Capacity must be a power of 2", nameof(capacity));

            _buffer = new Item[capacity];
            _mask = capacity - 1;

            for (int i = 0; i < capacity; i++)
                _buffer[i].Sequence = i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool TryEnqueue(in byte* value, in int length)
        {
            var buffer = _buffer;
            var mask = _mask;

            while (true)
            {
                var head = Volatile.Read(ref _head.Value);
                ref var cell = ref buffer[(int)head & mask];

                var sequence = Volatile.Read(ref cell.Sequence);
                var dif = sequence - head;

                if (dif == 0)
                {
                    if (Interlocked.CompareExchange(ref _head.Value, head + 1, head) == head)
                    {
                        cell.Value = value;
                        cell.Length = length;
                        Volatile.Write(ref cell.Sequence, head + 1);
                        return true;
                    }

                    _spinner.SpinOnce();
                    continue;
                }

                // queue full
                if (dif < 0)
                    return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(OnDequeueDelegate cb)
        {
            var tail = _tail.Value;
            ref var cell = ref _buffer[(int)tail & _mask];
            var sequence = Volatile.Read(ref cell.Sequence);
            var dif = sequence - (tail + 1);

            if (dif == 0)
            {
                cb(ref cell);
                _tail.Value = tail + 1;
                Volatile.Write(ref cell.Sequence, tail + _mask + 1);
                return true;
            }

            return false;
        }
    }
}
