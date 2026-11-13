# Euonia.Uow 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

`Euonia.Uow` 用工作单元（Unit of Work）把多个仓储操作包进同一个事务边界，并提供环境上下文、嵌套工作单元与 Castle 拦截器。

- **包名**：`Euonia.Uow`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Uow`（DI 扩展位于 `Microsoft.Extensions.DependencyInjection`）
- **完整选项与边界**：[Source/Euonia.Uow/README.md](../../../Source/Euonia.Uow/README.md)

## 教程：用工作单元包住订单创建

这个场景从空 Web 项目开始：SQLite + EF Core 仓储，先用手动 `Begin/CompleteAsync` 创建订单，再用 `[UnitOfWork]` 标注应用服务，验证事务提交与异常回滚。

### 前置条件

- 已安装 .NET 10 或 .NET 11 SDK。
- 本机可写 SQLite 文件；示例不需要外部数据库。
- 已了解 `Euonia.Modularity` 的模块加载方式。

### 1. 创建项目并安装

```bash
dotnet new web -n SalesHost
cd SalesHost
dotnet add package Euonia.Uow
dotnet add package Euonia.Repository.EfCore
dotnet add package Euonia.Application
```

### 2. 实体与 EF Core 上下文

```csharp
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

public sealed class Order : IEntity<long>, IHasCreateTime
{
    public long Id { get; set; }
    public string Number { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }

    public object[] GetKeys() => [Id];
}

[ConnectionString(Name = "Default")]
public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options)
    : DataContextBase<SalesDbContext>(options)
{
    protected override bool AutoSetEntryValues => false;

    public DbSet<Order> Orders => Set<Order>();
}
```

### 3. 模块接线

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;
using Nerosoft.Euonia.Uow;

[DependsOn(typeof(ApplicationModule), typeof(RepositoryModule), typeof(UnitOfWorkModule))]
public sealed class SalesModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddEfCoreRepository<SalesDbContext>(options =>
            options.UseSqlite(Configuration.GetConnectionString("Default")));
        context.Services.AddProxiedScoped<IOrderAppService, OrderAppService>();
        context.Services.AddScoped<OrderBatchService>();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        db.Database.EnsureCreated();
    }
}
```

### 4. 手动事务与特性事务

```csharp
using System.Data;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Uow;

public interface IOrderAppService
{
    Task CreateAsync(Order order, CancellationToken cancellationToken);
}

[UnitOfWork(true, IsolationLevel.ReadCommitted)]
public sealed class OrderAppService : IOrderAppService
{
    private readonly IRepository<SalesDbContext, Order, long> _repository;

    public OrderAppService(IRepository<SalesDbContext, Order, long> repository)
    {
        _repository = repository;
    }

    public Task CreateAsync(Order order, CancellationToken cancellationToken)
        => _repository.InsertAsync(order, autoSave: false, cancellationToken);
}

public sealed class OrderBatchService(
    IUnitOfWorkManager manager,
    IRepository<SalesDbContext, Order, long> repository)
{
    public async Task CreateAsync(Order order, CancellationToken cancellationToken)
    {
        using var uow = manager.Begin(
            new UnitOfWorkOptions(isTransactional: true),
            requiresNew: true);

        await repository.InsertAsync(order, autoSave: false, cancellationToken);

        await uow.CompleteAsync(cancellationToken);
    }
}
```

### 5. 运行与验证

```bash
dotnet run
```

在端点中解析 `IOrderAppService` 并调用 `CreateAsync`，数据库应出现一行订单；把 `CreateAsync` 改成先插入再抛异常，再调用一次，事务性工作单元应在 `Dispose` 时回滚，数据库不新增行。手动路径可注入 `OrderBatchService`，观察 `CompleteAsync` 前数据不可见、调用后才提交。

### 常见失败点

- **事务没开**：`UnitOfWorkOptions.IsTransactional` 默认是 `false`，且 `Normalize` 不会把它改成 `true`；必须在 `Euonia:Uow`、`[UnitOfWork(true)]` 或 `Begin` 参数中显式开启。
- **数据提前写入**：仓储 `autoSave` 默认 `true`，工作单元内必须传 `autoSave: false`。
- **特性不生效**：`[UnitOfWork]` 只对接口代理生效；模块必须依赖 `ApplicationModule`，并用 `AddProxiedScoped<IOrderAppService, OrderAppService>()` 注册。
- **嵌套提交无效**：未传 `requiresNew: true` 时返回 `ChildUnitOfWork`，其 `CompleteAsync` 是空操作，由根工作单元负责提交。

## 相关文档

- [API 参考](API.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Uow/README.md)
- [Euonia.Repository 用户手册](../Euonia.Repository/TUTORIAL.md) —— 仓储抽象与连接串
- [Euonia.Repository.EfCore 用户手册](../Euonia.Repository.EfCore/TUTORIAL.md) —— EF Core 上下文与工作单元工厂
- [Euonia.Auditing 用户手册](../Euonia.Auditing/TUTORIAL.md) —— 在事务边界外记录审计
- [仓库根 README](../../../README.md)
