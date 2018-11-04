using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Como.WebApi.Caching
{
    public class DelayedInvalidationQueueProcessor : IHostedService, IDisposable
    {
        private readonly IWebApiCacheAdapter _cacheAdapter;
        private readonly ILogger<DelayedInvalidationQueueProcessor> _logger;
        private readonly ConcurrentQueue<DelayedInvalidationParameters> _queue;
        private readonly ConsecutiveTimer _timer;
        private CancellationTokenSource _cancellationTokenSource;

        public DelayedInvalidationQueueProcessor(
            DelayedInvalidationQueue queue,
            IWebApiCacheAdapter cacheAdapter,
            ILogger<DelayedInvalidationQueueProcessor> logger)
        {
            _timer = new ConsecutiveTimer();
            _timer.OnTick += ProcessDelayedActionsQueue;
            _queue = queue.Queue;
            _logger = logger;
            _cacheAdapter = cacheAdapter;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Start(TimeSpan.FromMilliseconds(100));
            _cancellationTokenSource = new CancellationTokenSource();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            _cancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }

        private void ProcessDelayedActionsQueue()
        {
            var dequeuedBuffer = new List<DelayedInvalidationParameters>();
            try
            {
                var invalidationTasks = new List<Task>();
                while (_queue.TryDequeue(out var item)
                       && !_cancellationTokenSource.IsCancellationRequested)
                {
                    if (DateTime.UtcNow < item.DueTime)
                    {
                        dequeuedBuffer.Add(item);
                    }
                    else
                    {
                        invalidationTasks.Add(_cacheAdapter.InvalidateCachedMethodResults(item.Methods));
                    }
                }

                Task.WaitAll(invalidationTasks.ToArray(), _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while trying to invalidate a cached method(s) (delayed)!");
            }
            finally
            {
                dequeuedBuffer.ForEach(_queue.Enqueue);
            }
        }
    }
}