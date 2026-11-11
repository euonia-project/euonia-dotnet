# Euonia.Mapping.Mapster

Euonia 对象映射的 Mapster 实现：将 Mapster 的 `IMapper` 适配为 `ITypeAdapter`，并通过 `MapsterModule` 在模块初始化时设为当前映射工厂。相比 AutoMapper 更轻量、启动更快。

Mapster implementation for Euonia object mapping: adapts Mapster's `IMapper` to `ITypeAdapter` and installs it as the current mapping factory during module initialization via `MapsterModule`. It is lighter and faster to start than AutoMapper.

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`
- **NuGet 包**：`Euonia.Mapping.Mapster`

## 安装

```bash
dotnet add package Euonia.Mapping.Mapster
```

## 主要功能

- **映射工厂**：`MapsterTypeAdapterFactory` 实现 `ITypeAdapterFactory`。
- **模块集成**：`MapsterModule` 注册 Mapster 服务，并在应用初始化时调用 `TypeAdapterFactory.SetCurrent`。
- **映射注册**：`MapsterOptions.AddProfile`、`AddProfiles` 支持 `IRegister` 与程序集扫描。
- **全局配置**：`TypeAdapterConfig.GlobalSettings` 可在启动时追加配置。
- **键控注册**：以键 `"mapster"` 注册 `ITypeAdapterFactory`，可与 AutoMapper 并存。
- **内置转换**：自动注册 `byte[]` 与 `long` 之间的转换。

## 快速开始

```csharp
using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(MapsterModule))]
public sealed class ApplicationModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<MapsterOptions>(options =>
        {
            options.AddProfile<OrderMappingRegister>();
            options.AddProfiles(typeof(ApplicationModule).Assembly);
        });
    }
}
```

定义映射注册：

```csharp
using Mapster;

public sealed class OrderMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderDto>();
        config.NewConfig<OrderLine, OrderLineDto>();
    }
}
```

投影：

```csharp
var dto = order.ProjectedAs<OrderDto>();
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `MapsterModule` | 注册 Mapster 并设置当前工厂 |
| `MapsterTypeAdapterFactory` / `MapsterTypeAdapter` | `ITypeAdapterFactory` / `ITypeAdapter` 实现 |
| `MapsterOptions` | `IRegister`、程序集与配置委托注册 |
| `AddMapster()` | 注册 `TypeAdapterConfig` 与 `IMapper` |

> 说明：模块使用键控单例工厂，避免在 `ValidateScopes` 下捕获根作用域的 `IMapper`。

## 相关包

- [`Euonia.Mapping`](../Euonia.Mapping/README.md) —— 映射抽象
- [`Euonia.Mapping.Automapper`](../Euonia.Mapping.Automapper/README.md) —— AutoMapper 实现
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
