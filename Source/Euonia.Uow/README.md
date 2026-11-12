# Euonia.Uow

Euonia 的工作单元（Unit of Work）抽象：用 `IUnitOfWork` 管理事务边界、上下文集合与提交/回滚生命周期，并通过 Castle DynamicProxy 拦截器把 `[UnitOfWork]` 方法包进独立作用域。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Uow`
- **NuGet 包**：`Euonia.Uow`

## 安装

```bash
dotnet add package Euonia.Uow
```

## 快速开始

通过模块启用（推荐；会注册拦截器并绑定 `Euonia:Uow`）：

```csharp
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Uow;

[DependsOn(typeof(UnitOfWorkModule))]
public sealed class ApplicationModule : ModuleContextBase
{
}
```

手动注册：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Uow;

services.AddUnitOfWork();
services.Configure<UnitOfWorkOptions>(options =>
{
    options.IsTransactional = true;
    options.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
    options.Timeout = TimeSpan.FromSeconds(30);
});
```

> `AddUnitOfWork()` 只注册 `IUnitOfWork`、`IUnitOfWorkAccessor` 与 `IUnitOfWorkManager`；`UnitOfWorkInterceptor` 与 `Euonia:Uow` 的绑定由 `UnitOfWorkModule` 完成。手动注册时需自行注册选项与拦截器。

## 主要功能

- **事务边界**：`CompleteAsync` 先保存所有上下文，再对事务性工作单元逐个提交；`RollbackAsync` 逐个回滚。
- **环境上下文**：`UnitOfWorkAccessor` 用 `AsyncLocal<T>` 保存当前工作单元，异步流之间互不干扰。
- **嵌套工作单元**：已存在当前工作单元且未要求新建时，`Begin` 返回 `ChildUnitOfWork` 代理，提交由根工作单元负责。
- **拦截器**：`UnitOfWorkInterceptor` 为标注 `[UnitOfWork]` 或实现 `IUnitOfWorkEnabled` 的方法创建独立 DI 作用域与工作单元。
- **上下文注册表**：`AddContext` / `GetOrAddContext` / `FindContext` 按键管理 `IUnitOfWorkContext`。
- **预留**：`Reserve(name)` 标记工作单元为独占，`UnitOfWorkAccessor` 会跳过已预留实例向外查找。

## 核心类型

| 类型 | 作用 |
|------|------|
| `IUnitOfWork` | 工作单元契约：保存、提交、回滚、上下文与服务解析 |
| `IUnitOfWorkManager` | 获取当前工作单元、开启新工作单元 |
| `IUnitOfWorkAccessor` | 基于 `AsyncLocal<T>` 的环境访问器 |
| `IUnitOfWorkContext` | 单个数据上下文的 `SaveChangesAsync` / `CommitAsync` / `RollbackAsync` |
| `UnitOfWorkOptions` / `IUnitOfWorkOptions` | 事务性、隔离级别、超时 |
| `UnitOfWorkAttribute` | 标注类/方法/接口启用工作单元 |
| `IUnitOfWorkEnabled` | 标记接口，按约定参与工作单元 |
| `UnitOfWorkModule` | 注册拦截器并绑定 `Euonia:Uow` |
| `UnitOfWorkInterceptor` | Castle `IInterceptor` 实现 |
| `UnitOfWorkManager` / `UnitOfWork` | 默认管理器与工作单元实现 |

## 配置项

`UnitOfWorkModule` 从 `Euonia:Uow` 绑定 `UnitOfWorkOptions`：

| 键 | 类型 | 默认值 | 说明 |
|----|------|--------|------|
| `IsTransactional` | `bool` | `false` | 是否在事务中执行；`[UnitOfWork]` 上的设置优先 |
| `IsolationLevel` | `IsolationLevel?` | `null` | 事务隔离级别；`null` 使用数据存储默认值 |
| `Timeout` | `TimeSpan?` | `null` | 完成操作的超时；`null` 使用系统默认值 |

```json
{
  "Euonia": {
    "Uow": {
      "IsTransactional": true,
      "IsolationLevel": "ReadCommitted",
      "Timeout": "00:00:30"
    }
  }
}
```

```bash
Euonia__Uow__IsTransactional=true        # .NET 原生：双下划线表示层级
EUONIA_UOW_ISTRANSACTIONAL=true          # 仓库 alias：大写单下划线
```

> `UnitOfWorkOptions.Normalize` 只用默认值补 `IsolationLevel` 与 `Timeout`，**不会**把 `IsTransactional` 改成 `true`；需要事务的方法应显式标注或在 `Begin` 时传入。

## 使用示例

手动开启并提交：

```csharp
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Uow;

public sealed class OrderBatchService(
    IUnitOfWorkManager manager,
    IRepository<Order, long> orders)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var uow = manager.Begin(
            new UnitOfWorkOptions(isTransactional: true),
            requiresNew: true);

        await orders.InsertAsync(new Order { Id = 1 }, autoSave: false, cancellationToken);

        await uow.CompleteAsync(cancellationToken);
    }
}

public sealed class Order : IEntity<long>
{
    public long Id { get; set; }

    public object[] GetKeys() => [Id];
}
```

用特性标注应用服务（沿用上例的 `Order` 实体；由 `Euonia.Application` 的代理管线拦截）：

```csharp
using System.Data;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Uow;

[UnitOfWork(true, IsolationLevel.ReadCommitted)]
public sealed class OrderAppService : IUnitOfWorkEnabled
{
    private readonly IRepository<Order, long> _orders;

    public OrderAppService(IRepository<Order, long> orders)
    {
        _orders = orders;
    }

    public Task CreateAsync(Order order, CancellationToken cancellationToken)
        => _orders.InsertAsync(order, autoSave: false, cancellationToken);
}
```

> `IUnitOfWorkManager.Begin()` 的第二个重载带默认参数，可直接调用 `manager.Begin()`（非事务）；不要写成静态的 `UnitOfWorkManager.Begin()`，`UnitOfWorkManager` 是实例服务。

## 注意事项 / 限制

1. `UnitOfWorkAttribute` 要生效，目标类型必须由 `Euonia.Application` 的拦截器管线代理；直接 `new` 的对象或未注册代理的类型不会触发工作单元。
2. 拦截器解析出的 `Timeout` 只用于 `CompleteAsync` 的 `CancellationToken`，不会取消目标方法本身；`UnitOfWorkAttribute.Timeout` 的类型是 `TimeSpan?`，C# 特性实参不支持 `TimeSpan`，因此超时请通过 `Euonia:Uow:Timeout` 或手动 `UnitOfWorkOptions` 设置。
3. 嵌套 `Begin` 在未指定 `requiresNew: true` 时返回 `ChildUnitOfWork`，其 `CompleteAsync` 是空操作；真正的提交与释放由根工作单元负责。
4. 事务性工作单元在未 `CompleteAsync` 就被 `Dispose` 时，会通过 `AsyncContext.Run` 同步回滚；非事务性工作单元不会回滚。
5. `UnitOfWork` 构造函数依赖 `IOptionsMonitor<UnitOfWorkOptions>`；手动 `AddUnitOfWork()` 时要确保选项基础设施已注册，否则解析会失败。
6. `IUnitOfWork.Items` 是普通 `Dictionary<string, object>`，不保证线程安全；跨线程共享需自行加锁。
7. 拦截器每次调用都会创建新的 DI 作用域，`uow.ServiceProvider` 与当前请求作用域不是同一个；不要假设两者解析到同一 Scoped 实例。
8. `Reserve(name)` 只影响 `UnitOfWorkAccessor.GetCurrentUnitOfWork()` 的查找：已预留、已释放或已完成的工作单元会被跳过并向外层查找。

## 相关包

- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 仓储抽象
- [`Euonia.Repository.EfCore`](../Euonia.Repository.EfCore/README.md) —— EF Core 上下文与工作单元工厂
- [`Euonia.Repository.Mongo`](../Euonia.Repository.Mongo/README.md) —— MongoDB 上下文实现
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— `UnitOfWorkModule` 的模块框架
- [`Euonia.Application`](../Euonia.Application/README.md) —— 拦截器管线

## 常见问题与故障排查

### 事务没有提交，数据却已经写入
`UnitOfWorkOptions.IsTransactional` 默认是 `false`，`Normalize` 也不会把它改成 `true`；非事务性工作单元只调用上下文的 `SaveChangesAsync`，不会开启数据库事务。另一个常见原因是仓储使用 `autoSave: true`，在 `CompleteAsync` 之前就提交了。解决：在 `Euonia:Uow:IsTransactional`、`[UnitOfWork(true, ...)]` 或 `Begin(new UnitOfWorkOptions(isTransactional: true))` 中显式开启事务，并在工作单元内统一使用 `autoSave: false`。

### 方法抛异常后没有回滚
拦截器在目标方法抛出异常时不会调用 `CompleteAsync`；事务性工作单元在 `Dispose` 时通过 `AsyncContext.Run` 回滚。若目标类型没有被 `Euonia.Application` 的代理管线拦截，`[UnitOfWork]` 完全不生效。检查模块是否依赖 `UnitOfWorkModule`、服务是否由 DI 创建，以及 `Options.IsTransactional` 是否为 `true`。

### 嵌套调用提前提交或提交无效
未指定 `requiresNew: true` 时，`Begin` 返回 `ChildUnitOfWork`，它的 `CompleteAsync` 是空操作，真正的提交由根工作单元负责；在子作用域里调用 `CompleteAsync` 不会落库。需要独立事务时显式传 `requiresNew: true`，并确保最外层调用 `CompleteAsync`。

### `[UnitOfWork(Timeout = ...)]` 编译失败
`UnitOfWorkAttribute.Timeout` 的类型是 `TimeSpan?`，C# 特性实参不支持 `TimeSpan`，因此无法在特性上直接设置。改用 `Euonia:Uow:Timeout` 或手动构造 `UnitOfWorkOptions.Timeout`；该超时只用于 `CompleteAsync` 的取消令牌，不会中断业务方法本身。

## 最佳实践

- 用环境变量覆盖事务开关：`Euonia__Uow__IsTransactional=true` 或 `EUONIA_UOW_ISTRANSACTIONAL=true`，避免把环境差异写进代码。
- 应用服务边界统一标注 `[UnitOfWork(true, IsolationLevel.ReadCommitted)]`，仓储统一 `autoSave: false`，只在最外层调用一次 `CompleteAsync`。
- 长事务拆成短工作单元；`Timeout` 只保护完成阶段，业务耗时仍需自己的 `CancellationToken` 与超时。
- 排障时先看 `IUnitOfWork.Current` 与 `Options.IsTransactional`，再检查 `UnitOfWorkAccessor` 是否因 `Reserve`、`Dispose` 或 `IsCompleted` 跳过了当前实例。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`（`Source/common.props`）。
- 关键依赖：`Euonia.Modularity`、Castle DynamicProxy、`Microsoft.Extensions.Options` / 依赖注入抽象；具体包版本由仓库构建属性决定，本文不固定版本号。
- 已知边界：`UnitOfWorkAttribute.Timeout` 无法用特性实参设置；`IUnitOfWork.Items` 不是线程安全字典；`AddUnitOfWork()` 不注册拦截器，需要 `UnitOfWorkModule` 或自行装配。

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
