namespace QueueShenanigans.Utilities
{
    internal class MetricsRecorder
    {
        long _dequeueCount;
        long _enqueueCount;

        long _enqueueMiss;
        long _dequeueMiss;

        public bool IncEnqueue()
        {
            var enqCount = Volatile.Read(ref _enqueueCount);
            return Interlocked.CompareExchange(ref _enqueueCount, enqCount + 1, enqCount) == enqCount;
        }

        public bool IncDequeue()
        {
            var deqCount = Volatile.Read(ref _dequeueCount);
            return Interlocked.CompareExchange(ref _dequeueCount, deqCount + 1, deqCount) == deqCount;
        }

        public bool IncEnqueueMiss()
        {
            var enqMiss = Volatile.Read(ref _enqueueMiss);
            return Interlocked.CompareExchange(ref _enqueueMiss, enqMiss + 1, enqMiss) == enqMiss;
        }

        public bool IncDequeueMiss()
        {
            var deqMiss = Volatile.Read(ref _dequeueMiss);
            return Interlocked.CompareExchange(ref _dequeueMiss, deqMiss + 1, deqMiss) == deqMiss;
        }

        public (long enqCount,long deqCount,long enqMiss,long deqMiss) Read()
        {
            return (Volatile.Read(ref _enqueueCount), Volatile.Read(ref _dequeueCount),
                Volatile.Read(ref _enqueueMiss), Volatile.Read(ref _dequeueMiss));
        }
    }
}
