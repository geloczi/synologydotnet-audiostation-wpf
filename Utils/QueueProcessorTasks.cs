using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Utils
{
    public class QueueProcessorTasks<T>
    {
        private readonly Func<CancellationToken, T, Task> _action;
        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly List<Task> _tasks = new List<Task>();
        private readonly CancellationToken _token;
        private bool _finishedAdding;

        public bool Cancelled => _token.IsCancellationRequested;

        public QueueProcessorTasks(Func<CancellationToken, T, Task> action) : this(action, Environment.ProcessorCount)
        {
        }
        public QueueProcessorTasks(Func<CancellationToken, T, Task> action, int numberOfParallelTasks)
        {
            if (action is null)
                throw new ArgumentNullException(nameof(action));
            if (numberOfParallelTasks <= 0)
                throw new ArgumentOutOfRangeException(nameof(numberOfParallelTasks));

            _action = action;
            _token = _tokenSource.Token;

            for (int i = 0; i < numberOfParallelTasks; i++)
                _tasks.Add(Task.Run(WorkMethod));
        }

        private async Task WorkMethod()
        {
            while (!_token.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out var item))
                    await _action(_token, item);
                else if (_finishedAdding)
                    break;
                else
                    await Task.Delay(10);
            }
        }

        public void Enqueue(T item)
        {
            if (_finishedAdding)
                throw new InvalidOperationException("Adding new items is not possible after the Wait method invoked");
            if (_token.IsCancellationRequested)
                throw new TaskCanceledException();
            _queue.Enqueue(item);
        }

        public void FinishedAdding()
        {
            _finishedAdding = true;
        }

        public void Wait()
        {
            _finishedAdding = true;
            foreach (var t in _tasks)
                t.Wait();
        }

        public async Task WaitAsync()
        {
            _finishedAdding = true;
            foreach (var t in _tasks)
                await t;
        }

        public void Cancel()
        {
            if (!Cancelled)
                _tokenSource.Cancel();
        }
    }
}
