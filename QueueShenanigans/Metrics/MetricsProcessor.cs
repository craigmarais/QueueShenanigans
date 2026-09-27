using System;
using System.Collections.Generic;
using System.Text;

namespace QueueShenanigans.Utilities
{
    internal class MetricsProcessor
    {
        public static async Task ToConsoleAsync(MetricsRecorder metricsRecorder, CancellationToken token)
        {
            long enqCount = 0;
            long deqCount = 0;
            long enqMiss = 0;
            long deqMiss = 0;

            while (!token.IsCancellationRequested)
            {
                var metrics = metricsRecorder.Read();
                var enqRate = metrics.enqCount - enqCount;
                var deqRate = metrics.deqCount - deqCount;
                var enqMissRate = metrics.enqMiss - enqMiss;
                var deqMissRate = metrics.deqMiss - deqMiss;

                if (enqRate > 0)
                {
                    enqCount = metrics.enqCount;
                    Console.WriteLine($"Enq: {enqRate:N0}\tDeq: {deqRate:N0}\tEnqMiss: {enqMissRate:N0}\tDeqMiss: {deqMissRate:N0}");
                }

                enqCount = metrics.enqCount;
                deqCount = metrics.deqCount;
                enqMiss = metrics.enqMiss;
                deqMiss = metrics.deqMiss;

                await Task.Delay(1000, token);
            }
        }
    }
}
