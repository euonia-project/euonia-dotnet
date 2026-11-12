# Euonia.Mapping.Automapper 用户手册

`Euonia.Mapping.Automapper` 把 AutoMapper 的 `IMapper` 适配为 Euonia 的 `ITypeAdapter`，并通过 `AutomapperModule` 在应用初始化时安装全局映射工厂。

- **包名**：`Euonia.Mapping.Automapper`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`（`AddAutomapper` 位于 `Microsoft.Extensions.DependencyInjection`）
- **完整选项/边界**：[Source/Euonia.Mapping.Automapper/README.md](../../Source/Euonia.Mapping.Automapper/README.md)

## 教程：用模块接线 AutoMapper 并投影 DTO

这个场景用 `AutomapperModule` 注册 `IMapper` 与键控工厂，在业务模块中配置 Profile，初始化后通过 `ProjectedAs` 完成实体到 DTO 的映射。

### 前置条件

- .NET 10 或 .NET 11 SDK。
- 不要求数据库；示例使用内存对象。

### 1. 创建项目并安装

```bash
dotnet new console -n OrderMapping
cd OrderMapping
dotnet add package Euonia.Mapping.Automapper
```

### 2. 定义实体、DTO 与 Profile

```csharp
using AutoMapper;

public sealed class Order
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
}

public sealed class OrderDto
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
}

public sealed class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}
```

### 3. 用模块配置 Profile

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(AutomapperModule))]
public sealed class MappingModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<AutomapperOptions>(options =>
        {
            options.AddMaps(typeof(MappingModule).Assembly, validate: true);
        });
    }
}
```

### 4. 初始化并投影

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

var services = new ServiceCollection();
var configuration = ConfigurationHelper.BuildConfiguration(new ConfigurationBuilderOptions
{
    FileName = "appsettings",
    EnvironmentName = "Development"
});

using var application = ApplicationFactory.Create<MappingModule>(services, configuration);
using var provider = services.BuildServiceProvider();

// AutomapperModule 在初始化阶段调用 TypeAdapterFactory.SetCurrent。
application.Initialize(provider);

var order = new Order { Id = 42, CustomerName = "Ada" };
var dto = order.ProjectedAs<OrderDto>();

Console.WriteLine($"{dto.Id}:{dto.CustomerName}");
```

### 5. 运行与验证

```bash
dotnet run
```

预期输出：

```text
42:Ada
```

如果启动时抛出 AutoMapper 配置异常，说明 `validate: true` 已生效；检查 `OrderProfile` 的 `CreateMap` 与目标成员是否匹配。

### 常见失败点

- **`ProjectedAs` 抛 `NullReferenceException`**：没有调用 `TypeAdapterFactory.SetCurrent`；模块化应用确认已加载 `AutomapperModule` 并执行初始化，手动装配则显式设置。
- **Profile 没注册**：`AutomapperModule` 使用无参 `AddAutomapper()`，只应用 `AutomapperOptions` 中累积的 `AddMaps`/`AddProfile`；没有配置时映射器为空。
- **启动没报配置错误**：`validate: true` 只加入校验列表，真正的 `AssertConfigurationIsValid` 在首次解析 `IMapper` 时执行；在启动测试中显式解析一次。
- **找不到扩展方法**：`ProjectedAs` 在全局命名空间，不需要 `using Nerosoft.Euonia.Mapping;`；`AddAutomapper` 在 `Microsoft.Extensions.DependencyInjection`。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `AutomapperModule` | 注册 `IMapper`、键控 `ITypeAdapterFactory` 并设置当前工厂 | 通过 `[DependsOn]` 引入 |
| `AutomapperOptions.AddMaps(Assembly, bool validate = false)` | 添加程序集中的 Profile | `validate` 加入校验列表 |
| `AutomapperOptions.AddProfile<TProfile>(bool validate = false)` | 添加 Profile 类型 | 要求 `TProfile : Profile, new()` |
| `AutomapperOptions.AddProfile<TProfile>(TProfile profile, bool validate = false)` | 添加 Profile 实例 | 适合无无参构造的 Profile |
| `AutomapperOptions.AddProfile(Type profileType, bool validate = false)` | 按类型添加 Profile | 不要求泛型约束 |
| `AutomapperOptions.AddProfile(ICollection<Type>, bool validate = false)` | 批量添加 | `null`/空集合静默返回 |
| `AutomapperOptions.Configure(Action<IServiceProvider, MapperConfigurationExpression>)` | 追加自定义映射配置 | 在构建映射器时执行 |
| `AddAutomapper()` | 注册 `IMapper` 单例 | 读取已配置的 `AutomapperOptions` |
| `AddAutomapper(IEnumerable<Type>? types, ..., bool assertConfiguration = false)` | 直接传入 Profile 类型 | 可立即断言配置 |
| `AddAutomapper(Action<MapperConfigurationExpression>, bool assertConfiguration = false)` | 用委托配置映射 | 适合小型场景 |
| `AutomapperTypeAdapterFactory` | `ITypeAdapterFactory` 的 AutoMapper 实现 | 构造函数接收 `IMapper` |
| `AutomapperTypeAdapter` | `ITypeAdapter` 的 AutoMapper 实现 | 支持泛型/非泛型适配 |
| `TypeAdapterFactory.SetCurrent(factory)` | 安装全局类型适配器工厂 | 手动装配必须调用 |
| `TypeAdapterFactory.CreateAdapter()` | 创建当前适配器 | 未设置时返回 `null` |
| `ProjectedAs<TDestination>(object source)` | 投影为新的目标实例 | 全局命名空间扩展方法 |
| `ProjectedAsCollection<TDestination>(IEnumerable<object>)` | 批量投影 | 返回 `List<TDestination>` |
| `Profile` / `MapperConfigurationExpression` | AutoMapper 原生类型 | Profile 中调用 `CreateMap` |

### 配置说明

`Euonia.Mapping.Automapper` 没有配置节或配置绑定；`AutomapperOptions` 是纯代码配置对象，因此本页不提供 appsettings 示例。需要并存 Mapster 时，保留 `AutomapperModule` 注册的键为 `"automapper"` 的 `ITypeAdapterFactory`。

## 相关文档

- [Source README：完整 Profile、选项与全局工厂边界](../../Source/Euonia.Mapping.Automapper/README.md)
- [Source README：映射抽象与全局命名空间](../../Source/Euonia.Mapping/README.md)
- [Euonia.Modularity 用户手册](./Euonia.Modularity.md) —— 模块初始化
- [Euonia.Application 用户手册](./Euonia.Application.md) —— 应用服务中的 DTO 投影
- [仓库根 README](../../README.md)
