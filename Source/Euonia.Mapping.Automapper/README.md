# Euonia.Mapping.Automapper

Euonia 对象映射的 AutoMapper 实现：将 `IMapper` 适配为 `ITypeAdapter`，并通过 `AutomapperModule` 在模块初始化时设为当前映射工厂。

AutoMapper implementation for Euonia object mapping: adapts `IMapper` to `ITypeAdapter` and installs it as the current mapping factory during module initialization via `AutomapperModule`.

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`
- **NuGet 包**：`Euonia.Mapping.Automapper`

## 安装

```bash
dotnet add package Euonia.Mapping.Automapper
```

## 主要功能

- **映射工厂**：`AutomapperTypeAdapterFactory` 实现 `ITypeAdapterFactory`。
- **模块集成**：`AutomapperModule` 注册 AutoMapper 服务，并在应用初始化时调用 `TypeAdapterFactory.SetCurrent`。
- **Profile 管理**：`AutomapperOptions.AddMaps`、`AddProfile` 注册并校验 Profile。
- **键控注册**：以键 `"automapper"` 注册 `ITypeAdapterFactory`，可与 Mapster 并存。
- **配置校验**：`assertConfiguration` 控制是否在构建前断言配置有效。

## 快速开始

```csharp
using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(AutomapperModule))]
public sealed class ApplicationModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<AutomapperOptions>(options =>
        {
            options.AddMaps(typeof(ApplicationModule).Assembly, validate: true);
        });
    }
}
```

定义 Profile：

```csharp
using AutoMapper;

public sealed class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<OrderLine, OrderLineDto>();
    }
}
```

投影：

```csharp
var dto = order.ProjectedAs<OrderDto>();
```

不使用模块时手动注册：

```csharp
using AutoMapper;
using Nerosoft.Euonia.Mapping;

services.Configure<AutomapperOptions>(options =>
{
    options.AddMaps(typeof(Program).Assembly);
});

services.AddAutomapper();

var mapper = serviceProvider.GetRequiredService<IMapper>();
TypeAdapterFactory.SetCurrent(new AutomapperTypeAdapterFactory(mapper));
```

> `AddAutomapper()` 只注册 `IMapper`；键控工厂 `ITypeAdapterFactory`（键 `"automapper"`）由 `AutomapperModule` 负责注册。

## 核心类型

| 类型 | 作用 |
|------|------|
| `AutomapperModule` | 注册 AutoMapper 并设置当前工厂 |
| `AutomapperTypeAdapterFactory` / `AutomapperTypeAdapter` | `ITypeAdapterFactory` / `ITypeAdapter` 实现 |
| `AutomapperOptions` | Profile 与配置注册 |
| `AddAutomapper(...)` | 注册 `IMapper`（键控工厂由模块注册） |

## 相关包

- [`Euonia.Mapping`](../Euonia.Mapping/README.md) —— 映射抽象
- [`Euonia.Mapping.Mapster`](../Euonia.Mapping.Mapster/README.md) —— Mapster 实现
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
