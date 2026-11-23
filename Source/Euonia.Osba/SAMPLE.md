# Euonia.Osba 全景示例

> 一份从零到可运行的完整示例，覆盖业务对象、工厂、状态机、规则、执行器与权限体系。
> 权限的说明与示例见 [PERMISSION.md](PERMISSION.md) 与
> [PERMISSION-SAMPLE.md](PERMISSION-SAMPLE.md)，边界设计见 [PERMISSION-DESIGN.md](PERMISSION-DESIGN.md)。

**本文的全部代码可以直接放进项目使用。**

---

## 0. 场景

一个「团队仓库」小系统，贯穿全文：

| 实体 | 说明 |
|---|---|
| `Repo` | 仓库（业务对象）：名称、所属团队、所有者、密级、是否公开 |
| `RepoRecord` | 仓库的读模型（查询用） |
| `PushService` | 服务对象：对某个仓库执行 push |

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

// 扫描业务对象并注册工厂、业务上下文与对象类型
services.AddBusinessObject(typeof(Repo).Assembly);

// 应用自己的服务
services.AddSingleton<RepoStore>();   // 本示例用内存存储代替数据库

var provider = services.BuildServiceProvider();

using var scope = provider.CreateScope();
BusinessContextAccessor.SetCurrent(scope.ServiceProvider);

var factory = scope.ServiceProvider.GetRequiredService<IObjectFactory>();

// ... 业务代码 ...

BusinessContextAccessor.Clear();
```

`AddBusinessObject` 会注册 `IActuator`、`BusinessContext` 与 `BusinessContextAccessor`、
`IObjectFactory`，并把扫描到的每个业务对象类型注册为 Transient。全部使用 `TryAdd*`：
已注册的服务不会被覆盖。它**不注册任何权限服务**；权限由宿主在组合根按需装配。

> **两个容易踩的点**
> 1. `AddBusinessObject(...)` 返回 `IServiceCollection`，可以链式调用；需要启动期校验时用
>    `AddBusinessObject(o => o.ValidateOnStartup = true, assemblies)`。
> 2. 手动 `CreateScope()` 的场景必须**先** `SetCurrent(scope.ServiceProvider)`，**再**解析
>    `BusinessContext`/工厂；用完 `Clear()`。ASP.NET Core 下由中间件自动完成，无需手写。

## 2. 第一个业务对象

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

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

repo.GetOriginalValue(Repo.NameProperty); // 上次 AcceptChanges 时的值
repo.GetChangedValues();                  // 已改属性的当前值
repo.RejectChanges();                     // 回滚到原始值并清空变更记录
```

> `MarkAsNew()` / `MarkAsChanged()` / `MarkAsDeleted(bool checkObjectRules = false)` 都是 **virtual**，
> 统一经过 `TransitionTo`；派生类可重写 `CanTransitionTo` 表达领域状态机，并订阅 `StateChanged`。
> `MarkAsClean()` / `AcceptChanges()` 也是 virtual。
>
> `RejectChanges()` 把已更改属性恢复到 `AcceptChanges()` 时的基线；`AcceptChanges()` 则把当前值当作新基线。
>
> 属性被直接改回基线时（例如先改成 `"x"` 再改回 `""`），`FieldData.IsChanged` 会变回 `false`，该属性自动从 `ChangedProperties` 移除；子对象/集合内容变化仍保持父对象为脏。
>
> `MarkAsDeleted(true)` 才会让「删除」也执行对象级规则——默认**不**执行。
>
> 删除成功后对象会复位为 `None`，重复 `SaveAsync` 是幂等 no-op；不要再依赖“删除后仍是 Deleted”。
>
> **子对象属性变化会向上通知父对象**：父对象上持有该子对象的属性会被标脏并发出 `PropertyChanged`，
> 变更沿父链逐级上浮；`ObservableList<T>` 的元素变化与集合增删、`ObservableDictionary<TKey,TValue>` 的项变化也会通知父对象。
>
> 直接 `new` 出来的对象**不会**被注入依赖，必须手工设置 `BusinessContext`，否则 `AddRules()` 永不执行、`IsValid` 恒为 `true`；
> 未接线的对象直接 `SaveAsync` 会抛 `InvalidOperationException`（消息含类型名与操作名），而不是放任空引用往下走。

### 2.2 只回写改过的属性

变更追踪是「最小写入」的依据：**未修改的属性既不校验、也不应回写数据库**。
`UpdateAsync` 里按 `ChangedProperties`（或 `FieldManager` 的字段级 `IsChanged`）拼更新语句：

```csharp
[FactoryUpdate]
protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
{
    // 只更新改过的列：未修改的属性保持数据库中的原值，
    // 也让「只装载了部分字段」的对象不会被整体回写
    var changed = ChangedProperties;

    // 例：EF Core 里只标记改过的属性（属性名 → 列名按你的映射换算）
    foreach (var property in changed)
    {
        context.Entry(entity).Property(property.Name).IsModified = true;
    }

    await context.SaveChangesAsync(cancellationToken);
}
```

> 赋**同一个值**不算变更：`SetProperty` 会直接返回，既不标脏也不触发规则检查——
> 与「未修改就不回写」保持一致。
>
> `LoadProperty` 是装载（不标脏、不触发检查），`SetProperty` 是用户修改（标脏、触发检查）。
> 装载一条历史数据不会报出校验错误，也不会把对象变成待保存状态。

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
// 注意：criteria 写入口无法在写入前判定行级权限；受数据权限约束的类型请用
// Fetch → IObjectScopeAuthorizer.Allows → Save，或在工厂方法内先判定。
await factory.UpdateAsync<Repo>(repoId);
await factory.DeleteAsync<Repo>(repoId);     // 返回 Task，无结果
// factory.InsertAsync<Repo>(repoId) 已标记 [Obsolete]：改用 CreateAsync + SaveAsync

// 服务对象
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
- **漏写 `[FactoryInsert]/[FactoryUpdate]/[FactoryDelete]/[FactoryExecute]` 会在保存/执行时抛
  `NotSupportedException`**，不再调用基类空实现“静默成功”；兼容期可用
  `AddBusinessObject(o => o.StrictFactoryMethods = false, assemblies)` 临时回退。
- 工厂方法可以返回 `Task`、`Task<T>`、`ValueTask`、`ValueTask<T>`，都会被等待。

---

## 4. 规则体系

规则分两类：**对象级**（`Property == null`）与**属性级**（绑定到某个已注册属性）；
按作用域又分**类型级**（`AddRule`，容器级共享；无 DI 时回退到进程级）与**实例级 / 操作级**（`AddInstanceRule`、
执行器的 `WithRule`，随对象或操作消失）。四个组合如下表。

| | 类型级 `AddRule` | 实例级 `AddInstanceRule` / 操作级 `WithRule` |
|---|---|---|
| **对象级**（无属性绑定） | 最常用；在 `AddRules()` 里注册 | 按操作附加的对象级校验 |
| **属性级**（绑定属性） | 在 `AddRules()` 里注册 | 按操作附加的字段级校验，结果按属性归因 |

**两类的触发时机不同，这是设计要点**：

| | 何时执行 | 为什么 |
|---|---|---|
| 对象级 | 保存 / 命令执行时（由工厂边界裁决） | 它看的是「整个对象合不合格」，只在真正要落库时判定一次 |
| 属性级 | **属性变更时**（每次 `SetProperty`） | 它看的是「这个字段值合不合格」，**未修改的属性不校验**——否则「只装载了部分字段」的对象会因为未装载字段读出默认值而被凭空拦下 |

属性级规则的违规会按属性归因落进 `BrokenRules`，因此保存时仍会经 `IsValid` 生效并抛
`ValidationException`（见 §4.1）——**默认模式下不需要**在保存时再重复跑一遍属性规则。

把触发点关掉时（`CheckRuleOnPropertyChanged => false`）**不是不检查，而是推迟**：变更时先挂起，
到保存/命令执行时补跑，顺序是**先属性级（只跑变更过的属性），再对象级**。顺序不能颠倒——
对象级那一遍一旦发现 Error 就抛异常，排在它之后的属性级检查就没机会执行，字段级错误会被整批丢掉：

```csharp
protected override bool CheckRuleOnPropertyChanged => false;   // 变更时挂起，改到保存时统一检查

// 保存时的实际顺序（EditableObject.SaveAsync → ObjectBase.EnsureRulesAsync）：
//   1. 属性级：只跑 ChangedProperties 里那些属性 —— 它们才是本次要落库的字段
//   2. 对象级：整体一致性 / 跨字段校验（它可能依赖一个或多个属性值）
//
// 走的是异步路径，因此推迟模式下的属性级规则可以放心 await（I/O 校验），
// 不像 setter 上那样必须同步完成。
```

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

    // ⑤ 对象级：无参构造 ⇒ Property == null
    Rules.AddRule<RepoConsistencyRule>();
}
```

> **`AddRules()` 只对每个类型执行一次**（由 `RuleManager` 的 `Initialized` 标志守卫）。
> 因此**类型级注册只应写在 `AddRules()` 里**；在业务方法或属性 setter 里调 `AddRule`
> 会注册进进程级共享集合，泄漏到同类型的**所有**实例、乃至同进程的所有容器与请求。
> 运行期要加规则请用 `AddInstanceRule` 或执行器的 `WithRule`。

自定义属性级规则：

```csharp
public sealed class RepoNameCheckRule(IPropertyInfo property) : PropertyRuleBase(property)
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

> 本节只讲**验证规则**。权限由工厂边界裁决，不在此表内，失败一律 `SecurityException`（见 §6.5）。

| 路径 | 是否执行**对象级**规则 | 失败形态 |
|---|---|---|
| `EditableObject.SaveAsync`（直接调用） | ✅ | `ValidationException` |
| `UpdateActuator` / `CreateActuator` | ✅（经 `SaveAsync`） | `ValidationException` |
| `DeleteActuator` | 默认❌；`MarkAsDeleted(true)` / 执行器 `WithRuleChecksOnDelete()` 才执行 | `ValidationException`（**验证**失败；越权由权限线抛 `SecurityException`，与规则无关） |
| `ExecuteActuator`（服务对象） | ✅（工厂边界在**执行体之前**裁决） | `ValidationException` |
| `IObjectFactory.*Async(criteria)` 低层入口 | ❌ | —（权限仍然强制） |
| 任意 `SetProperty`（属性变更） | 执行该**属性级**规则（该类型把检查推迟时为挂起） | 违规落进 `BrokenRules`，`IsValid` 转 `false` |
| `CheckRuleOnPropertyChanged => false` 的类型 | 保存/命令执行时补跑：**先属性级（仅变更过的属性）→ 再对象级** | `ValidationException`，错误列表「先字段、后整体」 |

```csharp
repo.ValidationState;                           // NotValidated / Valid / Invalid（补上三态语义）
repo.IsValid;                                   // ErrorCount == 0（首次校验前恒为 true）
repo.GetBrokenRules();                          // BrokenRuleCollection（Error/Warning/Information）

await repo.SaveAsync();                         // 内部先跑对象级规则；有 Error → 抛 ValidationException

await repo.ValidateAsync();                     // 显式检查，返回 bool，不改状态、不抛异常
await repo.EnsureValidAsync();                  // 显式检查，有 Error → 抛 ValidationException

repo.ValidationComplete += (_, _) => { };       // 规则跑完的事件

repo.SuspendRuleChecking();                     // 挂起：本次不给出结论（不再读上一次的结论）
repo.ResumeRuleChecking();
```

属性级规则在 setter 上就会跑。关心性能、或规则需要 I/O 的类型可以关掉这个触发点——
不过注意那是**推迟到保存时**，不是「不检查」：

```csharp
protected override bool CheckRuleOnPropertyChanged => false;   // 变更时挂起，保存时统一补跑
```

> **`IsValid` 在首次检查之前恒为 `true`**——它取自违规集合，而集合只有在某次检查跑过之后才有内容。
> 因此不要用 `IsSavable` 当作「能保存」的判据，需要结论就先 `ValidateAsync()`（或直接保存）。
>
> **挂起规则检查 ≠ 对象有效**：挂起期间既不跑规则、也不读陈旧的违规集合，
> `SaveAsync` 因此不会按上一次检查的结论放行或拦截。
>
> **属性级规则是同步阻塞的**：它在属性 setter 上跑，会阻塞调用线程（`Rules` 在有同步上下文时
> 会把整轮检查挪到线程池，避免与需要回到该上下文的续体互等死锁）。
> 因此属性级规则应当是**同步即可完成的纯校验**；需要 I/O 的校验请写成对象级规则——
> 保存走的是异步路径。
>
> 只有该属性**存在规则**时才进入规则检查；没有规则的属性走普通变更通知，没有额外开销。

只让规则在特定状态下生效：

```csharp
[ExecuteOnState(ObjectEditState.New)]           // 标在规则类上，只对 IEditableObject 目标生效
public sealed class RepoCreateOnlyRule : ObjectRuleBase { /* ... */ }
```

> **只有 `Error` 会让对象无效**（`IsValid => BrokenRules.ErrorCount == 0`）；Warning / Information 不阻断保存。
>
> **报错必须给消息**：`AddErrorResult(null)` / `AddErrorResult("")` / `AddErrorResult(" ")` 会用占位消息
> （`Rule message is required`）代替，不会被静默当成通过。
>
> **规则只在 `BusinessContext` 被赋值后才初始化**：setter 会触发 `AddRules()` 与 `AddDataAnnotations()`。
> 直接 `new` 出来的对象若从不接线，`AddRules()` 永不执行。
>
> `ObjectBase.Rules` 是 **`protected`**。要从外部驱动规则用 `ValidateAsync()` / `EnsureValidAsync()`；
> 要在派生类里暴露规则集合（例如运行期加实例级规则）：
> `public Rules PublicRules => Rules;`

### 4.2 按操作指定规则

执行器让「只对这一次操作生效」的规则不必写进 `AddRules()`：

```csharp
await actuator.For<Repo>()
             .Update(id)
             .Handle(repo => repo.Name = "pay-web-2")
             .WithRule(new RepoNameUniqueRule(Repo.NameProperty))   // 实例：按操作构造
             .WithRule<RepoConsistencyRule>()                        // 无参构造
             .WithRule<RepoAuditRule>(serviceProvider)               // 从容器解析依赖
             .WithRules(ruleEnumerable)                              // 批量
             .BypassRule<SlowRule>()                                 // 本次绕过这条类型级规则
             .BypassRule("rule://nerosoft.../repo/name")             // 按 Name 绕过
             .BypassRuleChecks()                                    // 本次完全跳过规则检查
             .ExecuteAsync(cancellationToken);
```

语义要点：

- **追加的规则不写入类型级共享集合**，只挂在本次操作取到的对象上；并发请求互不可见，操作结束即消失。
  动态加规则请一律走这条通道，不要在业务方法里 `AddRule`（那会泄漏到同进程的所有实例）。
- **规则在 `Handle` 之后执行**：它看到的是处理完的对象，不在 `Handle` 里设值就看不到变更。
- **绑定属性的附加规则也一定执行**：附加规则进的是**对象级**检查（保存 / 命令执行那一遍），
  无论它有没有绑定属性——所以附加在某个属性上的规则不会因为「那个属性这次没改」而被跳过，
  语义就是「本次操作必须满足它」。失败时按该属性归因（`BrokenRule.Property`）。
- `BypassRule<T>()` 按**精确类型**匹配，不含派生类型——避免 `BypassRule<RuleBase>()`
  一次笔误就关掉所有数据校验规则。
- **规则属于验证线，权限不属于规则**：`BypassRuleChecks()`、`.BypassRule<T>()`、
  `SuspendRuleChecking()` 只跳过**校验**，不解除**权限**：越权仍由工厂边界抛 `SecurityException`，
  没有开关能关掉它。规则集合里也不应再出现权限类规则。
- **删除默认不跑规则**。`.Delete(id).WithRule(...)` 要让附加规则真正执行，须同时 `.WithRuleChecksOnDelete()`。
- 若 `Handle` 什么也没改、对象又是干净的，`SaveAsync` 会直接返回（无事可保存），规则那一轮不会发生。

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

服务对象——**它需要同时提供 `[FactoryCreate]` 与 `[FactoryExecute]`**：

```csharp
// 服务对象不是可编辑实体：属性只承载操作入参/出参，不参与变更追踪与保存。
// 它继承 ObjectBase<T>（属性 + 规则），不继承 BusinessObject<T>（变更追踪），
// 因此没有脏状态、ChangedProperties 恒为空、也没有 SaveAsync。
// 下面的属性是操作入参/出参，不是被校验的字段——服务对象的校验请写成对象级规则。
public sealed class PushService : ServiceObject<PushService>
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

    [FactoryExecute]    // 执行体
    protected override Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Pushed = true;
        return Task.CompletedTask;
    }
}

// 用法
await actuator.For<PushService>()
              .Execute(repoId, CancellationToken.None)
              .Handle(command => command.Note = "first")
              .ExecuteAsync(CancellationToken.None);
```

> **两处 `ct` 的区别**：`.Execute(ct)` 的参数进 `criteria`（用于**定位**工厂方法），
> `.ExecuteAsync(ct)` 的才是取消令牌。两处都要传。
>
> 执行器提供 `Fetch`（只读取、不保存）；需要更底层的读取时也可直接用 `IObjectFactory.FetchAsync` 或 `BusinessContext.FetchAsync`。
>
> 执行器还能按操作指定规则（`WithRule` / `BypassRule` / `BypassRuleChecks`），
> 见 [§4.2](#42-按操作指定规则)。

> **`ServiceObject` 的定位**：它封装一次**独立业务操作**——可以协调多个业务对象、访问数据、
> 执行不属于单个实体生命周期的逻辑。因此它不继承 `BusinessObject<T>` 的变更追踪，
> 也不参与 `SaveAsync`；它是“操作对象”，不是可编辑实体。若操作需要跨聚合/直接访问数据，
> 优先把依赖通过构造函数注入，并让应用层负责事务边界。

自定义管道行为：

```csharp
public sealed class AuditBehavior<TTarget> : IActuatorBehavior<TTarget>
    where TTarget : ObjectBase<TTarget>
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

## 6. 权限示例

完整示例工程：[docs/examples/osba-console](../../docs/examples/osba-console/README.md)。

```bash
dotnet run --project docs/examples/osba-console
```

预期输出：

```text
granted(order:read+order:update) => saved=A-1
missing(order:update) => SecurityException
missing(order:read) => SecurityException
```

关键代码：

```csharp
[Permission("order:read")]                    // 类型级：对该类型的所有方法生效
public sealed class Order : EditableObject<Order>
{
    public string Name { get; set; } = string.Empty;

    [Permission("order:update")]               // 方法级：只在 UpdateAsync 被工厂调用时生效
    [FactoryUpdate]
    protected override Task UpdateAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
```

```csharp
services.AddBusinessObject(typeof(Order).Assembly);
services.AddPermission(p => p.Scan(typeof(Order).Assembly));   // 可选策略引擎
```

```csharp
var order = new Order { Name = "A-1", BusinessContext = businessContext };
order.MarkAsChanged();
var saved = await factory.SaveAsync(order, CancellationToken.None);
```

- 第一条输出：类型级 `order:read` 与方法级 `order:update` 都满足，更新成功。
- 第二条输出：缺少方法级 `order:update`，工厂边界抛出 `SecurityException`。
- 第三条输出：缺少类型级 `order:read`，更新路径同样被拒绝。

**宿主装配**：需要策略引擎时，可选装配 `Euonia.Security` 并调用
`AddPermission(p => p.Scan(asm))`；完整装配见示例工程。

## 7. 易错点清单

### 装配

1. `AddBusinessObject(...)` 返回 `IServiceCollection`，可链式调用；它用 `TryAdd*`，已注册的服务不会被覆盖。
   启动期校验默认关闭，生产宿主应显式 `AddBusinessObject(o => o.ValidateOnStartup = true, assemblies)`。
2. 它**不注册** `IObjectActivator` 与 `ILazyServiceProvider`。要用 `IHasLazyServiceProvider`，
   需先有 `ILazyServiceProvider`（由 `Euonia.Modularity` 的 `AddModularityApplication` 提供）。
3. 手动作用域：**先** `BusinessContextAccessor.SetCurrent(...)`，**再**解析 `BusinessContext`/工厂；用完 `Clear()`。

### 对象与属性

4. `RegisterProperty<TValue>` 全是 `protected static`；表达式重载**必须显式写 `TValue`**。
5. **必须使用 `RegisterProperty` 的返回值**，不要自己 `new PropertyInfo<T>(...)`。
6. `string` 属性的默认值是 `string.Empty`，不是 `null`。
7. 加载数据用 `LoadProperty`（不标脏），用户改动用 `SetProperty`（标脏）——不要混用。
8. `MarkAsNew` / `MarkAsChanged` / `MarkAsDeleted` 都是 **virtual**，统一经过 `TransitionTo`；
   派生类可重写 `CanTransitionTo` / `TransitionTo` / `OnStateChanged`，并订阅 `StateChanged`。
9. `MarkAsDeleted(true)` 才让删除也执行对象级规则，默认**不执行**；走执行器时用 `WithRuleChecksOnDelete()`。
10. `ObjectBase.Rules` 是 **`protected`**；`Rules.RuleManager` / `Rules.BrokenRules` 是 `internal`。
    外部可读 `IsValid` / `GetBrokenRules()`，并用 `ValidateAsync()` / `EnsureValidAsync()` 驱动检查；
    要直接操作规则集合（例如加实例级规则）请在派生类里暴露它。
11. `CheckRuleOnPropertyChanged` 默认 **`true`**：属性变更即触发该属性的**属性级**规则检查。
    它是 `virtual`；覆写为 `false` 表示**推迟到保存时补跑**（不是不检查），补跑顺序是
    **先属性级（仅变更过的属性）再对象级**——对象级那一遍会抛异常，顺序颠倒会让字段级错误整批丢失。
    默认模式下属性级规则必须能同步完成（setter 上阻塞调用线程）；推迟模式下走异步路径，可以 await。
12. **赋同一个值不算变更**：`SetProperty` 直接返回，不标脏、不触发规则检查、不发出变更通知。
    所以「把值设成非法」只有在它确实与当前值不同时才会报错。
13. **未修改的属性不校验、也不应回写数据库**：默认模式下属性级规则在变更时已经跑过，
    其结果经 `BrokenRules` / `IsValid` 在保存时生效；推迟模式（第 11 条）下保存时只补跑变更过的属性。
    两种模式都不会去校验本次没改的属性——否则只装载部分字段的对象会被 `[Required]` 凭空拦下。
    持久化只取 `ChangedProperties`。
14. **服务对象不是可编辑实体**：它只表达「要执行某种操作」，属性只承载入参/出参；输入也可以由工厂方法的参数
    （执行器 `criteria`）带入，而不是先写进对象属性再校验。因此它不参与变更追踪
    （`SetProperty` 在 `ObservableObject<T>` 上，`ServiceObject<T>` 继承 `ObjectBase<T>`，
    不进入 `BusinessObject<T>` 的变更追踪层级），`ChangedProperties` 恒为空，
    **属性级规则对它也无从触发**。服务对象的校验请写成**对象级**规则。
15. 跨程序集重写工厂方法时用 **`protected override`**（不是 `protected internal override`）。

### 工厂

16. `CreateAsync`/`InsertAsync`/`UpdateAsync`/`DeleteAsync` 是 **`protected internal virtual`**。
17. `SaveAsync` 对外只有一个重载：`Task<T> SaveAsync(bool forceUpdate = false, CancellationToken = default)`；
    带 `userState` 的那个是 `protected`。
18. 工厂方法约定名**大小写敏感**，拼错即 `MissingMethodException`；加 `[FactoryXxx]` 则不限名字。
19. 目标方法多出的尾部参数**必须有默认值**；精确长度匹配优先。
20. `IObjectFactory.Create<T>` 会自动 `MarkAsNew()`——不要重复调用。
21. 同步的 `Create`/`Fetch` 在没有同步上下文时直接等待；只有在 UI 等同步上下文里才用
    `AsyncContext.Run` 避免死锁。服务端应优先使用 `*Async` 入口。
22. **只有经工厂创建的对象才会被注入**（`[Inject]` 属性、`BusinessContext`）；`new` 出来的不会。
23. **服务对象走执行器时要同时提供 `[FactoryCreate]` 与 `[FactoryExecute]`**——
    执行器先 `CreateAsync` 造实例，再在终结阶段调 `ExecuteAsync`。

### 规则

24. `RulesExtensions.AddRule<T>(IPropertyInfo, ...)` 的 handler 收到的是**业务对象本身**，不是属性值；
    必须显式写 `<T>`。
25. `CommonRule.Lambda<T>` 的参数类型是 **`PropertyInfo<T>`**（不是 `IPropertyInfo`），且 handler 是**同步**的。
26. `CommonRule.Regular` **只支持 `string` 值**，其他类型抛 `NotSupportedException`。
27. `ExecuteOnStateAttribute` 标在**规则类**上，且只对 `IEditableObject` 目标生效。
    写了特性却**不填状态**视为「不限制」，不是「全都不匹配」——后者会把规则静默关掉。
28. `AddRule<TRule>()` 要求规则类有公共无参构造；需要依赖时用 `AddRule<TRule>(IServiceProvider)`。
29. 规则实例是**类型级共享的单例**——绝不能把请求态存进规则字段，一律从 `context.Target` 取。
30. 只有 `Error` 让对象无效；Warning/Information 不阻断保存。
31. **规则只在 `BusinessContext` 被赋值后才初始化**；不设上下文则 `AddRules()` 永不执行、`IsValid` 恒 `true`。
32. `IsValid` 在**首次检查之前恒为 `true`**（违规集合还是空的）。要结论就 `ValidateAsync()`，别拿 `IsSavable` 当判据。
33. **挂起规则检查 ≠ 对象有效**：挂起期间不跑规则、也不看陈旧的违规集合，`SaveAsync` 不会按上一次的结论放行或拦截。
34. `AddRule` 写进**进程级、按类型共享**的集合；`AddRules()` 每个类型只跑一次。运行期加规则请用
    `AddInstanceRule` 或执行器的 `WithRule`，否则会泄漏到同进程的所有实例与容器。
35. `AddErrorResult` 收到 `null`/空白描述时会用占位消息补上，**不会**被静默当成通过；
    但反过来，报错却不给消息本身就是缺陷，别依赖这个兜底。
36. **删除默认不跑对象级规则**，`.Delete(id).WithRule(...)` 需同时 `.WithRuleChecksOnDelete()` 才会执行。
37. `BypassRule<T>()` 按**精确类型**匹配；`BypassRuleChecks()` 只跳规则、**不**解除权限。
    绕过会沿父子链向下穿透到子对象（含 `ObservableList` 元素）；子对象自身的绕过不会反向影响父对象。
38. **服务对象也会跑对象级规则**，且由工厂边界在执行体之前裁决——规则失败时执行体不会执行。
    但 `IObjectFactory` 的 criteria 低层入口（`ExecuteAsync(criteria)` 等）不做规则判定。
39. `ValidateAsync(cascade, ct)` 只管**对象级**规则且不抛异常（返回 bool）；
    要抛异常用 `EnsureValidAsync()`。二者都不修改对象状态。

### 执行器

40. `Actuator` 类是 `internal`；对外只能注入 `IActuator` 后 `For<T>()`。
41. `ActuatorBuilderExtensions` 提供 `Fetch`；它只读取，不触发保存。
42. `.Execute(ct)` 的参数进 `criteria`，`.ExecuteAsync(ct)` 的才是取消令牌——两处都要传。
43. `IActuatorBehavior<T>` 需要注册到容器才会被自动接入管道。
44. `WithRule` 附加的规则**在 `Handle` 之后执行**：不在 `Handle` 里设值，规则看不到变更。
45. `Handle` 什么都没改、对象又干净时 `SaveAsync` 直接返回，那一轮规则不会发生。

### 权限

46. 权限码写在 `[Permission]` 上：类型级对该类型的所有方法生效，方法级只在对应方法被工厂调用时生效。
47. `CanPerformOperation` 只反映类型级与已登记来源，不含方法级声明；方法级判断用
    `IPermissionChecker.IsGranted`，真正的拦截在工厂边界。
48. 越权一律抛 `SecurityException`，不会出现在 `ValidationException.Errors` 里。
49. 有要求却缺 `BusinessContext` 或判定实现时抛 `InvalidOperationException`，不会静默放行。
50. 数据权限由 `IObjectScopeAuthorizer` 实现负责；形态与时机见 [PERMISSION.md](PERMISSION.md)。
