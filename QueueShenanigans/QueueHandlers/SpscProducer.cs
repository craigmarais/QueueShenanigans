using QueueShenanigans.Queues;
using QueueShenanigans.Utilities;

namespace QueueShenanigans.QueueHandlers
{
    internal class SpscProducer
    {
        readonly SpscRing _ring;
        readonly MetricsRecorder _recorder;
        readonly Thread _workingThread;
        readonly CancellationTokenSource _cts = new();
        readonly int _core;

        public SpscProducer(SpscRing ring, MetricsRecorder recorder, int core = -1)
        {
            _ring = ring;
            _recorder = recorder;
            _core = core;
            _workingThread = new Thread(Run);
        }

        public void Start(CancellationToken token)
        {
            token.Register(_cts.Cancel);
            _workingThread.Start();
        }

        public void Stop()
        {
            _cts.Cancel();
            _workingThread.Join();
        }

        unsafe void Run()
        {
            if (_core > 0)
                WinSafeThreadHandle.SetProcessorAffinity(_core);

            long value = 0;
            var data = stackalloc long[1024 * 128];
            var mask = (1024 * 128) - 1;
            var i = 0;

            while (!_cts.IsCancellationRequested)
            {
                var idx = i & mask;
                data[idx] = ++value;

                while (!_ring.Enqueue((byte*)(data + idx), 8))
                    _recorder.IncEnqueueMiss();

                _recorder.IncEnqueue();
                i++;
            }
        }
    }
}
