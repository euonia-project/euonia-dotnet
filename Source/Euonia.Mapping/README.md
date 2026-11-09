# Euonia.Mapping

Euonia 的对象映射抽象：以 `ITypeAdapter` / `ITypeAdapterFactory` 定义投影契约，`TypeAdapter` 与扩展方法提供与具体映射库无关的调用入口，具体实现由 AutoMapper 或 Mapster 包提供。

Object mapping abstractions for Euonia: projection contracts defined by `ITypeAdapter` / `ITypeAdapterFactory`, with `TypeAdapter` and extension methods providing a library-agnostic entry point. Concrete implementations are supplied by the AutoMapper and Mapster packages.

- **目标框架**：`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`
- **NuGet 包**：`Euonia.Mapping`

## 安装

```bash
dotnet add package Euonia.Mapping
```

选择并添加一个实现：

```bash
dotnet add package Euonia.Mapping.Automapper
# 或
dotnet add package Euonia.Mapping.Mapster
```

## 主要功能

- **统一入口**：`TypeAdapter.ProjectedAs<T>()` 与 `source.ProjectedAs<T>()` 不暴露具体映射库。
- **集合投影**：`ProjectedAsCollection<T>()` 批量转换。
- **可插拔工厂**：`TypeAdapterFactory.SetCurrent(factory)` 指定当前适配器工厂。
- **目标类型投影**：`ProjectedAs(Type destinationType)` 支持运行时类型。
- **实现方解耦**：业务代码只依赖 `Euonia.Mapping`。

## 快速开始

使用模块自动配置（以 AutoMapper 为例）：

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
            options.AddMaps(typeof(ApplicationModule).Assembly);
        });
    }
}
```

投影对象：

```csharp
using Nerosoft.Euonia.Mapping;

var dto = order.ProjectedAs<OrderDto>();

var dto = TypeAdapter.ProjectedAs<Order, OrderDto>(order);

var items = orders.Cast<object>().ProjectedAsCollection<OrderDto>();
```

手动指定工厂（不使用模块时）：

```csharp
TypeAdapterFactory.SetCurrent(new AutomapperTypeAdapterFactory(mapper));
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `ITypeAdapter` | 投影契约：`Adapt<T>`、`Adapt(object, Type)` |
| `ITypeAdapterFactory` | 创建 `ITypeAdapter` 实例 |
| `TypeAdapterFactory` | 维护当前工厂（`SetCurrent` / `CreateAdapter`） |
| `TypeAdapter` | 静态投影入口 |
| `ProjectedAs<T>()` / `ProjectedAsCollection<T>()` | 扩展方法入口 |

## 相关包

- [`Euonia.Mapping.Automapper`](../Euonia.Mapping.Automapper/README.md) —— AutoMapper 实现
- [`Euonia.Mapping.Mapster`](../Euonia.Mapping.Mapster/README.md) —— Mapster 实现
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块初始化与依赖注入

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
