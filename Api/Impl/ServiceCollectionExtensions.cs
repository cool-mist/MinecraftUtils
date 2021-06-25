using Microsoft.Extensions.DependencyInjection;

namespace MinecraftUtils.Api.Impl
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSingletonMinecraftClient(this IServiceCollection serviceCollection)
        {
            IMinecraftClient client = new MinecraftClient();
            return serviceCollection.AddSingleton(client);
        }

        public static IServiceCollection AddSingletonTaskExecutor(this IServiceCollection serviceCollection)
        {
            ITaskExecutor executor = new TaskExecutor();
            return serviceCollection.AddSingleton(executor);
        }
    }
}
