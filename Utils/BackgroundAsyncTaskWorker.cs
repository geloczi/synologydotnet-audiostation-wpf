using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public class BackgroundAsyncTaskWorker
    {
        private readonly object _lock = new object();
        private readonly Func<CancellationToken, Task> _action;
        private CancellationTokenSource _tokenSource;

        public Task WorkerTask { get; private set; }
        public bool IsRunning { get; private set; }
        public bool Cancelled { get; private set; }
        public Exception Error { get; private set; }
        public BackgroundAsyncTaskWorker(Func<CancellationToken, Task> asyncAction)
        {
            _action = asyncAction;
        }

        public void Start()
        {
            lock (_lock)
            {
                if (IsRunning)
                    throw new InvalidOperationException("The task is already running.");
                Cancelled = false;
                IsRunning = true;
                _tokenSource = new CancellationTokenSource();
                var token = _tokenSource.Token;
                WorkerTask = Task.Run(async () =>
                {
                    try
                    {
                        await _action(token);
                    }
                    catch (Exception ex)
                    {
                        Error = ex;
                    }
                    finally
                    {
                        IsRunning = false;
                    }
                });
            }
        }

        public void Cancel()
        {
            Cancelled = true;
            _tokenSource?.Cancel();
        }

        public void Wait()
        {
            WorkerTask?.GetAwaiter().GetResult();
        }
    }
}
