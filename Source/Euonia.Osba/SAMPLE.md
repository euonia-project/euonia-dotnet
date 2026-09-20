# Euonia.Osba 全景示例

> 一份从零到可运行的完整示例，覆盖业务对象、工厂、状态机、规则、执行器与权限体系。
> 权限的深入说明见 [Permission/README.md](Permission/README.md)，
> 设计动因与取舍见 [Permission/DESIGN.md](Permission/DESIGN.md)。

**本文的全部代码经过编译并实际运行验证**（端到端断言通过），可以直接放进项目使用。

---

## 0. 场景

一个「团队仓库」小系统，贯穿全文：

| 实体 | 说明 |
|---|---|
| `Repo` | 仓库（业务对象）：名称、所属团队、所有者、密级、是否公开 |
| `RepoRecord` | 仓库的读模型（查询用） |
| `PushCommand` | 命令对象：对某个仓库执行 push |

需要的能力：

- 创建/读取/重命名/删除仓库，以及 push 命令；
- 名称必填、格式受限、保留名拦截；
- 按团队划分数据可见范围；
- 按仓库**逐个**判定操作权限（`repo-a1` 可 push+delete、`repo-a2` 仅可 push）。

---

## 1. 装配

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

var services = new ServiceCollection();

// 注册 Osba 基础设施，并扫描程序集里的业务对象与权限模型
services.AddBusinessObject(typeof(Repo).Assembly);

// 应用自己的服务
services.AddSingleton<RepoStore>();                                  // 本示例用内存存储代替数据库
services.AddSingleton<RepoAcl>();                                    // 行级授权数据
services.AddSingleton<IScopeSubjectResolver, DemoSubjectResolver>();
services.AddSingleton(DemoUser.Dev);                                 // 当前用户（UserPrincipal）

var provider = services.BuildServiceProvider();

// 启动期校验：声明了权限模型或 [Permission] 却忘了注册解析器时，在这里失败
provider.ValidatePermissionSetup();

// 建立请求作用域并设置上下文
using var scope = provider.CreateScope();
BusinessContextAccessor.SetCurrent(scope.ServiceProvider);

var factory = scope.ServiceProvider.GetRequiredService<IObjectFactory>();
var guard = scope.ServiceProvider.GetRequiredService<IScopeGuard>();

// ... 业务代码 ...

BusinessContextAccessor.Clear();   // 用完清理（静态 AsyncLocal）
```

`AddBusinessObject` 会注册：`IActuator`、`BusinessContext` 与 `BusinessContextAccessor`、
`IObjectFactory`、`IPermissionChecker`、权限模型注册表、`IScopeGuard`，
并把扫描到的每个业务对象类型注册为 Transient。全部使用 `TryAdd*`——已注册的服务不会被覆盖。

> **两个容易踩的点**
> 1. `AddBusinessObject(...)` 的返回类型是 **`void`**，不能链式调用。
> 2. 手动 `CreateScope()` 的场景必须**先** `SetCurrent(scope.ServiceProvider)`，**再**解析
>    `BusinessContext`/工厂；用完 `Clear()`。ASP.NET Core 下由中间件自动完成，无需手写。

需要一个不含任何权限声明的用户（权限码只能来自授权数据）：

```csharp
public static class DemoUser
{
    public static UserPrincipal Dev => Create("dev");

    private static UserPrincipal Create(string userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(UserClaimTypes.Subject, userId)],
            "Bearer",
            ClaimTypes.Name,
            UserClaimTypes.Role);

        return new UserPrincipal(new ClaimsPrincipal(identity));
    }
}
```

---

## 2. 第一个业务对象

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

public sealed class Repo : EditableObject<Repo>
{
    // ① 属性元数据必须用 RegisterProperty 的返回值
    public static readonly PropertyInfo<string> IdProperty = RegisterProperty<string>(p => p.Id);
    public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);
    public static readonly PropertyInfo<string> TeamIdProperty = RegisterProperty<string>(p => p.TeamId);
    public static readonly PropertyInfo<string> OwnerIdProperty = RegisterProperty<string>(p => p.OwnerId);
    public static readonly PropertyInfo<string> LevelProperty = RegisterProperty<string>(p => p.Level);
    public static readonly PropertyInfo<bool> IsPublicProperty = RegisterProperty<bool>(p => p.IsPublic);

    // ② 属性读写一律走 GetProperty / SetProperty
    public string Id { get => GetProperty(IdProperty); private set => SetProperty(IdProperty, value); }

    [Required(ErrorMessage = "仓库名不能为空。")]
    [StringLength(64, MinimumLength = 3, ErrorMessage = "仓库名长度必须在 3 到 64 之间。")]
    public string Name { get => GetProperty(NameProperty); set => SetProperty(NameProperty, value); }

    public string TeamId { get => GetProperty(TeamIdProperty); set => SetProperty(TeamIdProperty, value); }

    public string OwnerId { get => GetProperty(OwnerIdProperty); set => SetProperty(OwnerIdProperty, value); }

    public string Level { get => GetProperty(LevelProperty); set => SetProperty(LevelProperty, value); }

    public bool IsPublic { get => GetProperty(IsPublicProperty); set => SetProperty(IsPublicProperty, value); }

    // ③ 工厂方法（见 §3）
    [FactoryCreate]
    [Permission("repo:create")]
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

        // 加载用 LoadProperty（标记为「未更改」），不要用 SetProperty
        LoadProperty(IdProperty, record.Id);
        LoadProperty(NameProperty, record.Name);
        LoadProperty(TeamIdProperty, record.TeamId);
        LoadProperty(OwnerIdProperty, record.OwnerId);
        LoadProperty(LevelProperty, record.Level);
        LoadProperty(IsPublicProperty, record.IsPublic);

        return Task.CompletedTask;
    }

    [FactoryInsert]
    protected override Task InsertAsync(CancellationToken cancellationToken = default)
    {
        BusinessContext.GetRequiredService<RepoStore>().Repos[Id] = ToRecord();
        return Task.CompletedTask;
    }

    [FactoryUpdate]
    [Permission("repo:push")]
    protected override Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        BusinessContext.GetRequiredService<RepoStore>().Repos[Id] = ToRecord();
        return Task.CompletedTask;
    }

    [FactoryDelete]
    [Permission("repo:delete")]
    protected override Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        BusinessContext.GetRequiredService<RepoStore>().Repos.Remove(Id);
        return Task.CompletedTask;
    }

    protected override void AddRules()
    {
        Rules.AddRule(new CommonRule.Required(NameProperty, "仓库名不能为空。"));
        Rules.AddRule(new CommonRule.Regular(NameProperty, "^[a-z0-9-]+$", "仓库名只能包含小写字母、数字和连字符。"));
        Rules.AddRule<Repo>(NameProperty, repo => repo.Name != "admin", "该名称被保留。");
    }

    private RepoRecord ToRecord() => new(Id, Name, TeamId, OwnerId, Level, IsPublic);
}
```

配套的读模型与内存存储：

```csharp
public sealed record RepoRecord(string Id, string Name, string TeamId, string OwnerId, string Level, bool IsPublic);

public sealed class RepoStore
{
    public Dictionary<string, RepoRecord> Repos { get; } = [];
}
```

要点：

- `RegisterProperty<TValue>(p => p.Xxx)` 的 `TValue` **必须显式写**——它不出现在参数表里，编译器推断不出来。
- **务必使用返回值**。自己 `new PropertyInfo<T>(...)` 会因注册表里没有而抛 `ArgumentOutOfRangeException`。
- 声明为 `public static readonly` 字段：注册依赖静态字段初始化。
- 只读加载用 `LoadProperty`（不标脏），用户改动用 `SetProperty`（标脏）。
- `string` 属性的默认值是 `string.Empty` 而不是 `null`（`SetProperty` 也会把 `null` 归一成 `string.Empty`）。
- 重写 `InsertAsync`/`UpdateAsync`/`DeleteAsync` 时用 **`protected override`**（它们在跨程序集时看成 `protected`），
  并保留 `CancellationToken cancellationToken = default` 默认值。

### 2.1 状态机与变更追踪

`EditableObject<T>` 继承自 `ObservableObject<T>`，后者就是一个状态机：

```csharp
var repo = new Repo();
repo.BusinessContext = scope.ServiceProvider.GetRequiredService<BusinessContext>();

repo.State;                 // ObjectEditState.None
repo.MarkAsNew();           // → New       （保存时走 Insert）
repo.Name = "pay-web";      // 属性改动
repo.HasChangedProperties;  // true
repo.ChangedProperties;     // 改过的属性清单
repo.IsSavable;             // IsValid && 有改动 && !IsBusy

repo.MarkAsChanged();       // → Changed   （保存时走 Update）
repo.MarkAsDeleted();       // → Deleted   （保存时走 Delete）
repo.MarkAsClean();         // → None，并清空变更记录（virtual，可重写）
repo.AcceptChanges();       // 同上
```

> `MarkAsNew()` / `MarkAsChanged()` / `MarkAsDeleted(bool checkObjectRules = false)` **都不是 virtual**；
> 只有 `MarkAsClean()` 可重写。
>
> `MarkAsDeleted(true)` 才会让「删除」也执行对象级规则——默认**不**执行。
>
> 直接 `new` 出来的对象**不会**被注入依赖，必须手工设置 `BusinessContext`，否则 `AddRules()` 永不执行、`IsValid` 恒为 `true`。

---

## 3. 工厂：五种操作

```csharp
// 创建：new 一个对象并由 [FactoryCreate] 方法初始化（工厂会自动 MarkAsNew）
var repo = await factory.CreateAsync<Repo>("pay-web");

// 读取：调用 [FactoryFetch] 方法把数据加载进对象
var loaded = await factory.FetchAsync<Repo>(repoId);

// 保存：按对象当前状态分派到 Insert / Update / Delete
loaded.Name = "pay-web-2";
loaded.MarkAsChanged();
await factory.SaveAsync(loaded);

// 直接指定操作（目标对象由工厂内部创建并填充）
await factory.InsertAsync<Repo>(repoId);
await factory.UpdateAsync<Repo>(repoId);
await factory.DeleteAsync<Repo>(repoId);     // 返回 Task，无结果

// 命令对象
await factory.ExecuteAsync(command);
```

也可以直接调用对象自己的 `SaveAsync`，它会先跑规则、再走工厂：

```csharp
repo.MarkAsNew();
await repo.SaveAsync();                      // 规则不通过会抛 ValidationException
await repo.SaveAsync(forceUpdate: true);     // 无改动也强制走 Update
```

### 3.1 工厂方法如何被找到

两种声明方式等价：

```csharp
[FactoryUpdate]                              // ① 有特性：方法名任意
private Task PersistAsync(CancellationToken cancellationToken = default) { ... }

[FactoryUpdate]                              // ② 没有特性也行：名字必须是约定名
protected override Task UpdateAsync(CancellationToken cancellationToken = default) { ... }
```

约定名（**大小写敏感**）：

| 操作 | 特性 | 可用的方法名 |
|---|---|---|
| 创建 | `[FactoryCreate]` | `Create`、`CreateAsync`、`FactoryCreate`、`FactoryCreateAsync` |
| 读取 | `[FactoryFetch]` | `Fetch`、`FetchAsync`、`FactoryFetch`、`FactoryFetchAsync` |
| 插入 | `[FactoryInsert]` | `Insert`、`InsertAsync`、`FactoryInsert`、`FactoryInsertAsync` |
| 更新 | `[FactoryUpdate]` | `Update`、`UpdateAsync`、`FactoryUpdate`、`FactoryUpdateAsync` |
| 删除 | `[FactoryDelete]` | `Delete`、`DeleteAsync`、`FactoryDelete`、`FactoryDeleteAsync` |
| 执行 | `[FactoryExecute]` | `Execute`、`ExecuteAsync`、`FactoryExecute`、`FactoryExecuteAsync` |

调用参数（`criteria`）与目标方法参数的匹配规则：

- 目标方法可以有**更多的尾部参数，但必须有默认值**——所以 `InsertAsync(CancellationToken = default)`
  能同时服务 `InsertAsync()` 与 `InsertAsync(ct)`；
- 参数**精确匹配**的重载优先于靠可选参数凑上的重载；
- 拼错名字 → `MissingMethodException`；加了 `[FactoryXxx]` 则不限名字。

---

## 4. 规则体系

规则分两类：**对象级**（`Property == null`，保存时执行）与**属性级**（绑定到某个已注册属性）。

```csharp
protected override void AddRules()
{
    // ① 属性级：DataAnnotations 由 AddDataAnnotations() 自动转成规则（在 AddRules 之前已执行）
    //    这里只需补充额外逻辑

    // ② 属性级：用具体规则类（绑定到 RegisterProperty 的返回值）
    Rules.AddRule(new RepoNameCheckRule(NameProperty));

    // ③ 属性级：内置规则
    Rules.AddRule(new CommonRule.Required(NameProperty, "仓库名不能为空。"));
    Rules.AddRule(new CommonRule.Regular(NameProperty, "^[a-z0-9-]+$", "仓库名只能包含小写字母、数字和连字符。"));

    // ④ 属性级：便捷 Lambda 重载（handler 收到的是「业务对象本身」，不是属性值）
    Rules.AddRule<Repo>(NameProperty, repo => repo.Name != "admin", "该名称被保留。");

    // ⑤ 对象级：无参构造 ⇒ Property == null，只在保存时执行
    Rules.AddRule<RepoConsistencyRule>();
}
```

自定义属性级规则：

```csharp
public sealed class RepoNameCheckRule(IPropertyInfo property) : RuleBase(property)
{
    public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
    {
        if (context.Target is not IEditableObject target)
        {
            return Task.CompletedTask;
        }

        // 依赖从业务上下文取；规则实例是类型级共享的，绝不能把请求态存进字段
        var store = ((IBusinessObject)target).BusinessContext?.GetService<RepoStore>();
        var name = target.ReadProperty(Property)?.ToString();

        if (store != null && store.Repos.Values.Any(record => record.Name == name))
        {
            context.AddErrorResult($"仓库名 '{name}' 已存在。");
        }

        return Task.CompletedTask;
    }
}
```

### 4.1 规则的触发与结果

```csharp
repo.IsValid;                                   // ErrorCount == 0
repo.GetBrokenRules();                          // BrokenRuleCollection（Error/Warning/Information）

await repo.SaveAsync();                         // 内部先 CheckObjectRulesAsync(true, ct)
                                                // 有 Error → 抛 ValidationException

repo.ValidationComplete += (_, _) => { };       // 规则跑完的事件

repo.SuspendRuleChecking();                     // 暂停检查
repo.ResumeRuleChecking();
```

只让规则在特定状态下生效：

```csharp
[ExecuteOnState(ObjectEditState.New)]           // 标在规则类上，只对 IEditableObject 目标生效
public sealed class RepoCreateOnlyRule : RuleBase { /* ... */ }
```

> **只有 `Error` 会让对象无效**（`IsValid => BrokenRules.ErrorCount == 0`）；Warning / Information 不阻断保存。
>
> **规则只在 `BusinessContext` 被赋值后才初始化**：setter 会触发 `AddRules()` 与 `AddDataAnnotations()`。
>
> `BusinessObject.Rules` 是 **`protected`**。要从外部驱动规则，得在派生类里暴露它：
> `public Rules PublicRules => Rules;`

---

## 5. 执行器：把「取对象 → 处理 → 保存」串成一条链

```csharp
var actuator = scope.ServiceProvider.GetRequiredService<IActuator>();

// 创建
var id = await actuator.For<Repo>()
                       .Create("repo-a1")                       // criteria：定位 [FactoryCreate] 方法
                       .Handle(repo =>
                       {
                           repo.TeamId = "TeamA";
                           repo.OwnerId = "dev";
                       })
                       .ExecuteAsync(CancellationToken.None)    // 分派到 SaveAsync
                       .ReturnAsync(repo => repo.Id);

// 更新
await actuator.For<Repo>()
              .Update(repoId)
              .Handle(repo => repo.Name = "pay-web-2")
              .ExecuteAsync(CancellationToken.None);

// 删除
await actuator.For<Repo>()
              .Delete(repoId)
              .ExecuteAsync(CancellationToken.None);
```

命令对象——**注意它需要同时提供 `[FactoryCreate]` 与 `[FactoryExecute]`**：

```csharp
public sealed class PushCommand : CommandObject<PushCommand>
{
    public string RepoId { get; set; }
    public string Note { get; set; }
    public bool Pushed { get; private set; }

    [FactoryCreate]     // 执行器先创建实例（这一步是必须的）
    private Task CreateAsync(string repoId, CancellationToken cancellationToken = default)
    {
        RepoId = repoId;
        return Task.CompletedTask;
    }

    [FactoryExecute]    // 命令体
    protected override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Pushed = true;
        return Task.CompletedTask;
    }
}

// 用法
await actuator.For<PushCommand>()
              .Execute(repoId, CancellationToken.None)
              .Handle(command => command.Note = "first")
              .ExecuteAsync(CancellationToken.None);
```

> **两处 `ct` 的区别**：`.Execute(ct)` 的参数进 `criteria`（用于**定位**工厂方法），
> `.ExecuteAsync(ct)` 的才是取消令牌。两处都要传。
>
> 执行器**没有 `Fetch`**——读取请直接用 `IObjectFactory.FetchAsync` 或 `BusinessContext.FetchAsync`。

自定义管道行为：

```csharp
public sealed class AuditBehavior<TTarget> : IActuatorBehavior<TTarget>
    where TTarget : BusinessObject<TTarget>
{
    public async Task<TTarget> HandleAsync(TTarget context, PipelineDelegate<TTarget, TTarget> next)
    {
        // 前置
        var result = await next(context);
        // 后置
        return result;
    }
}

// 注册后会被自动接入管道
services.AddScoped<IActuatorBehavior<Repo>, AuditBehavior<Repo>>();
```

---

## 6. 权限体系

两套权限共享同一份**授权数据**：由应用实现，从数据实时解析、按请求缓存。

### 6.1 声明权限码

```csharp
[FactoryInsert]
protected override Task InsertAsync(CancellationToken cancellationToken = default) { ... }   // 无权限码

[FactoryUpdate]
[Permission("repo:push")]                     // ← 这个码同时决定「类型级闸门」与「行级策略的键」
protected override Task UpdateAsync(CancellationToken cancellationToken = default) { ... }

[FactoryDelete]
[Permission("repo:delete")]
protected override Task DeleteAsync(CancellationToken cancellationToken = default) { ... }
```

> **`[Permission]` 的码不只是「有没有这个权限」，它还是行级策略的键。**
> 如果 `DeleteAsync` 上没有 `[Permission("repo:delete")]`，删除操作会解析到默认键 `@delete`
> 并回落到模型的默认策略——你在 `Declare` 里为 `"repo:delete"` 写的行级策略**根本不会生效**。

### 6.2 授权数据来源

权限码**来自授权数据，而不是令牌**——权限码多时不会撑爆 Token，且**取消授权立即生效**，
不需要重新签发令牌。

```csharp
/// <summary>行级授权数据（真实系统里是数据库里的一张 ACL 表）。</summary>
public sealed class RepoAcl
{
    public List<(string RepoId, string UserId, string Operation)> Entries { get; } = [];
}

public sealed class DemoSubjectResolver(RepoAcl acl) : IScopeSubjectResolver
{
    public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;

        if (userId == null)
        {
            return ValueTask.FromResult(ScopeSubjectSet.Empty);      // 匿名 → 空集合，fail-closed
        }

        var builder = ScopeSubjectSet.CreateBuilder()
                                     .AddCodes(["repo:create", "repo:push", "repo:delete"])   // 类型级
                                     .AddSelf(userId)                                          // 「本人」
                                     .Add(ScopeDimensions.Dept, "TeamA");                      // 可见范围

        // 行级：ACL 里逐条授权
        foreach (var operation in new[] { "repo:push", "repo:delete" })
        {
            builder.AddGrant(
                operation,
                "repo",
                acl.Entries.Where(entry => entry.UserId == userId && entry.Operation == operation)
                   .Select(entry => entry.RepoId));
        }

        return ValueTask.FromResult(builder.Build());
    }
}
```

### 6.3 资源侧声明

```csharp
public sealed class RepoScope : ScopeModel<Repo>
{
    public override void Define(ScopeModelBuilder<Repo> builder)
    {
        builder.Map(ScopeDimensions.Owner, r => r.OwnerId)
               .Map(ScopeDimensions.Dept, r => r.TeamId)
               .Map("repo", r => r.Id)                    // 资源标识也作为维度 → 行级权限的前提
               .Classify("level", r => r.Level);          // 分类属性：不参与授权
    }

    // 默认策略：未单独声明的操作都用它
    public override ScopePolicy<Repo> Policy =>
        ScopePolicy<Repo>.Any(
            ScopePolicy<Repo>.Self(),
            ScopePolicy<Repo>.Grant(ScopeDimensions.Dept));

    // 按权限码声明行级策略
    public override void Declare(ScopePolicySet<Repo> policies)
    {
        // 新建行还没有既有归属，用「既有行的范围」去约束它没有意义；
        // 「能不能创建」由类型级的 [Permission("repo:create")] 负责。
        policies.For(BusinessOperation.Create, ScopePolicy<Repo>.Where(_ => true));

        policies.For("repo:push", ScopePolicy<Repo>.Grant("repo"));
        policies.For("repo:delete", ScopePolicy<Repo>.Grant("repo"));
    }
}

// 读模型也需要自己的声明，否则查询侧不受约束
public sealed class RepoRecordScope : ScopeModel<RepoRecord>
{
    public override void Define(ScopeModelBuilder<RepoRecord> builder)
    {
        builder.Map(ScopeDimensions.Owner, r => r.OwnerId)
               .Map(ScopeDimensions.Dept, r => r.TeamId);
    }

    public override ScopePolicy<RepoRecord> Policy =>
        ScopePolicy<RepoRecord>.Any(
            ScopePolicy<RepoRecord>.Self(),
            ScopePolicy<RepoRecord>.Grant(ScopeDimensions.Dept));
}
```

**模型与策略写在同一个类型里**，所以结构上不可能出现「有模型没策略」。
模型由 `AddBusinessObject` 的扫描发现，并在**注册期**完成校验（未映射维度、策略键歧义等都会启动即失败）。

> **未声明模型的类型不受任何数据权限约束**——这是当前边界。所以查询用的读模型也要单独声明，
> 否则 `guard.Apply` 会原样返回。

### 6.4 判定

```csharp
// 读侧：下推到数据库（生成的是表达式树，由 EF/提供程序翻成 WHERE）
var visible = await guard.Apply(dbContext.Repos).ToListAsync();

// 单行判定
guard.Allows(repo);                                  // 按当前对象状态对应的操作解析键
guard.Allows(repo, "repo:delete");                   // 指定权限码
guard.Explain(repo, "repo:delete");                  // 审计：命中了哪条策略

// 业务对象内部
protected bool CanDelete()
{
    return CanAccessRow("repo:delete") && HasPermission("repo:delete");
}
```

**判定语义**：策略编译成 `Allow`/`Deny` 一对表达式，结论恒为 `Allow && !Deny`。
`Deny` 是**否决**（压过一切、且一律上浮），不是布尔取反——需要取反请用
`ScopePolicy<Repo>.Where(r => !...)`。

### 6.5 写侧强制与规则互通

保存时有两道关卡：

| 时机 | 结果 |
|---|---|
| 规则阶段（框架对已声明模型的类型**自动注入**范围规则） | `ValidationException` |
| 工厂边界（criteria 入口、或规则被跳过时） | `SecurityException` |

```csharp
try
{
    repo.MarkAsChanged();
    await repo.SaveAsync();
}
catch (ValidationException ex)          // 规则阶段拦下（新增/更新）
{
    foreach (var error in ex.Errors) { Console.WriteLine(error.ErrorMessage); }
}
catch (SecurityException ex)            // 工厂边界拦下（越权删除走这里）
{
    Console.WriteLine($"越权：{ex.Message}");
}
```

> 越权**删除**抛的是 `SecurityException` 而不是 `ValidationException`：
> `EditableObject<T>` 在 `IsDeleted` 时**默认跳过对象级规则**。
> 需要让规则覆盖删除时，重写 `CheckObjectRulesOnDelete` 返回 `true`。

也可以把权限断言写进自己的规则：

```csharp
protected override void AddRules()
{
    Rules.AddRule(new PermissionRule("repo:force-push"));   // 缺码即报验证错误
    // ScopePolicyRule 已自动注入，无需手工添加
}
```

---

## 7. 端到端

把上面的片段串起来，一次完整流程（这段代码已实际运行验证）：

```csharp
var services = new ServiceCollection();
services.AddBusinessObject(typeof(Repo).Assembly);
services.AddSingleton<RepoStore>();
services.AddSingleton<RepoAcl>();
services.AddSingleton<IScopeSubjectResolver, DemoSubjectResolver>();
services.AddSingleton(DemoUser.Dev);

var provider = services.BuildServiceProvider();
provider.ValidatePermissionSetup();

using var scope = provider.CreateScope();
BusinessContextAccessor.SetCurrent(scope.ServiceProvider);

var factory = scope.ServiceProvider.GetRequiredService<IObjectFactory>();
var guard = scope.ServiceProvider.GetRequiredService<IScopeGuard>();
var store = scope.ServiceProvider.GetRequiredService<RepoStore>();
var acl = scope.ServiceProvider.GetRequiredService<RepoAcl>();

// 1. 造两个属于 TeamA、所有者为 dev 的仓库
async Task<Repo> CreateAsync(string name, string teamId, string ownerId)
{
    var repo = await factory.CreateAsync<Repo>(name);
    repo.TeamId = teamId;
    repo.OwnerId = ownerId;
    repo.Level = "normal";
    repo.MarkAsNew();
    return await repo.SaveAsync();
}

var a1 = await CreateAsync("repo-a1", "TeamA", "dev");
var a2 = await CreateAsync("repo-a2", "TeamA", "dev");

// 别的团队的仓库：dev 既不是所有者、也不在授予的团队里
var b1 = await CreateAsync("repo-b1", "TeamB", "someone");

// 2. 写入行级授权：两个都能 push，只有 a1 能 delete
acl.Entries.Add((a1.Id, "dev", "repo:push"));
acl.Entries.Add((a2.Id, "dev", "repo:push"));
acl.Entries.Add((a1.Id, "dev", "repo:delete"));
guard.Refresh();                                   // 授权数据变了，让本作用域的缓存失效

// 3. 读侧：只有本团队/本人的仓库可见
var visible = guard.Apply(store.Repos.Values.AsQueryable()).ToList();
Console.WriteLine(string.Join(", ", visible.Select(r => r.Name)));   // repo-a1, repo-a2

// 4. 行级操作权限：同一用户、同一类型，不同仓库结论不同
Console.WriteLine(guard.Allows(a2, "repo:push"));     // True
Console.WriteLine(guard.Allows(a2, "repo:delete"));   // False ← 这就是「行级操作权限」

// 5. 越权删除被工厂边界拦下
try
{
    a2.MarkAsDeleted();
    await a2.SaveAsync();
}
catch (SecurityException ex)
{
    Console.WriteLine($"越权删除被拒绝：{ex.Message}");
    // Data scope denied. Delete (before): Repo. [code=repo:delete] 判定：拒绝；…
}

// 6. 命令对象走执行器
var actuator = scope.ServiceProvider.GetRequiredService<IActuator>();
await actuator.For<PushCommand>()
              .Execute(a1.Id, CancellationToken.None)
              .Handle(command => command.Note = "first")
              .ExecuteAsync(CancellationToken.None);

BusinessContextAccessor.Clear();
```

---

## 8. 易错点清单

### 装配

1. `AddBusinessObject(...)` 返回 **`void`**，不能链式调用；它用 `TryAdd*`，已注册的服务不会被覆盖。
2. 它**不注册** `IObjectActivator` 与 `ILazyServiceProvider`。要用 `IHasLazyServiceProvider`，
   需先有 `ILazyServiceProvider`（由 `Euonia.Modularity` 的 `AddModularityApplication` 提供）。
3. 手动作用域：**先** `BusinessContextAccessor.SetCurrent(...)`，**再**解析 `BusinessContext`/工厂；用完 `Clear()`。

### 对象与属性

4. `RegisterProperty<TValue>` 全是 `protected static`；表达式重载**必须显式写 `TValue`**。
5. **必须使用 `RegisterProperty` 的返回值**，不要自己 `new PropertyInfo<T>(...)`。
6. `string` 属性的默认值是 `string.Empty`，不是 `null`。
7. 加载数据用 `LoadProperty`（不标脏），用户改动用 `SetProperty`（标脏）——不要混用。
8. `MarkAsNew` / `MarkAsChanged` / `MarkAsDeleted` **不是 virtual**；只有 `MarkAsClean` 是。
9. `MarkAsDeleted(true)` 才让删除也执行对象级规则，默认**不执行**。
10. `BusinessObject.Rules` 是 **`protected`**；`Rules.RuleManager` / `Rules.BrokenRules` 是 `internal`。
    外部只能读 `IsValid` / `GetBrokenRules()`，要驱动规则请在派生类里暴露。
11. `CheckRuleOnPropertyChanged` 恒为 `false` 且不能赋值——**属性变更不会自动触发规则检查**，
    需要时显式调用 `CheckPropertyRules(property)`。
12. 跨程序集重写工厂方法时用 **`protected override`**（不是 `protected internal override`）。

### 工厂

13. `CreateAsync`/`InsertAsync`/`UpdateAsync`/`DeleteAsync` 是 **`protected internal virtual`**。
14. `SaveAsync` 对外只有一个重载：`Task<T> SaveAsync(bool forceUpdate = false, CancellationToken = default)`；
    带 `userState` 的那个是 `protected`。
15. 工厂方法约定名**大小写敏感**，拼错即 `MissingMethodException`；加 `[FactoryXxx]` 则不限名字。
16. 目标方法多出的尾部参数**必须有默认值**；精确长度匹配优先。
17. `IObjectFactory.Create<T>` 会自动 `MarkAsNew()`——不要重复调用。
18. 同步的 `Create`/`Fetch` 内部用 `AsyncContext.Run` 阻塞执行异步工厂方法，避免在同步 API 里放重活。
19. **只有经工厂创建的对象才会被注入**（`[Inject]` 属性、`BusinessContext`）；`new` 出来的不会。
20. **命令对象走执行器时要同时提供 `[FactoryCreate]` 与 `[FactoryExecute]`**——
    执行器先 `CreateAsync` 造实例，再在终结阶段调 `ExecuteAsync`。

### 规则

21. `RulesExtensions.AddRule<T>(IPropertyInfo, ...)` 的 handler 收到的是**业务对象本身**，不是属性值；
    必须显式写 `<T>`。
22. `CommonRule.Lambda<T>` 的参数类型是 **`PropertyInfo<T>`**（不是 `IPropertyInfo`），且 handler 是**同步**的。
23. `CommonRule.Regular` **只支持 `string` 值**，其他类型抛 `NotSupportedException`。
24. `ExecuteOnStateAttribute` 标在**规则类**上，且只对 `IEditableObject` 目标生效。
25. `AddRule<TRule>()` 要求规则类有公共无参构造；需要依赖时用 `AddRule<TRule>(IServiceProvider)`。
26. 规则实例是**类型级共享的单例**——绝不能把请求态存进规则字段，一律从 `context.Target` 取。
27. 只有 `Error` 让对象无效；Warning/Information 不阻断保存。
28. **规则只在 `BusinessContext` 被赋值后才初始化**；不设上下文则 `AddRules()` 永不执行、`IsValid` 恒 `true`。

### 执行器

29. `Actuator` 类是 `internal`；对外只能注入 `IActuator` 后 `For<T>()`。
30. `ActuatorBuilderExtensions` **没有 `Fetch`**。
31. `.Execute(ct)` 的参数进 `criteria`，`.ExecuteAsync(ct)` 的才是取消令牌——两处都要传。
32. `IActuatorBehavior<T>` 需要注册到容器才会被自动接入管道。

### 权限

33. 权限码来自 `IScopeSubjectResolver`，**不在令牌里**；忘记注册解析器 →
    启动期 `ValidatePermissionSetup()` 失败（不调用它，首次判定也会报错，不会静默放行）。
34. **`[Permission]` 的码同时是行级策略的键**：漏写就会解析到 `@delete` 之类的默认键，
    你在 `Declare` 里写的行级策略不会生效。
35. `Self()` 等价于 `Grant(owner)`，解析器必须 `AddSelf(userId)` 才成立——漏了是 fail-closed，不会反向放行。
36. `Deny` 是**全局否决**且一律上浮，不是布尔取反。
37. 码级授予**覆盖**默认键（不是并集）；权限码通配（`repo:*`）**不参与**维度查找。
38. 越权新增/更新抛 `ValidationException`，越权删除抛 `SecurityException`（删除默认跳过对象级规则）。
39. **`factory.CreateAsync<T>(...)` 会立刻做数据范围判定**，而那时 `[FactoryCreate]` 方法刚填充完字段。
    若默认策略依赖行数据（如「本人或本团队」），创建会失败。
    正确做法是**为 `Create` 操作显式声明策略**（新建行没有既有归属，用既有行的范围约束它没有意义）。
40. 未声明 `ScopeModel<T>` 的类型不受数据权限约束——**读模型也要单独声明**。
41. 不要把 `Allow`/`Deny` 塞进 EF 全局查询过滤器——EF 按 DbContext 类型缓存模型，
    会把每用户不同的常量烘进缓存，导致**跨用户数据泄漏**。逐查询用 `guard.Apply(query)`。
