using QueueShenanigans.Utilities;

namespace QueueShenanigans.QueueHandlers
{
    internal class DisruptorConsumer
    {
        readonly MetricsRecorder _recorder;
        long _expectedInput = 0;

        public DisruptorConsumer(MetricsRecorder recorder)
        {
            _recorder = recorder;
        }

        public unsafe void Handle(long value)
        {
            var input = value;
            if (++_expectedInput != input)
                Console.WriteLine($"Unexpected value received: {input} expected {_expectedInput}");

            _recorder.IncDequeue();
        }
    }
}
