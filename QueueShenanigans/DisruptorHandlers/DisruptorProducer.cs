using QueueShenanigans.Queues;
using QueueShenanigans.Utilities;

namespace QueueShenanigans.QueueHandlers
{
    internal class DisruptorProducer 
    {
        readonly DisruptorQueue<long> _ring;
        readonly MetricsRecorder _recorder;
        readonly Thread _workingThread;
        readonly CancellationTokenSource _cts = new();
        readonly int _core;

        public DisruptorProducer(DisruptorQueue<long> ring, MetricsRecorder recorder, int core = -1)
        {
            _ring = ring;
            _recorder = recorder;
            _workingThread = new Thread(Run);
        }

        public void Produce(long value)
        {
            _ring.Enqueue(value);
            _recorder.IncEnqueue();
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

        void Run()
        {
            if (_core > 0)
                WinSafeThreadHandle.SetProcessorAffinity(_core);
            long value = 0;
            while (!_cts.IsCancellationRequested)
            {
                Produce(++value);
            }
        }
    }
}
