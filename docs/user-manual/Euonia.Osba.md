# Euonia.Osba 用户手册

面向对象的业务对象运行时：属性元数据与变更追踪、状态机、规则校验、反射驱动的工厂分派、执行器管道与权限边界。本页是端到端教程与 API 速查；架构与 50 条陷阱见 [项目 README](../../Source/Euonia.Osba/README.md)。

- **NuGet 包**：`Euonia.Osba`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Osba`
- **依赖**：`Euonia.Core`、`Euonia.Modularity`、`Euonia.Pipeline`、`Euonia.Validation`（不引用 `Euonia.Security` / `Euonia.Domain`）
- **完整参考**：[Source/Euonia.Osba/README.md](../../Source/Euonia.Osba/README.md)

## 教程

### 场景：仓库对象的创建 → 读取 → 更新

后台服务管理「仓库（Repo）」：创建时按命名规则校验、读取时装载不标脏、更新时只写回变更字段。教程用内存字典代替数据库，专注对象模型与执行器。

### 前置条件

.NET 10 SDK；控制台项目；无需数据库（持久化由工厂方法自己实现）。

### 1. 安装

```bash
dotnet new console -n RepoAdmin && cd RepoAdmin
dotnet add package Euonia.Osba
```

### 2. 定义业务对象与工厂方法

```csharp
using System.ComponentModel.DataAnnotations;
using Nerosoft.Euonia.Osba;

public sealed class RepoStore
{
    public Dictionary<string, (string Name, string TeamId)> Repos { get; } = new();
}

public sealed class Repo : EditableObject<Repo>
{
    public static readonly PropertyInfo<string> IdProperty = RegisterProperty<string>(p => p.Id);
    public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);
    public static readonly PropertyInfo<string> TeamIdProperty = RegisterProperty<string>(p => p.TeamId);

    public string Id { get => GetProperty(IdProperty); private set => SetProperty(IdProperty, value); }

    [Required(ErrorMessage = "仓库名不能为空。")]
    [StringLength(64, MinimumLength = 3)]
    public string Name { get => GetProperty(NameProperty); set => SetProperty(NameProperty, value); }

    public string TeamId { get => GetProperty(TeamIdProperty); set => SetProperty(TeamIdProperty, value); }

    protected override void AddRules()
    {
        Rules.AddRule(new CommonRule.Required(NameProperty, "仓库名不能为空。"));
        Rules.AddRule(new CommonRule.Regular(NameProperty, "^[a-z0-9-]+$", "只能包含小写字母、数字和连字符。"));
    }

    [FactoryCreate]
    private Task CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        Id = Guid.NewGuid().ToString("N");
        Name = name;
        return Task.CompletedTask;
    }

    [FactoryFetch]
    private Task FetchAsync(string id, CancellationToken cancellationToken = default)
    {
        var store = BusinessContext.GetRequiredService<RepoStore>();

        if (!store.Repos.TryGetValue(id, out var record))
        {
            throw new InvalidOperationException($"仓库 {id} 不存在。");
        }

        LoadProperty(IdProperty, id);          // 装载：不标脏、不触发规则
        LoadProperty(NameProperty, record.Name);
        LoadProperty(TeamIdProperty, record.TeamId);
        return Task.CompletedTask;
    }

    [FactoryInsert]
    protected override Task InsertAsync(CancellationToken cancellationToken = default)
    {
        BusinessContext.GetRequiredService<RepoStore>().Repos[Id] = (Name, TeamId);
        return Task.CompletedTask;
    }

    [FactoryUpdate]
    protected override Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        BusinessContext.GetRequiredService<RepoStore>().Repos[Id] = (Name, TeamId);
        return Task.CompletedTask;
    }
}
```

### 3. 装配并调用执行器

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

var services = new ServiceCollection();
services.AddBusinessObject(options =>
{
    options.ValidateOnStartup = true;      // 启动期检查工厂签名、[Inject] 可解析性、属性元数据
    options.StrictFactoryMethods = true;   // 缺 Insert/Update/Delete 时保存直接失败
}, typeof(Repo).Assembly);
services.AddScoped<RepoStore>();

var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
BusinessContextAccessor.SetCurrent(scope.ServiceProvider);

try
{
    var actuator = scope.ServiceProvider.GetRequiredService<IActuator>();

    var repo = await actuator.For<Repo>()
                             .Create("pay-web")
                             .Handle(r => r.TeamId = "TeamA")
                             .ExecuteAsync();

    await actuator.For<Repo>()
                  .Update(repo.Id)
                  .Handle(r => r.Name = "pay-web-2")
                  .ExecuteAsync();

    var loaded = await actuator.For<Repo>().Fetch(repo.Id).ExecuteAsync();
    Console.WriteLine($"name={loaded.Name} state={loaded.State} changed={loaded.ChangedProperties.Count}");
}
finally
{
    BusinessContextAccessor.Clear();
}
```

### 4. 运行与验证

```bash
dotnet run
# 期望输出：
# name=pay-web-2 state=None changed=0
```

把 `.Create("pay-web")` 改成 `.Create("Pay Web")`，执行器会在保存前抛 `ValidationException`（`Errors` 含「只能包含小写字母、数字和连字符。」），对象不会落库。

### 常见失败点

- `Property 'X' is not registered.`：属性未注册或访问器名称不匹配；必须使用 `RegisterProperty` 的返回值。
- 状态迁移异常：对已删除对象再 `MarkAsChanged`；自定义迁移需重写 `CanTransitionTo`。
- 首次 `SaveAsync` 才报缺少持久化方法：`ValidateOnStartup` 默认 `false`，生产宿主显式开启。
- 声明 `[Permission]` 后抛异常：Osba 不注册权限服务，需宿主 `AddPermission` 或自建 `IPermissionChecker`。
- 后台任务拿不到 `BusinessContext`：用 `IServiceScopeFactory` 新建作用域并 `SetCurrent`，不要复用请求作用域。

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

- [项目 README](../../Source/Euonia.Osba/README.md) —— 架构设计、逐文件结构、陷阱清单
- [Euonia.Core 用户手册](Euonia.Core.md) —— 本包依赖的权限契约与反射工具
- [Euonia.Security 用户手册](Euonia.Security.md) —— 用 `AddPermission` 接上操作/数据权限
- [Euonia.Mapping 用户手册](Euonia.Mapping.md) —— DTO 投影
- [仓库根 README](../../README.md)
