# Euonia.Mapping API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

与映射库无关的对象投影抽象：`ITypeAdapter` / `ITypeAdapterFactory` 定义契约，`TypeAdapter` 与 `ProjectedAs*` 扩展方法提供统一入口，具体实现由 AutoMapper 或 Mapster 包提供。本页是 API 速查；完整说明见 [项目 README](../../../Source/Euonia.Mapping/README.md)。

- **NuGet 包**：`Euonia.Mapping`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`（`ITypeAdapter` / `ITypeAdapterFactory` / `TypeAdapter` / `TypeAdapterFactory`）；`ProjectedAs*` 扩展方法在**全局命名空间**的 `Extensions` 类
- **实现包**：`Euonia.Mapping.Automapper`（AutoMapper 17.0.0）或 `Euonia.Mapping.Mapster`（Mapster 10.0.14）
- **完整参考**：[Source/Euonia.Mapping/README.md](../../../Source/Euonia.Mapping/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `ITypeAdapter.Adapt<TSource,TDestination>(source)` | 投影为新实例 | 两端 `: class` |
| `ITypeAdapter.Adapt<TSource,TDestination>(source, destination)` | 就地填充 | 返回填充后的目标 |
| `ITypeAdapter.Adapt<TDestination>(object)` / `Adapt(object, Type)` | 运行时类型投影 | — |
| `ITypeAdapterFactory.Create()` | 创建适配器 | 单成员接口 |
| `TypeAdapterFactory.SetCurrent(ITypeAdapterFactory)` | 设置当前工厂 | 进程级静态状态 |
| `TypeAdapterFactory.CreateAdapter()` | 从当前工厂创建适配器 | 未设置时返回 `null` |
| `TypeAdapter.ProjectedAs<TSource,TDestination>(source)` | 静态投影 | 另有 `(source, destination)` 重载 |
| `TypeAdapter.ProjectedAs<TDestination>(object)` | 单参数静态投影 | `TDestination : class` |
| `TypeAdapter.ProjectedAsCollection<TDestination>(IEnumerable<object>)` | 集合投影为 `List<T>` | 元素需为 `object` |
| `Extensions.ProjectedAs<TDestination>(this object)` | 扩展方法（全局命名空间） | 无需 `using Nerosoft.Euonia.Mapping` |
| `Extensions.ProjectedAs(this object, Type)` | 按运行时类型投影 | 返回 `object` |
| `Extensions.ProjectedAsCollection<TDestination>(this IEnumerable<object>)` | 集合扩展方法 | 需 `Cast<object>()` |
| `AutomapperOptions.AddProfile<TProfile>(bool validate)` | 注册 Profile | `validate = false` |
| `AutomapperOptions.AddMaps(Assembly, bool validate)` | 扫描程序集内 Profile | `validate = false` |
| `AutomapperOptions.Configure(Action<IServiceProvider, MapperConfigurationExpression>)` | 自定义映射配置 | 构建 `IMapper` 时执行 |
| `AddAutomapper()` / `AddAutomapper(Action<MapperConfigurationExpression>, bool)` | 注册 `IMapper` | 另有类型集合 / 工厂委托重载 |
| `AutomapperTypeAdapterFactory(IMapper)` / `MapsterTypeAdapterFactory(IMapper)` | 内置工厂实现 | 也可自行实现 `ITypeAdapterFactory` |
| `AutomapperModule` / `MapsterModule` | 模块入口，自动 `SetCurrent` | keyed 键 `"automapper"` / `"mapster"` |

本包**没有配置节**（不读取 `IConfiguration`）：工厂与 Profile 全部通过代码装配，不需要 `appsettings` 与环境变量。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Mapping/README.md) —— 命名空间说明、注意事项与版本兼容
- [Euonia.Mapping.Automapper 用户手册](../Euonia.Mapping.Automapper/API.md) —— AutoMapper 实现与选项
- [Euonia.Mapping.Mapster 用户手册](../Euonia.Mapping.Mapster/API.md) —— Mapster 实现
- [Euonia.Core 用户手册](../Euonia.Core/API.md) —— 基础类型与扩展方法
- [仓库根 README](../../../README.md)
