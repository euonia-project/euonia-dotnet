# Euonia.Mapping.Mapster API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

`Euonia.Mapping.Mapster` 把 Mapster 的 `IMapper` 适配为 Euonia 的 `ITypeAdapter`，并在模块初始化时安装为当前映射工厂。

- **包名**：`Euonia.Mapping.Mapster`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Mapping`；`ProjectedAs` 扩展位于全局命名空间
- **完整选项与边界**：[Source/Euonia.Mapping.Mapster/README.md](../../../Source/Euonia.Mapping.Mapster/README.md)

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

- [教程](TUTORIAL.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Mapping.Mapster/README.md)
- [Euonia.Mapping Source README](../../../Source/Euonia.Mapping/README.md) —— 映射抽象与 `ProjectedAs`
- [Euonia.Domain 用户手册](../Euonia.Domain/API.md) —— 聚合与 DTO 来源
- [Euonia.Repository 用户手册](../Euonia.Repository/API.md) —— 查询结果投影
- [仓库根 README](../../../README.md)
