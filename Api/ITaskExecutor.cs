using System;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftUtils.Api
{
    public interface ITaskExecutor
    {
        public Task<ITaskResponse<T>> ExecuteAsync<T>(string taskName, Func<Task<T>> taskFn, CancellationToken cancellationToken) where T : class;
    }
}
