using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Como.WebApi.Caching
{
    public class DelayedInvalidationQueue
    {
        internal readonly ConcurrentQueue<DelayedInvalidationParameters> Queue;

        public DelayedInvalidationQueue()
        {
            Queue = new ConcurrentQueue<DelayedInvalidationParameters>();
        }

        public void Enqueue(IList<MethodInvalidationParameters> methodParameters, TimeSpan delay)
        {
            Queue.Enqueue(new DelayedInvalidationParameters
            {
                Methods = methodParameters,
                DueTime = DateTime.UtcNow + delay
            });
        }
    }
}