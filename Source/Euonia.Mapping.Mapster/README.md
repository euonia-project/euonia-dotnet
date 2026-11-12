# Euonia.Mapping.Mapster

Euonia 对象映射抽象的 Mapster 后端：把 Mapster 的 `IMapper` 适配为 `ITypeAdapter`，并由 `MapsterModule` 在应用初始化时安装为当前映射工厂。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`
- **NuGet 包**：`Euonia.Mapping.Mapster`

> `ProjectedAs` / `ProjectedAsCollection` 扩展方法来自 `Euonia.Mapping`，位于**全局命名空间**；本包的模块、工厂与选项类型在 `Nerosoft.Euonia.Mapping`。

## 安装

```bash
dotnet add package Euonia.Mapping.Mapster
```

## 快速开始

通过模块启用（模块初始化时会调用 `TypeAdapterFactory.SetCurrent`）：

```csharp
using Mapster;
using Microsoft.Extensions.DependencyInjection;
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

定义映射注册。`config.NewConfig<,>()` 是 **Mapster 原生 API**，不是 Euonia 的扩展：

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

## 主要功能

- **映射工厂**：`MapsterTypeAdapterFactory` 实现 `ITypeAdapterFactory`，`MapsterTypeAdapter` 实现 `ITypeAdapter`。
- **模块集成**：`MapsterModule` 注册 Mapster 服务，并在 `OnApplicationInitialization` 中安装当前工厂。
- **映射注册**：`MapsterOptions.AddProfile` / `AddProfiles` 支持 `IRegister` 实例、类型、工厂与程序集扫描。
- **键控注册**：以键 `"mapster"` 注册 `ITypeAdapterFactory`，可与 AutoMapper 实现并存。
- **内置转换**：`AddMapster()` 自动应用 `ByteArrayToInt64Converter`，即 `byte[]` → `long` 的单向转换。
- **可选转换器**：`JsonValueConverter<TDest>` 提供 `string` JSON → `TDest` 的映射注册，需自行加入 `MapsterOptions`。

## 核心类型

| 类型 | 作用 |
|------|------|
| `MapsterModule` | 注册 Mapster 并设置当前工厂 |
| `MapsterTypeAdapterFactory` / `MapsterTypeAdapter` | `ITypeAdapterFactory` / `ITypeAdapter` 实现 |
| `MapsterOptions` | `IRegister`、程序集与配置委托注册 |
| `ByteArrayToInt64Converter` | `byte[]` → `long` 的内置 `IRegister` |
| `JsonValueConverter<TDest>` | `string` JSON → `TDest` 的 `IRegister` |
| `AddMapster()` | 注册 `TypeAdapterConfig` 与作用域 `IMapper` |

## 选项（代码配置）

本包没有 `IConfiguration` 读取点，也不存在 `Euonia:Mapping` 配置节。`MapsterOptions` 只能通过 `services.Configure<MapsterOptions>(...)` 代码配置。

| 成员 | 说明 |
|------|------|
| `AddProfile<TRegister>()` | 注册 `IRegister` 类型，构建配置时通过 `ActivatorUtilities` 创建实例 |
| `AddProfile<TRegister>(Func<TRegister>)` | 用工厂创建并注册实例 |
| `AddProfile(Type)` | 反射创建实例（要求可赋值给 `IRegister`） |
| `AddProfile(IRegister)` | 直接注册实例 |
| `AddProfiles(IEnumerable<IRegister>)` | 批量注册实例 |
| `AddProfiles(params Assembly[])` | 调用 `TypeAdapterConfig.Scan` 扫描程序集 |

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Mapping;

services.Configure<MapsterOptions>(options =>
{
    options.AddProfile(new OrderMappingRegister());
    options.AddProfiles(typeof(Program).Assembly);
});
```

> 该包是代码配置：不要为它编写 `appsettings.json` 节。

## 使用示例

在应用服务中直接投影，不需要注入 `IMapper`：

```csharp
public sealed class OrderQueryService
{
    public OrderDto Get(long id)
    {
        var order = LoadOrder(id);
        return order.ProjectedAs<OrderDto>();
    }

    private static Order LoadOrder(long id) => new() { Id = id, Number = "SO-1" };
}
```

需要构造注入时使用 Mapster 的 `IMapper`：

```csharp
using MapsterMapper;

public sealed class OrderQueryService(IMapper mapper)
{
    public OrderDto Get(Order order) => mapper.Map<Order, OrderDto>(order);
}
```

## 注意事项 / 限制

1. `TypeAdapterFactory.SetCurrent` 是进程级静态状态；同时安装 AutoMapper 与 Mapster 时，最后执行 `OnApplicationInitialization` 的模块获胜。
2. `MapsterTypeAdapterFactory` 以键 `"mapster"` 注册为键控单例，内部 `ServiceMapper` 持有单例 `TypeAdapterConfig`；`IMapper` 是 Scoped。不要在单例中捕获 `IMapper`，否则会触发 `ValidateScopes` 失败。
3. `AddMapster()` 在首次解析 `TypeAdapterConfig` 时应用 `MapsterOptions`；该单例构建完成后再修改 options 不会重新应用，需重启宿主。
4. 内置转换只有 `byte[]` → `long`；没有 `long` → `byte[]` 的反向映射，需要时自行注册。
5. `ProjectedAs` 依赖当前工厂；未通过模块初始化或手动调用 `TypeAdapterFactory.SetCurrent` 时，`CreateAdapter()` 返回 `null`，扩展方法会失败。
6. `AddProfile` 重载按注册类型去重（`TryAdd`），同一类型重复注册只保留第一次；`AddProfiles(Assembly[])` 添加的是 `Scan` 配置委托，不做去重。
7. 映射配置在全局 `TypeAdapterConfig.GlobalSettings` 上生效；多个宿主共享同一进程时不要假设配置隔离。

## 相关包

- [`Euonia.Mapping`](../Euonia.Mapping/README.md) —— 映射抽象与 `ProjectedAs`
- [`Euonia.Mapping.Automapper`](../Euonia.Mapping.Automapper/README.md) —— AutoMapper 实现
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。