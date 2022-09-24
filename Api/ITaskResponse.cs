namespace MinecraftUtils.Api
{
    using System;

    /// <summary>
    /// Task wrapper to return statistics for the task
    /// </summary>
    /// <typeparam name="T">Return type of Task</typeparam>
    /// <seealso cref="ITaskExecutor"/>
    public interface ITaskResponse<T> where T : class
    {
        /// <summary>
        /// Result of the task
        /// </summary>
        public T Result { get; }

        /// <summary>
        /// Details about the task that was run
        /// </summary>
        public ITaskAction Task { get; }
    }

    public interface ITaskAction
    {
        public string Name { get; }

        /// <summary>
        /// Execution stats of the task
        /// </summary>
        public ITaskStatistics Stats { get; }
    }

    public interface ITaskStatistics
    {
        /// <summary>
        /// Time taken for the task to run to completion or timeout
        /// </summary>
        public TimeSpan ExecutionTime { get; }

        /// <summary>
        /// True if the task threw TaskCancelledException
        /// </summary>
        public bool TimedOut { get; }

        /// <summary>
        /// True if the task ran to completion
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Populated in case the task threw an exception
        /// </summary>
        public Exception Exception { get; }
    }


}
