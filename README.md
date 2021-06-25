# MinecraftUtils

Collection of utils related to minecraft used in other projects

[![NuGet version (MinecraftUtils)](https://img.shields.io/nuget/v/MinecraftUtils.svg?style=flat-square)](https://www.nuget.org/packages/MinecraftUtils/)

## Usage

```

IServiceProvider minecraftUtils = new ServiceCollection()
                .AddSingletonMinecraftClient()
                .AddSingletonTaskExecutor()
                .BuildServiceProvider();

IMinecraftClient minecraftClient = minecraftUtils.GetService<IMinecraftClient>();

// This is used to return any task wrapped as ITaskResponse with execution statistics. 
// IMinecraftClient already internally uses this.
ITaskExecutor taskExecutor = minecraftUtils.GetService<ITaskExecutor>();
```
