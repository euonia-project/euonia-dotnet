# Euonia.Mapping.Automapper

Euonia 对象映射的 AutoMapper 实现：将 `IMapper` 适配为 `ITypeAdapter`，并通过 `AutomapperModule` 在模块初始化时设为当前映射工厂。

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

## 选项（代码配置）

`Euonia.Mapping.Automapper` 没有配置节或 `*Options` 配置绑定；`AutomapperOptions` 是纯代码配置对象：

| 配置点 | 说明 |
|--------|------|
| `AutomapperOptions.AddMaps(Assembly, bool validate = false)` | 添加程序集中的 Profile；`validate` 为 `true` 时把 Profile 类型加入校验列表 |
| `AutomapperOptions.AddProfile<TProfile>(bool validate = false)` | 添加 Profile 类型，要求 `TProfile : Profile, new()` |
| `AutomapperOptions.AddProfile<TProfile>(TProfile profile, bool validate = false)` | 添加 Profile 实例，适用于没有无参构造函数的 Profile |
| `AutomapperOptions.AddProfile(Type profileType, bool validate = false)` | 按类型添加 Profile |
| `AutomapperOptions.AddProfile(ICollection<Type> profileTypes, bool validate = false)` | 批量添加；`null`、空集合或全 `null` 时直接返回 |
| `AutomapperOptions.Configure(Action<IServiceProvider, MapperConfigurationExpression>)` | 追加自定义映射配置委托 |
| `AddAutomapper(..., bool assertConfiguration = false)` | 构建映射器时是否立即执行 `AssertConfigurationIsValid()` |

## 注意事项 / 限制

- `AutomapperOptions` 在解析 `IMapper` 时读取；必须在首次解析之前完成 `services.Configure<AutomapperOptions>(...)`，之后再修改不会影响已创建的映射器。
- `AddAutomapper()` 注册的是 `IMapper` 单例；键为 `"automapper"` 的 `ITypeAdapterFactory` 由 `AutomapperModule` 注册。手动装配时还需要调用 `TypeAdapterFactory.SetCurrent(...)`，否则 `ProjectedAs` 会得到 `null` 适配器并抛出 `NullReferenceException`。
- `AutomapperModule` 调用的是无参 `AddAutomapper()`，只应用 `AutomapperOptions` 中累积的配置；没有配置任何 `AddMaps`/`AddProfile` 时，映射器没有任何 Profile。
- `AddProfile<TProfile>(bool validate = false)` 约束 `new()`；Profile 没有无参构造函数时改用实例重载或 `AddProfile(Type)`。
- `AddProfile(ICollection<Type>)` 对 `null`、空集合或全 `null` 集合静默返回；集合中的 `null` 项会被跳过。
- `ProjectedAs`/`ProjectedAsCollection` 定义在**全局命名空间**的 `Extensions` 类中，不需要 `using Nerosoft.Euonia.Mapping;`；`TypeAdapterFactory`、`ITypeAdapter`、`ITypeAdapterFactory` 才在 `Nerosoft.Euonia.Mapping` 命名空间。
- `validate: true` 只是把 Profile 加入校验列表；真正的 `AssertConfigurationIsValid()` 在映射器构建阶段执行，配置错误会在首次解析 `IMapper` 时暴露。

## 相关包

- [`Euonia.Mapping`](../Euonia.Mapping/README.md) —— 映射抽象
- [`Euonia.Mapping.Mapster`](../Euonia.Mapping.Mapster/README.md) —— Mapster 实现
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
