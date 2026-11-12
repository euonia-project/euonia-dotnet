# Euonia.Mapping

Euonia 的对象映射抽象：以 `ITypeAdapter` / `ITypeAdapterFactory` 定义投影契约，`TypeAdapter` 与 `ProjectedAs*` 扩展方法提供与具体映射库无关的调用入口，具体实现由 AutoMapper 或 Mapster 包提供。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`（`ITypeAdapter`、`ITypeAdapterFactory`、`TypeAdapter`、`TypeAdapterFactory`）
- **NuGet 包**：`Euonia.Mapping`
- **扩展方法命名空间**：**全局命名空间**的 `Extensions` 类（`Source/Euonia.Mapping/Extensions.cs:11` 关闭了 CA1050）

> 因为 `Extensions` 位于全局命名空间，调用 `ProjectedAs<T>()` / `ProjectedAsCollection<T>()` **不需要** `using Nerosoft.Euonia.Mapping;`；只有使用 `TypeAdapter`、`TypeAdapterFactory` 或实现 `ITypeAdapter` 时才需要该 using。

## 安装

```bash
dotnet add package Euonia.Mapping
```

再选择一个实现包（二者择一即可）：

```bash
dotnet add package Euonia.Mapping.Automapper
```

或：

```bash
dotnet add package Euonia.Mapping.Mapster
```

## 快速开始

模块装配（以 AutoMapper 为例）：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(AutomapperModule))]
public sealed class ApplicationModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<AutomapperOptions>(options =>
        {
            options.AddProfile<OrderProfile>();
        });
    }
}
```

`AutomapperModule` 在 `ConfigureServices` 中调用 `AddAutomapper()`，并以键 `"automapper"` 注册 `ITypeAdapterFactory`；在 `OnApplicationInitialization` 中自动调用 `TypeAdapterFactory.SetCurrent(...)`（`Source/Euonia.Mapping.Automapper/AutomapperModule.cs:19-33`）。Mapster 侧对应 `MapsterModule`（键为 `"mapster"`）。

投影对象：

```csharp
using Nerosoft.Euonia.Mapping;   // 只有 TypeAdapter 需要；ProjectedAs 扩展方法在全局命名空间

var dto = order.ProjectedAs<OrderDto>();                         // 扩展方法

var typed = TypeAdapter.ProjectedAs<Order, OrderDto>(order);      // 静态门面

var items = orders.Cast<object>().ProjectedAsCollection<OrderDto>();
```

不使用模块时，需要自己指定工厂：

```csharp
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Mapping;

var services = new ServiceCollection();
services.AddAutomapper();

var mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
TypeAdapterFactory.SetCurrent(new AutomapperTypeAdapterFactory(mapper));
```

`SetCurrent` 必须在任何投影调用之前执行：`TypeAdapterFactory.CreateAdapter()` 在未设置工厂时返回 `null`（`Source/Euonia.Mapping/TypeAdapterFactory.cs:32-35`）。

## 主要功能

- **统一入口**：`TypeAdapter.ProjectedAs<T>()` 与 `source.ProjectedAs<T>()` 不暴露具体映射库。
- **集合投影**：`ProjectedAsCollection<T>()` 批量转换为 `List<T>`。
- **目标类型投影**：`ProjectedAs(this object source, Type destinationType)` 支持运行时类型。
- **就地填充**：`TypeAdapter.ProjectedAs<TSource, TDestination>(source, destination)` 复用已有目标实例。
- **可插拔工厂**：`TypeAdapterFactory.SetCurrent(factory)` 切换当前适配器工厂，业务代码只依赖 `Euonia.Mapping`。

## 核心类型

| 类型 | 命名空间 | 作用 |
|------|----------|------|
| `ITypeAdapter` | `Nerosoft.Euonia.Mapping` | 投影契约：`Adapt<TSource, TDestination>`、`Adapt<TDestination>(object)`、`Adapt(object, Type)`、两个就地填充重载 |
| `ITypeAdapterFactory` | `Nerosoft.Euonia.Mapping` | 单一成员 `ITypeAdapter Create()` |
| `TypeAdapterFactory` | `Nerosoft.Euonia.Mapping` | 静态类，维护当前工厂：`SetCurrent` / `CreateAdapter` |
| `TypeAdapter` | `Nerosoft.Euonia.Mapping` | 静态投影门面：`ProjectedAs<TSource, TDestination>`、`ProjectedAs<TDestination>(object)`、`ProjectedAsCollection<TDestination>` |
| `Extensions` | **全局命名空间** | 扩展方法：`ProjectedAs<TDestination>()`、`ProjectedAs(Type)`、`ProjectedAsCollection<TDestination>()` |

## 选项（代码配置）

本包没有 `IConfiguration` 读取点，也没有 `*Options` 类型，因此不提供 `appsettings` 配置节；全部通过代码装配：

| 装配点 | 说明 |
|---|---|
| `services.Configure<AutomapperOptions>(...)` | AutoMapper 实现包的选项：`AddMaps` / `AddProfile` / `Configure`，由 `AddAutomapper()` 在解析 `IMapper` 时应用 |
| `TypeAdapterFactory.SetCurrent(ITypeAdapterFactory)` | 进程级静态入口，决定所有 `ProjectedAs*` 调用使用的适配器 |
| `ITypeAdapterFactory` 实现 | `AutomapperTypeAdapterFactory(IMapper)`、`MapsterTypeAdapterFactory(IMapper)`，也可自行实现 |

## 使用示例

模型与 Profile：

```csharp
using AutoMapper;

public sealed class Order
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
```

装配、设置工厂并投影（`Program.cs`）：

```csharp
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Mapping;

var services = new ServiceCollection();

// 1. 提供 AutoMapper 配置：AddAutomapper() 在解析 IMapper 时读取 AutomapperOptions
services.Configure<AutomapperOptions>(options => options.AddProfile<OrderProfile>());
services.AddAutomapper();

// 2. 把工厂交给静态门面（模块化宿主中由 AutomapperModule 自动完成）
var provider = services.BuildServiceProvider();
TypeAdapterFactory.SetCurrent(new AutomapperTypeAdapterFactory(provider.GetRequiredService<IMapper>()));

// 3. 投影
var order = new Order { Id = "A-1", Amount = 12.5m };
OrderDto dto = order.ProjectedAs<OrderDto>();

var list = new List<object> { order }.ProjectedAsCollection<OrderDto>();
```

## 注意事项 / 限制

- `TypeAdapterFactory` 的当前工厂是**进程级静态状态**，同一进程内所有调用共享同一个工厂；多租户/多容器场景下不要在运行时反复切换。
- 未调用 `SetCurrent` 时 `CreateAdapter()` 返回 `null`，扩展方法随后会以 `NullReferenceException` 失败，而不是抛出带说明的异常。
- 扩展方法 `ProjectedAs<TDestination>(this object source)` 与 `ProjectedAsCollection<TDestination>(this IEnumerable<object> items)` 的形参是 `object` / `IEnumerable<object>`，值类型集合需要先 `Cast<object>()`，且泛型参数受 `where TDestination : class` 约束。
- `AutomapperModule` / `MapsterModule` 以 **keyed** 单例注册 `ITypeAdapterFactory`（键分别为 `"automapper"`、`"mapster"`）；按类型解析 `ITypeAdapterFactory` 会失败，需要工厂时请从 keyed 服务或自行注册。
- 两个实现包的 `AddAutomapper` / `AddMapster` 位于 `Microsoft.Extensions.DependencyInjection` 命名空间，而 `ITypeAdapter` 等在 `Nerosoft.Euonia.Mapping`，示例中两个 using 都要保留。
- 投影结果由所选实现库决定：嵌套类型、集合元素类型与 `null` 源的行为沿用 AutoMapper / Mapster 自身语义，本包不做额外兜底。

## 相关包

- [`Euonia.Mapping.Automapper`](../Euonia.Mapping.Automapper/README.md) —— AutoMapper 实现
- [`Euonia.Mapping.Mapster`](../Euonia.Mapping.Mapster/README.md) —— Mapster 实现
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块初始化与依赖注入
- [`Euonia.Core`](../Euonia.Core/README.md) —— 基础类型与扩展方法

## 常见问题与故障排查

### 调用 `ProjectedAs<T>()` 抛 `NullReferenceException`

**原因**：没有设置当前工厂，`TypeAdapterFactory.CreateAdapter()` 返回 `null`，随后对 `null` 调用 `Adapt`（`Source/Euonia.Mapping/TypeAdapterFactory.cs:32-35`）。
**解决**：启动时执行一次 `TypeAdapterFactory.SetCurrent(new AutomapperTypeAdapterFactory(mapper))`；模块化宿主引用 `AutomapperModule` / `MapsterModule` 会在初始化阶段自动完成。

### 编译报 `ProjectedAsCollection` 参数不匹配 / 泛型约束错误

**原因**：扩展方法签名是 `ProjectedAs<TDestination>(this object source)` 与 `ProjectedAsCollection<TDestination>(this IEnumerable<object> items)`，且要求 `where TDestination : class`（`Source/Euonia.Mapping/Extensions.cs:21/48`）。
**解决**：值类型集合先 `Cast<object>()`；目标类型必须是引用类型，值类型投影改用 `TypeAdapter` 的实现库原生 API。

### `GetRequiredService<ITypeAdapterFactory>()` 解析失败

**原因**：`AutomapperModule` / `MapsterModule` 以 **keyed** 单例注册工厂（键 `"automapper"` / `"mapster"`），普通按类型解析取不到（`Source/Euonia.Mapping.Automapper/AutomapperModule.cs:22`）。
**解决**：用 `GetKeyedService<ITypeAdapterFactory>("automapper")`，或按你的键自行 `AddSingleton<ITypeAdapterFactory, ...>()`。

### AutoMapper 报缺少类型映射配置

**原因**：`AddAutomapper()` 在解析 `IMapper` 时读取 `IOptions<AutomapperOptions>`，没有 `Configure<AutomapperOptions>` 就没有任何 Profile（`Source/Euonia.Mapping.Automapper/ServiceCollectionExtensions.cs:61-68`）。
**解决**：`services.Configure<AutomapperOptions>(o => o.AddProfile<OrderProfile>())` 或 `o.AddMaps(assembly, validate: true)`；后者会把 Profile 加入启动校验。

### Mapster 在 `ValidateScopes` 下启动失败

**原因**：`MapsterTypeAdapterFactory` 依赖作用域的 `IMapper`，按类型注册为单例会捕获根作用域（`Source/Euonia.Mapping.Mapster/MapsterModule.cs:25-32`）。
**解决**：使用 `MapsterModule` 的 keyed 注册；自己接线时用 `new MapsterTypeAdapterFactory(new ServiceMapper(provider, provider.GetRequiredService<TypeAdapterConfig>()))`。

## 最佳实践

- **只在启动时 `SetCurrent` 一次**：静态工厂是进程级状态，运行时反复切换会影响所有并发请求。
- **优先用模块**：`AutomapperModule` / `MapsterModule` 同时完成服务注册与 `SetCurrent`，比手写接线少一个易漏步骤。
- **开启配置校验**：`AddMaps(assembly, validate: true)` 或 `AddProfile<T>(validate: true)` 让 `AssertConfigurationIsValid` 在启动阶段暴露漏映射。
- **投影只用于读模型**：`ProjectedAs` 面向 DTO/ViewModel，不要把 DTO 映射回聚合根，避免绕过领域行为与校验。
- **抽象包与实现包同版本**：升级 `Euonia.Mapping` 时同步升级 `Euonia.Mapping.Automapper` / `Euonia.Mapping.Mapster`。

## 版本与兼容

| 项 | 值 |
|---|---|
| 目标框架 | `net10.0`、`net11.0` |
| 包版本 | `2026.5.0`（仓库根 `project.props`） |
| 实现包依赖 | AutoMapper 17.0.0、Mapster / Mapster.DependencyInjection 10.0.14（`Directory.Packages.props`） |

- **全局命名空间的 `Extensions` 是兼容边界**：扩展方法位置不变，`ProjectedAs*` 的方法名与泛型约束属于公开契约。
- **实现库升级由实现包承担**：AutoMapper 17 / Mapster 10 的破坏性变更只会影响对应的 `Euonia.Mapping.*` 包，抽象包本身不感知。
- **静态工厂不提供并发切换语义**：多容器/多租户宿主需要隔离时应各自维护 `ITypeAdapter` 实例，而不是共享 `TypeAdapterFactory`。

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
