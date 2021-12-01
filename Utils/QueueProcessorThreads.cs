using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public class QueueProcessorThreads<T>
    {
        private readonly Action<CancellationToken, T> _action;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly object _queueLock = new object();
        private readonly List<Thread> _threads = new List<Thread>();
        private readonly CancellationToken _token;
        private bool _finishedAdding;

        public bool Cancelled => _token.IsCancellationRequested;

        public QueueProcessorThreads(Action<CancellationToken, T> action) : this(action, Environment.ProcessorCount, ThreadPriority.Normal)
        {
        }
        public QueueProcessorThreads(Action<CancellationToken, T> action, int numberOfThreads, ThreadPriority threadPriority)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            if (numberOfThreads <= 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfThreads));

            _action = action;
            _token = _tokenSource.Token;

            for (int i = 0; i < numberOfThreads; i++)
            {
                var t = new Thread(new ThreadStart(WorkMethod));
                t.Name = $"QueueProcessorThread_{i}";
                t.Priority = threadPriority;
                t.IsBackground = true;
                _threads.Add(t);
                t.Start();
            }
        }

        private void WorkMethod()
        {
            while (!_token.IsCancellationRequested)
            {
                T item = default(T);
                bool dequeued = false;
                lock (_queueLock)
                {
                    if (_queue.Count > 0)
                    {
                        item = _queue.Dequeue();
                        dequeued = true;
                    }
                }
                if (dequeued)
                    _action(_token, item);
                else if (_finishedAdding)
                    break;
                else
                    Thread.Sleep(10);
            }
        }

        public void Enqueue(T item)
        {
            if (_finishedAdding)
                throw new InvalidOperationException("Adding new items is not possible after the Wait method invoked");
            if (_token.IsCancellationRequested)
                throw new TaskCanceledException();
            lock (_queueLock)
                _queue.Enqueue(item);
        }

        public void FinishedAdding()
        {
            _finishedAdding = true;
        }

        public void Wait()
        {
            _finishedAdding = true;
            foreach (var t in _threads)
                if (t.IsAlive)
                    t.Join();
        }

        public void Cancel()
        {
            if (!Cancelled)
                _tokenSource.Cancel();
        }
    }
}
