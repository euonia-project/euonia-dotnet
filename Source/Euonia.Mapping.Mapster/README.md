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

## 常见问题与故障排查

### `ProjectedAs` 抛 `NullReferenceException`
扩展方法通过 `TypeAdapterFactory.CreateAdapter()` 取当前工厂；未执行 `MapsterModule.OnApplicationInitialization`、未手动 `TypeAdapterFactory.SetCurrent(...)`，或当前工厂被其他映射后端覆盖时，`CreateAdapter()` 返回 `null`。确认模块已加载，且初始化顺序在首次投影之前。

### `MapsterOptions` 里注册的 Profile 没有生效
`AddMapster()` 在首次解析 `TypeAdapterConfig` 时应用 `MapsterOptions`；如果在此之前已经解析过 `IMapper`/`TypeAdapterConfig`，后续修改 options 不会重新应用。把 `services.Configure<MapsterOptions>(...)` 放在首次解析之前，或重启宿主。

### `ValidateScopes` 开启后启动失败
键控工厂 `MapsterTypeAdapterFactory` 依赖作用域的 `IMapper`，不能按普通单例注册；本包用键 `"mapster"` 注册键控单例并在内部构造 `ServiceMapper`。自定义注册时不要把 `IMapper` 捕获到单例，仓库的 `MapsterModuleTests` 覆盖了该场景。

### AutoMapper 与 Mapster 同时安装后行为不确定
`TypeAdapterFactory.SetCurrent` 是进程级静态状态，最后执行 `OnApplicationInitialization` 的模块获胜。一个进程只保留一个当前映射后端；需要并存时使用键控 `ITypeAdapterFactory` 显式解析，不要依赖全局投影。

### `config.NewConfig<,>()` 没有生效
`NewConfig` 是 Mapster 原生 API，只有在 `IRegister.Register` 被应用后才会进入配置。确认 Profile 已通过 `AddProfile`/`AddProfiles(Assembly[])` 注册，且 `AddMapster()` 在解析映射器前执行；扫描不到的程序集不会自动加载。

## 最佳实践

- 用 `[DependsOn(typeof(MapsterModule))]` 统一注册，避免手工 `AddMapster()` 与 `SetCurrent` 顺序不一致。
- 在模块 `ConfigureServices` 中注册 Profile，并确保首次解析 `IMapper` 发生在所有 `MapsterOptions` 配置之后。
- 业务代码优先使用全局 `ProjectedAs`；需要构造注入时使用作用域 `IMapper`，不要在单例中缓存。
- 同一进程只选 AutoMapper 或 Mapster 之一作为全局后端；多后端场景显式使用键控工厂。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`（`Source/common.props`）。
- 关键依赖：`Euonia.Mapping`、`Euonia.Modularity`、`Mapster` / `Mapster.DependencyInjection`；具体版本由仓库构建属性决定。
- 已知边界：`TypeAdapterConfig.GlobalSettings` 与 `TypeAdapterFactory` 都是进程级静态状态；`MapsterOptions` 只在首次构建配置时应用；内置转换只有 `byte[]` → `long`。

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。