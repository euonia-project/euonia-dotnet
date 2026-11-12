# Euonia.Mapping.Automapper API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

`Euonia.Mapping.Automapper` 把 AutoMapper 的 `IMapper` 适配为 Euonia 的 `ITypeAdapter`，并通过 `AutomapperModule` 在应用初始化时安装全局映射工厂。

- **包名**：`Euonia.Mapping.Automapper`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`（`AddAutomapper` 位于 `Microsoft.Extensions.DependencyInjection`）
- **完整选项/边界**：[Source/Euonia.Mapping.Automapper/README.md](../../../Source/Euonia.Mapping.Automapper/README.md)

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

- [教程](TUTORIAL.md)
- [Source README：完整 Profile、选项与全局工厂边界](../../../Source/Euonia.Mapping.Automapper/README.md)
- [Source README：映射抽象与全局命名空间](../../../Source/Euonia.Mapping/README.md)
- [Euonia.Modularity 用户手册](../Euonia.Modularity/API.md) —— 模块初始化
- [Euonia.Application 用户手册](../Euonia.Application/API.md) —— 应用服务中的 DTO 投影
- [仓库根 README](../../../README.md)
