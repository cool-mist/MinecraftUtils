# MinecraftUtils

Collection of utils related to minecraft used in other projects

[![NuGet version (MinecraftUtils)](https://img.shields.io/nuget/v/MinecraftUtils.svg?style=flat-square)](https://www.nuget.org/packages/MinecraftUtils/)

## Usage

```

IServiceProvider serviceProvider = new ServiceCollection()
                                    .AddSingletonMinecraftClient()
                                    .BuildServiceProvider();

IMinecraftClient client = serviceProvider.GetService<IMinecraftClient>();
```
