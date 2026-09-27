using QueueShenanigans.Queues;
using QueueShenanigans.Utilities;

namespace QueueShenanigans.QueueHandlers
{
    internal class SpscConsumer
    {
        readonly SpscRing _ring;
        readonly MetricsRecorder _recorder;
        readonly CancellationTokenSource _cts = new();
        readonly Thread _workingThread;
        long _expectedInput = 0;

        public SpscConsumer(SpscRing ring, MetricsRecorder recorder)
        {
            _ring = ring;
            _recorder = recorder;
            _workingThread = new Thread(Run);
        }

        public void Start(CancellationToken token = default)
        {
            token.Register(_cts.Cancel);
            _workingThread.Start();
        }

        public void Stop()
        {
            _cts.Cancel();
            _workingThread.Join();
        }

        public unsafe void Handle(ref SpscRing.Item value)
        {
            var input = *(long*)value.Value;
            if (++_expectedInput != input)
                Console.WriteLine($"Unexpected value received: {input} expected {_expectedInput}");

            _recorder.IncDequeue();
        }

        public void Run()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    if (!_ring.TryDequeue(Handle))
                        _recorder.IncDequeueMiss();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
