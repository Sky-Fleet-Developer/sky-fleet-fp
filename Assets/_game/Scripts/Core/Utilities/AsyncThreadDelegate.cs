using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Utilities
{
    public class AsyncThreadDelegate<T>
    {
        public AsyncThreadDelegate(Semaphore semaphore)
        {
            _semaphore = semaphore;
        }

        private readonly Semaphore _semaphore;

        public Task<T> RunAsync(Func<T> func)
        {
            TaskCompletionSource<T> taskCompletionSource = new TaskCompletionSource<T>();

            Thread thread = new Thread(ThreadProcess);
            thread.Start();
            
            void ThreadProcess()
            {
                _semaphore.WaitOne();
                taskCompletionSource.SetResult(func());
                _semaphore.Release();
            }
            return taskCompletionSource.Task;
        }


    }
}