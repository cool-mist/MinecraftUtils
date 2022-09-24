namespace MinecraftUtils.Api.Impl
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    internal class TaskExecutor : ITaskExecutor
    {
        public async Task<ITaskResponse<T>> ExecuteAsync<T>(
            string taskName,
            Func<Task<T>> taskFn,
            CancellationToken cancellationToken) where T : class
        {

            bool succeeded = false;
            bool timedout = false;
            Exception exception = null;
            T result = null;
            TimeSpan executionTime;

            var Watch = Stopwatch.StartNew();

            try
            {
                result = await taskFn.Invoke();
                succeeded = true;
            }
            catch (TaskCanceledException e)
            {
                succeeded = false;
                timedout = true;
                exception = e;
            }
            catch (Exception e)
            {
                succeeded = false;
                exception = e;
            }
            finally
            {
                Watch.Stop();
                executionTime = Watch.Elapsed;
            }

            var stats = new TaskStatistics()
            {
                Succeeded = succeeded,
                TimedOut = timedout,
                ExecutionTime = executionTime,
                Exception = exception
            };

            return new TaskResponse<T>()
            {
                Result = result,
                Task = new TaskAction(taskName, stats)
            };
        }
    }

    internal class TaskResponse<T> : ITaskResponse<T> where T : class
    {
        public T Result { get; set; }

        public ITaskAction Task { get; set; }
    }

    internal class TaskAction : ITaskAction
    {
        internal TaskAction(string name, ITaskStatistics stats)
        {
            this.Name = name;
            this.Stats = stats;
        }

        public string Name { get; }

        public ITaskStatistics Stats { get; }
    }

    internal class TaskStatistics : ITaskStatistics
    {
        public TimeSpan ExecutionTime { get; set; }

        public bool TimedOut { get; set; }

        public bool Succeeded { get; set; }

        public Exception Exception { get; set; }
    }
}
