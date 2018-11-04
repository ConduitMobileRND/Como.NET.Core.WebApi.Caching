using System;
using System.Threading;

namespace Como.WebApi.Caching
{
    public class ConsecutiveTimer : IDisposable
    {
        private readonly Timer _timer;
        private readonly object _timerSyncLock;
        private int _timerStarted;

        public ConsecutiveTimer()
        {
            _timer = new Timer(InvokeOnTick);
            _timerSyncLock = new object();
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timerStarted = 0;
        }

        public event Action OnTick;

        public void Start(TimeSpan interval)
        {
            if (Interlocked.CompareExchange(ref _timerStarted, 1, 0) == 1)
            {
                throw new InvalidOperationException("Timer already started!");
            }

            _timer.Change(TimeSpan.Zero, interval);
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _timerStarted, 0, 1) == 0)
            {
                throw new InvalidOperationException("Timer already stopped!");
            }

            _timer.Change(Timeout.Infinite, 0);
        }

        private void InvokeOnTick(object state)
        {
            if (!Monitor.TryEnter(_timerSyncLock))
            {
                return;
            }

            try
            {
                OnTick?.Invoke();
            }
            finally
            {
                Monitor.Exit(_timerSyncLock);
            }
        }
    }
}