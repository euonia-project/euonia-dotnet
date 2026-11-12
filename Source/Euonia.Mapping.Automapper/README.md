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

## 常见问题与故障排查

### `ProjectedAs` 抛 `NullReferenceException`
**原因**：`ProjectedAs` 通过全局 `TypeAdapterFactory.CreateAdapter()` 取适配器；没有调用 `TypeAdapterFactory.SetCurrent(...)`（模块未初始化或手动装配漏了这一步）时返回 `null`，随后调用 `Adapt` 触发空引用。
**解决**：模块化应用确认 `[DependsOn(typeof(AutomapperModule))]` 且应用已初始化；手动装配在解析 `IMapper` 后调用 `TypeAdapterFactory.SetCurrent(new AutomapperTypeAdapterFactory(mapper))`。

### Profile 没注册，映射时抛 AutoMapper 配置异常
**原因**：`AutomapperModule` 调用无参 `AddAutomapper()`，只应用 `AutomapperOptions` 中累积的 `AddMaps`/`AddProfile`；没有配置任何 Profile 时，`IMapper` 没有任何映射。
**解决**：在解析 `IMapper` 之前配置 `services.Configure<AutomapperOptions>(options => options.AddMaps(typeof(Program).Assembly))`；确认 Profile 在扫描程序集内且不是抽象/泛型类型。

### `validate: true` 没有在启动时报配置错误
**原因**：`AddMaps(..., validate: true)` / `AddProfile(..., validate: true)` 只把 Profile 加入校验列表；真正的 `AssertConfigurationIsValid()` 在 `IMapper` 构建阶段执行，而 `AddAutomapper()` 的映射器是首次解析时才创建。
**解决**：在启动冒烟测试中显式 `provider.GetRequiredService<IMapper>()`；或使用带 `assertConfiguration: true` 的 `AddAutomapper` 重载，让构建时立即断言。

### 找不到 `ProjectedAs` 或 `AddAutomapper`
**原因**：`ProjectedAs`/`ProjectedAsCollection` 在全局命名空间的 `Extensions` 类中，不在 `Nerosoft.Euonia.Mapping`；`AddAutomapper` 是 `Microsoft.Extensions.DependencyInjection` 的扩展方法。
**解决**：`ProjectedAs` 不需要额外 `using`；`AddAutomapper` 确保引入 `Microsoft.Extensions.DependencyInjection`；`AutomapperOptions`、`TypeAdapterFactory` 才需要 `using Nerosoft.Euonia.Mapping;`。

## 最佳实践

- 在首次解析 `IMapper` 前完成 `AutomapperOptions` 配置；模块化应用把配置放在依赖 `AutomapperModule` 的模块里，避免顺序问题。
- 启动时解析一次 `IMapper` 并开启 `validate: true`/`assertConfiguration: true`，把映射配置错误挡在启动阶段。
- Profile 按聚合/场景拆分并放在应用程序集，统一 `AddMaps(assembly)`；不要同时手工 `AddProfile` 同一类型造成重复配置。
- 需要与 Mapster 并存时保留 `"automapper"` 键控 `ITypeAdapterFactory`（模块已注册）；手动装配时按相同键注册并设置当前工厂。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`；包版本由仓库根 `project.props` 统一管理（当前 `2026.5.0`）。
- 关键依赖：AutoMapper 17.0.0、Newtonsoft.Json 13.0.4（由 `Directory.Packages.props` 锁定）；`AutomapperOptions` 为纯代码配置。
- 已知边界：`ProjectedAs` 在全局命名空间；`AutomapperModule` 注册 `IMapper` 与键控工厂，并在初始化时设置当前工厂；配置校验延迟到映射器构建。
- 升级提示：升级 AutoMapper 后复核 Profile 注册方式、`MapperConfigurationExpression` API 与 `AssertConfigurationIsValid` 行为。

## 相关包

- [`Euonia.Mapping`](../Euonia.Mapping/README.md) —— 映射抽象
- [`Euonia.Mapping.Mapster`](../Euonia.Mapping.Mapster/README.md) —— Mapster 实现
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与依赖注入

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
