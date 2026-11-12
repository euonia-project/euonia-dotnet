# Euonia.Osba API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

面向对象的业务对象运行时：属性元数据与变更追踪、状态机、规则校验、反射驱动的工厂分派、执行器管道与权限边界。本页是 API 速查；架构与 50 条陷阱见 [项目 README](../../../Source/Euonia.Osba/README.md)。

- **NuGet 包**：`Euonia.Osba`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Osba`
- **依赖**：`Euonia.Core`、`Euonia.Modularity`、`Euonia.Pipeline`、`Euonia.Validation`（不引用 `Euonia.Security` / `Euonia.Domain`）
- **完整参考**：[Source/Euonia.Osba/README.md](../../../Source/Euonia.Osba/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddBusinessObject(params Assembly[])` / `AddBusinessObject(Action<OsbaOptions>, params Assembly[])` | 注册基础设施并扫描业务对象 | 选项以第一次注册为准 |
| `OsbaOptions` | 装配选项（代码配置） | `ValidateOnStartup=false`、`StrictFactoryMethods=true`、`EnableDiagnostics=true`、`DiagnosticLevel=Warning` |
| `EditableObject<T>` | 可编辑对象：属性 + 变更追踪 + 状态机 | `SaveAsync(bool forceUpdate, CancellationToken)` |
| `ReadOnlyObject<T>` / `ServiceObject<T>` | 只读对象 / 服务对象 | 无 `SaveAsync`；服务对象校验写对象级规则 |
| `RegisterProperty<TValue>` | 注册属性元数据 | 表达式 / 字符串名 / 默认值重载 |
| `GetProperty` / `SetProperty` / `LoadProperty` | 读 / 用户写 / 装载 | `LoadProperty` 不标脏、不触发规则 |
| `ChangedProperties` / `HasChangedProperties` / `RejectChanges()` | 变更追踪与回滚 | 改回基线自动移出 |
| `ValidateAsync(bool cascade, CancellationToken)` / `EnsureValidAsync(ct)` | 显式校验 | `cascade = true`；后者失败抛 `ValidationException` |
| `IActuator.For<TTarget>()` | 执行器入口 | `TTarget : ObjectBase<TTarget>` |
| `ActuatorBuilderExtensions` | `Fetch` / `Create` / `Update` / `Delete` / `Execute(params object[] criteria)` | `Update`/`Delete` 仅可编辑对象 |
| `ActuatorBase<T>` | `Handle(Action<T>)`、`WithRule(...)`、`BypassRule<TRule>()`、`ExecuteAsync(ct)` | 规则只作用于本次执行 |
| `BusinessContext` / `BusinessContextAccessor` | 业务上下文与手动作用域 | `GetRequiredService<T>()`、`SetCurrent`、`Clear` |
| `[FactoryCreate]` … `[FactoryDelete]` / `[FactoryExecute]` | 工厂方法标记 | 也可用约定名（`CreateAsync` 等，大小写敏感） |
| `CommonRule.Required` / `Regular` / `Lambda` | 内置属性规则 | 经 `Rules.AddRule(...)` 注册 |

`OsbaOptions` **没有配置节**，不读取 `IConfiguration`：全部选项在 `AddBusinessObject(options => ...)` 回调里以代码给出。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Osba/README.md) —— 架构设计、逐文件结构、陷阱清单
- [Euonia.Core 用户手册](../Euonia.Core/API.md) —— 本包依赖的权限契约与反射工具
- [Euonia.Security 用户手册](../Euonia.Security/API.md) —— 用 `AddPermission` 接上操作/数据权限
- [Euonia.Mapping 用户手册](../Euonia.Mapping/API.md) —— DTO 投影
- [仓库根 README](../../../README.md)
