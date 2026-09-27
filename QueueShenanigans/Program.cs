using QueueShenanigans.QueueHandlers;
using QueueShenanigans.Queues;
using QueueShenanigans.Utilities;
try
{
    var producerCount = 1;
    var consumerCount = 1;
    CancellationTokenSource cts = new();
    var recorder = new MetricsRecorder();
    _ = MetricsProcessor.ToConsoleAsync(recorder, cts.Token);

    //HandleSpscRing();
    //HandleMpscRing();
    //HandleSpmcRing();
    HandleDisruptor();

    Console.CancelKeyPress += (s, k) =>
    {
        if (k.Cancel)
            cts.Cancel();
    };
    void HandleMpscRing()
    {
        var mpsc = new MpscRing(1024 * 64);

        var consumer = new MpscConsumer(mpsc, recorder);
        consumer.Start(cts.Token);

        var producers = new MpscProducer[producerCount];
        for (int i = 0; i < producerCount; i++)
        {
            var producer = new MpscProducer(mpsc, recorder);
            producer.Start(cts.Token);
            producers[i] = producer;
        }
    }

    void HandleSpmcRing()
    {
        var ring = new SpmcRing(1024 * 64);

        var consumers = new SpmcConsumer[consumerCount];
        for (int i = 0; i < consumerCount; i++)
        {
            var consumer = new SpmcConsumer(ring, recorder);
            consumer.Start(cts.Token);
            consumers[i] = consumer;
        }

        var producer = new SpmcProducer(ring, recorder);
        producer.Start(cts.Token);
    }

    void HandleSpscRing()
    {
        var spsc = new SpscRing(1024 * 64);

        var consumer = new SpscConsumer(spsc, recorder);
        consumer.Start(cts.Token);

        var producer = new SpscProducer(spsc, recorder);
        producer.Start(cts.Token);
    }

    void HandleDisruptor()
    {
        var recorder = new MetricsRecorder();
        var consumer = new DisruptorConsumer(recorder);
        var mpsc = new DisruptorQueue<long>(consumer.Handle, 1024 * 64);
        mpsc.Start();

        _ = MetricsProcessor.ToConsoleAsync(recorder, cts.Token);

        var producers = new MpscProducer[producerCount];
        for (int i = 0; i < producerCount; i++)
        {
            var producer = new DisruptorProducer(mpsc, recorder, i);
            producer.Start(cts.Token);
        }
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
}
