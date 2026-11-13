# Euonia.Mapping 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

与映射库无关的对象投影抽象：`ITypeAdapter` / `ITypeAdapterFactory` 定义契约，`TypeAdapter` 与 `ProjectedAs*` 扩展方法提供统一入口，具体实现由 AutoMapper 或 Mapster 包提供。本页是端到端教程；完整说明见 [项目 README](../../../Source/Euonia.Mapping/README.md)。

- **NuGet 包**：`Euonia.Mapping`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`（`ITypeAdapter` / `ITypeAdapterFactory` / `TypeAdapter` / `TypeAdapterFactory`）；`ProjectedAs*` 扩展方法在**全局命名空间**的 `Extensions` 类
- **实现包**：`Euonia.Mapping.Automapper`（AutoMapper 17.0.0）或 `Euonia.Mapping.Mapster`（Mapster 10.0.14）
- **完整参考**：[Source/Euonia.Mapping/README.md](../../../Source/Euonia.Mapping/README.md)

## 教程

### 场景：订单聚合到 DTO 的投影

订单查询服务要把领域对象 `Order` 投影成 `OrderDto` 返回给前端：单对象、集合、就地填充三种形态各用一次；宿主用 AutoMapper 模块装配，并在启动时把工厂交给静态门面。

### 前置条件

.NET 10 SDK；控制台或 ASP.NET Core 项目；不需要数据库。

### 1. 安装

```bash
dotnet new console -n OrderProjection && cd OrderProjection
dotnet add package Euonia.Mapping
dotnet add package Euonia.Mapping.Automapper
```

### 2. 定义模型与 Profile

```csharp
using AutoMapper;

public sealed class Order
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
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

### 3. 装配：注册映射并设置当前工厂

```csharp
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Mapping;

var services = new ServiceCollection();

// AddAutomapper() 在解析 IMapper 时读取 AutomapperOptions
services.Configure<AutomapperOptions>(options =>
{
    options.AddProfile<OrderProfile>(validate: true);   // validate=true 会加入启动校验
});

services.AddAutomapper();

var provider = services.BuildServiceProvider();

// 把工厂交给静态门面（模块化宿主中由 AutomapperModule 自动完成）
TypeAdapterFactory.SetCurrent(new AutomapperTypeAdapterFactory(provider.GetRequiredService<IMapper>()));
```

模块化宿主（`Euonia.Modularity`）等价写法：

```csharp
[DependsOn(typeof(AutomapperModule))]
public sealed class ApplicationModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<AutomapperOptions>(o => o.AddProfile<OrderProfile>());
    }
}
```

### 4. 三种投影形态

```csharp
var order = new Order { Id = "A-1", CustomerId = "C-1", Amount = 99.5m };

// ① 扩展方法（全局命名空间，不需要 using Nerosoft.Euonia.Mapping）
var dto = order.ProjectedAs<OrderDto>();

// ② 静态门面（显式指定源类型）
var typed = TypeAdapter.ProjectedAs<Order, OrderDto>(order);

// ③ 就地填充：复用已有实例
var target = new OrderDto();
TypeAdapter.ProjectedAs(order, target);

// ④ 集合投影：元素需为 object
var list = new List<object> { order }.ProjectedAsCollection<OrderDto>();

Console.WriteLine($"{dto.Id}/{dto.Amount} count={list.Count} same={ReferenceEquals(typed, order)}");
```

### 5. 运行与验证

```bash
dotnet run
# 期望输出：
# A-1/99.5 count=1 same=False
```

把 `TypeAdapterFactory.SetCurrent(...)` 注释掉再运行，会得到 `NullReferenceException`——这是「未设置工厂」的典型形态。

### 常见失败点

- **`NullReferenceException`**：没有 `SetCurrent`，`CreateAdapter()` 返回 `null`；模块宿主由 `AutomapperModule` / `MapsterModule` 自动设置。
- **`ProjectedAsCollection` 编译失败**：形参是 `IEnumerable<object>`，值类型集合需先 `Cast<object>()`，且 `TDestination : class`。
- **`GetRequiredService<ITypeAdapterFactory>()` 失败**：模块以 **keyed** 单例注册（键 `"automapper"` / `"mapster"`），需用 `GetKeyedService`。
- **AutoMapper 报缺少类型映射配置**：没有 `Configure<AutomapperOptions>`，`AddAutomapper()` 读不到任何 Profile。
- **Mapster 在 `ValidateScopes` 下启动失败**：工厂依赖作用域 `IMapper`，不要按类型注册为单例。

## 相关文档

- [API 参考](API.md)
- [项目 README](../../../Source/Euonia.Mapping/README.md) —— 命名空间说明、注意事项与版本兼容
- [Euonia.Mapping.Automapper 用户手册](../Euonia.Mapping.Automapper/TUTORIAL.md) —— AutoMapper 实现与选项
- [Euonia.Mapping.Mapster 用户手册](../Euonia.Mapping.Mapster/TUTORIAL.md) —— Mapster 实现
- [Euonia.Core 用户手册](../Euonia.Core/TUTORIAL.md) —— 基础类型与扩展方法
- [仓库根 README](../../../README.md)
