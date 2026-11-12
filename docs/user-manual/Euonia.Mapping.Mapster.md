# Euonia.Mapping.Mapster 用户手册

`Euonia.Mapping.Mapster` 把 Mapster 的 `IMapper` 适配为 Euonia 的 `ITypeAdapter`，并在模块初始化时安装为当前映射工厂。

- **包名**：`Euonia.Mapping.Mapster`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`；`ProjectedAs` 扩展位于全局命名空间
- **完整选项与边界**：[Source/Euonia.Mapping.Mapster/README.md](../../Source/Euonia.Mapping.Mapster/README.md)

## 教程：把订单聚合投影为 API DTO

这个场景在 Web API 中注册 Mapster 模块，用一个 `IRegister` 定义 `Order → OrderDto`，分别通过全局 `ProjectedAs` 与注入的 `IMapper` 投影，并用端点验证。

### 前置条件

- 已安装 .NET 10 或 .NET 11 SDK。
- 已有一个基于 `Euonia.Modularity` 的 Web 项目；没有可从 [Modularity 用户手册](./Euonia.Modularity.md) 的最小宿主开始。
- 示例不需要数据库。

### 1. 安装

```bash
dotnet add package Euonia.Mapping.Mapster
```

### 2. 定义模型与映射注册

```csharp
using Mapster;

public sealed class Order
{
    public long Id { get; set; }
    public string Number { get; set; } = null!;
    public decimal Amount { get; set; }
}

public sealed class OrderDto
{
    public long Id { get; init; }
    public string Number { get; init; } = null!;
    public decimal Amount { get; init; }
}

public sealed class OrderMappingRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // NewConfig 是 Mapster 原生 API，不是 Euonia 的扩展。
        config.NewConfig<Order, OrderDto>();
    }
}
```

### 3. 模块接线

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(MapsterModule))]
public sealed class SalesModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<MapsterOptions>(options =>
        {
            options.AddProfile<OrderMappingRegister>();
            options.AddProfiles(typeof(SalesModule).Assembly);
        });

        context.Services.AddScoped<OrderAppService>();
    }
}
```

`AddProfiles(Assembly[])` 会调用 Mapster 的 `Scan`；如果已经用 `AddProfile<OrderMappingRegister>()` 显式注册，重复扫描同一类型不会产生额外影响。

### 4. 两种投影方式

```csharp
using MapsterMapper;

public sealed class OrderAppService(IMapper mapper)
{
    public OrderDto GetWithMapper(Order order) => mapper.Map<Order, OrderDto>(order);

    public OrderDto GetWithProjection(Order order) => order.ProjectedAs<OrderDto>();
}
```

`ProjectedAs` 使用全局 `TypeAdapterFactory`，需要模块初始化完成；构造注入的 `IMapper` 是作用域服务，适合在应用服务中使用。

### 5. 运行与验证

```csharp
app.MapGet("/orders/{id:long}", (long id, OrderAppService service) =>
{
    var order = new Order { Id = id, Number = $"SO-{id}", Amount = 99.5m };
    return Results.Ok(service.GetWithProjection(order));
});
```

```bash
dotnet run
curl http://localhost:5000/orders/42
```

预期返回 `{"id":42,"number":"SO-42","amount":99.5}`。把端点改成 `GetWithMapper`，结果应一致；若抛出空引用，检查模块是否加载。

### 常见失败点

- **`ProjectedAs` 空引用**：没有执行 `MapsterModule.OnApplicationInitialization`，或当前工厂被 AutoMapper 覆盖。
- **Profile 没生效**：首次解析 `IMapper` 发生在 `Configure<MapsterOptions>` 之前，之后修改 options 不会重建配置。
- **`ValidateScopes` 启动失败**：自定义注册把 `IMapper` 捕获进单例；使用模块的键控工厂或保持 `IMapper` 作用域。
- **双向转换缺失**：内置只有 `byte[]` → `long`，反向需要自己注册。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `MapsterModule` | 注册 Mapster 服务并安装当前工厂 | 依赖模块加载 |
| `MapsterOptions.AddProfile<TRegister>()` | 注册 `IRegister` 类型 | 构建配置时通过 DI 创建实例 |
| `AddProfile<TRegister>(Func<TRegister>)` | 用工厂创建注册实例 | 适合需要依赖的 Profile |
| `AddProfile(Type)` | 反射创建 `IRegister` | 类型必须可赋值给 `IRegister` |
| `AddProfile(IRegister)` | 直接注册实例 | 实例复用 |
| `AddProfiles(IEnumerable<IRegister>)` | 批量注册实例 | 按类型去重 |
| `AddProfiles(params Assembly[])` | 扫描程序集 | 调用 `TypeAdapterConfig.Scan` |
| `AddMapster()` | 注册 `TypeAdapterConfig` 与作用域 `IMapper` | `IMapper` 为 Scoped |
| `MapsterTypeAdapterFactory` | `ITypeAdapterFactory` 实现 | 模块以键 `"mapster"` 注册 |
| `MapsterTypeAdapter` | `ITypeAdapter` 实现 | 包装 `IMapper` |
| `ByteArrayToInt64Converter` | 内置 `byte[]` → `long` | 单向转换 |
| `JsonValueConverter<TDest>` | `string` JSON → `TDest` | 需自行注册 |
| `TypeAdapterFactory.SetCurrent(ITypeAdapterFactory)` | 设置全局工厂 | 进程级静态状态 |
| `ProjectedAs<TDestination>()` | 全局投影扩展 | 依赖当前工厂 |
| `ProjectedAsCollection<TDestination>()` | 集合投影扩展 | 输入 `IEnumerable<object>` |

### 配置

本包没有 `IConfiguration` 读取点，也没有 `Euonia:Mapping` 配置节；`MapsterOptions` 只能代码配置，不需要 `appsettings.json` 或环境变量。

## 相关文档

- [Source README：完整安装、选项与边界](../../Source/Euonia.Mapping.Mapster/README.md)
- [Euonia.Mapping Source README](../../Source/Euonia.Mapping/README.md) —— 映射抽象与 `ProjectedAs`
- [Euonia.Domain 用户手册](./Euonia.Domain.md) —— 聚合与 DTO 来源
- [Euonia.Repository 用户手册](./Euonia.Repository.md) —— 查询结果投影
- [仓库根 README](../../README.md)
