using System;
using System.Threading;

namespace Utils
{
    public class BackgroundThreadWorker
    {
        private readonly Action<WorkerMethodParameter> _action;
        private CancellationTokenSource _tokenSource;
        private Thread _thread;
        private ThreadPriority _priority = ThreadPriority.Lowest;

        public string Name { get; }
        public bool IsRunning => _thread?.IsAlive == true;
        public bool Cancelled { get; private set; }
        public Exception Error { get; private set; }

        public BackgroundThreadWorker(Action<WorkerMethodParameter> action, string name)
        {
            _action = action;
            Name = name;
        }

        public BackgroundThreadWorker(Action<WorkerMethodParameter> action, string name, ThreadPriority priority)
            : this(action, name)
        {
            _priority = priority;
        }

        public void Start() => Start(null);
        public void Start(object data)
        {
            if (_thread?.IsAlive != true)
            {
                Error = null;
                Cancelled = false;
                _tokenSource = new CancellationTokenSource();
                var token = _tokenSource.Token;
                _thread = new Thread(() =>
                {
                    try
                    {
                        _action(new WorkerMethodParameter() { Token = token, Data = data });
                    }
                    catch (Exception ex)
                    {
                        Error = ex;
                    }
                })
                {
                    IsBackground = true,
                    Name = $"{nameof(BackgroundThreadWorker)}_{Name}",
                    Priority = _priority
                };
                _thread.Start();
            }
            else
                throw new InvalidOperationException("Already started");
        }

        public void Cancel()
        {
            Cancelled = true;
            _tokenSource?.Cancel();
        }

        public void Abort()
        {
            Cancelled = true;
            _tokenSource?.Cancel();
            if (_thread?.IsAlive == true)
                _thread.Abort();
        }

        public void Wait()
        {
            if (_thread?.IsAlive == true)
                _thread.Join();
        }
    }
}
