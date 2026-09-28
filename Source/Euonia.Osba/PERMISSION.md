# Euonia.Osba 权限控制使用说明

`Euonia.Osba` 自己**不认识任何鉴权实现**：它只定义权限契约，并在**工厂边界**强制判定。
接策略引擎是另一件事——由 `Euonia.Osba.Security` 把引擎接到这些契约上；宿主也可以用自己的实现
（读配置、查权限表、接已有鉴权框架）。本文档讲的就是这套契约与接线该怎么用。

> 引擎自身的类型、语义与设计取舍见 [`Euonia.Security/README.md`](../Euonia.Security/README.md)
> 与 [`DESIGN.md`](../Euonia.Security/DESIGN.md)；多场景示例见
> [`PERMISSION-SAMPLE.md`](PERMISSION-SAMPLE.md)；引擎与宿主之间的接线决策见
> [`PERMISSION-DESIGN.md`](PERMISSION-DESIGN.md)。

## 0. 库边界与依赖方向

| 库 | 内容 | 依赖 |
|---|---|---|
| `Euonia.Core` | 权限的**基础词汇**：`[Permission]`、`BusinessOperation`、`UserPrincipal`、`UserClaimTypes` | — |
| `Euonia.Osba` | `BusinessObject` / 工厂 / 上下文，权限**契约**（`IPermissionRequirementProvider`、`IOperationPermissionChecker`、`IObjectScopeAuthorizer`）与**强制执行点**（`ObjectAuthorization`、`ScopeAuthorization`） | `Euonia.Core`（**不引用引擎**） |
| `Euonia.Security` | 策略引擎：`ScopeModel<T>`、策略编译与下推、`IScopeGuard`、`ScopeKeyResolver` | `Euonia.Core` |
| `Euonia.Osba.Security` | **适配包**：把引擎接到 Osba 的权限契约上，提供 `AddObjectPermission` | `Euonia.Osba` + `Euonia.Security` |

**两条边都是单向的**：`Euonia.Osba.Security → (Euonia.Osba, Euonia.Security)`，而 `Euonia.Osba` 与
`Euonia.Security` 之间**没有边**。做成这样是因为两边的知识各自有主：

- 「资源当前代表哪个业务操作」（可编辑对象的新增/更改/删除状态、命令对象、只读对象）是**对象模型**的知识；
- 「这些要求是否被满足」是**鉴权实现**的知识。

因此 Osba 把前者收在自己的契约里（要求来源 + 两个判定入口），后者留给宿主回答：

| 契约 | Osba 自带 | 引擎适配包提供 | 宿主自己实现 |
|---|---|---|---|
| `IPermissionRequirementProvider`（要求从哪来） | ✅ 工厂约定扫描（`ObjectPermissionRequirementProvider`） | 桥接到引擎的权限码来源（含宿主用 `AddPermission` 追加的规则） | 例如规则来自配置或权限表 |
| `IOperationPermissionChecker`（操作权限判定） | — | ✅ `SubjectPermissionChecker` | 例如按权限码集合判定 |
| `IObjectScopeAuthorizer`（行级数据权限） | — | ✅ `IScopeGuard` + 行级模型 | 例如按租户/部门比较对象属性 |

**不装任何实现也能用**：声明了 `[Permission]` 的类型在工厂边界会因「无人判定」而**报错**，
而不是静默放行——这是刻意的（fail-closed）。

接引擎时 `AddObjectPermission` 会注册上表第二列的三个实现，因此**不需要**手工注册它们；
它同时注册引擎的两个映射（`IPermissionCodeSource`、`IScopeKeyResolver`）。
不调用它、也不注册自己的实现，就等于不启用操作权限与行内数据权限。

命名空间约定：`PermissionAttribute`、`BusinessOperation` 位于 `Euonia.Core` **程序集**但沿用命名空间
`Nerosoft.Euonia.Security`（与 `UserPrincipal` 同类）——不引入引擎的宿主也能用它们，
因此业务对象文件里出现 `using Nerosoft.Euonia.Security;` 是正常的。

| | 操作权限（Operation Permission） | 数据权限（Data Permission） |
|---|---|---|
| 回答的问题 | 当前用户**能否执行某项操作** | 当前用户**能看到/操作哪些数据行** |
| 作用对象 | 操作 × 对象类型（类级/方法级） | 声明了 `ScopeModel<T>` 的资源类型 |
| 判定依据 | 权限声明（或角色），一般来自用户声明 | 资源属性 × 用户从**授权数据**实时解析出的主体集合 |
| 强制执行点 | `BusinessObjectFactory` 调用边界（`Euonia.Osba`） | `IScopeGuard.Apply(IQueryable)` + 工厂保存边界（`Euonia.Osba`） |
| 失败形态 | 抛 `System.Security.SecurityException` | 查询排除该行 / 保存抛 `SecurityException` |

> 设计动因、被否决的方案与已知边界见 [`Euonia.Security/DESIGN.md`](../Euonia.Security/DESIGN.md)（面向维护者）。

> **核心原则：能预定义的进代码，不能预定义的走数据。**
> 操作类型、维度、判定语义可以预定义；而"用户属于哪些团队、能访问哪些仓库"这类授权值
> 随时可变（团队/资源会新建删除、人员会调整），**必须从应用数据实时解析，绝不能固化
> 在声明/Token/代码字面量里**。这决定了操作权限用声明，数据权限用数据源。

---

## 1. 准备工作

```csharp
var services = new ServiceCollection();

// 1) 注册 Osba 基础设施
services.AddBusinessObject(typeof(Order).Assembly);

// 2) 启用权限（需要时才加）
services.AddObjectPermission(typeof(Order).Assembly);
```

**这两步是分开的，是否启用由应用决定。**
`AddBusinessObject` 只注册对象工厂需要的东西：

- `BusinessContext` / `BusinessContextAccessor` / `IActuator`
- `IObjectFactory` → `BusinessObjectFactory`

它**不碰权限**——连 Osba 自己的三个权限契约也不注册。只用 Osba 做对象工厂、不做权限的应用
（纯查询、内部工具）不会被迫承担任何权限装配；反过来，宿主的其他模块想启用自己的权限体系，
也不必顺带把 Osba 的权限拉进来。（声明了 `[Permission]` 的类型仍需有人判定，否则工厂边界会报错——
见 §0 的说明。）

`AddObjectPermission`（来自 **`Euonia.Osba.Security`** 适配包）才启用引擎鉴权，它做两件事：

- 注册 Osba 三个权限契约的**引擎实现**：`IPermissionRequirementProvider`（桥接到引擎的权限码来源）、
  `IOperationPermissionChecker`、`IObjectScopeAuthorizer`，以及引擎映射
  `TryAddSingleton<IScopeKeyResolver, ObjectScopeKeyResolver>`——Osba 对「对象当前代表哪个操作」的回答
- `AddPermission(<Osba 的工厂约定来源>, assemblies)`——把「哪个工厂方法对应哪个操作」交给引擎
  （宿主因此**不需要**自己声明操作入口规则；要补充规则用 `AddPermission` 追加，见 §0 末）

引擎侧随之注册 `IPermissionChecker` → `SubjectPermissionChecker`（权限码来自授权数据，撤销立即生效）、
`ScopeModelRegistry`（数据权限模型注册表，注册期即完成校验）、
`IScopeGuard` → `ScopeGuard`（数据权限判定入口，按请求缓存）、`PermissionSetup`。

三个契约都是 `TryAdd` 语义：宿主可以先注册自己的实现，适配包不会覆盖它——
这样「接引擎」与「用自己的实现」可以是同一个装配路径，甚至可以交替使用（例如行级用引擎、操作权限用自建表）。

两点使用说明：

- **调用顺序与调用次数都不受限制。**`AddObjectPermission` 可以在 `AddBusinessObject`
  之前或之后调用；多个模块可以各自调用，权限码与模型按**并集**合并。
- **`IScopeKeyResolver` 是 `TryAdd` 语义**（先到先得），因为「某个资源实例当前代表哪个操作」
  是**全局**答案，多个模块给出不同答案本身就是配置错误。需要自定义时自己注册即可，会覆盖框架推断。

不用 Osba 的工厂约定时，另一种装配是：只 `AddBusinessObject`，然后注册自己的
`IPermissionRequirementProvider` / `IOperationPermissionChecker` / `IObjectScopeAuthorizer`
（`Euonia.Osba.Standalone.Tests` 项目就是这种用法的可运行示例）。此时不需要引用任何引擎包。

Osba 宿主用 `AddPermission` 补充的规则在**运行期同样生效**：额外注册的规则与 Osba 自己的工厂约定
取并集，且注册期校验、操作权限闸门、策略键解析问的是**同一个来源**
（见 [`DESIGN.md` §1.11](../Euonia.Security/DESIGN.md)）。

若使用权限（操作权限的权限码或数据权限），还必须**由应用注册一个 `IScopeSubjectResolver`**
（见 [3.2](#32-用户侧授权值从数据实时解析)），框架不提供默认实现，以免把授权值固化。

容器构建后请调用一次启动期校验，使「声明了权限却忘了接解析器」在启动时失败：

```csharp
var provider = services.BuildServiceProvider();
provider.ValidatePermissionSetup();   // 缺少 IScopeSubjectResolver 或 UserPrincipal 时在此抛出
```

使用时机说明：`BusinessContext` 在构造时会捕获当前用户
（来自 `BusinessContextAccessor.Current`，AsyncLocal）。请确保查询/请求开始时先设置：
`BusinessContextAccessor.SetCurrent(scope.ServiceProvider)`（依赖注入下由中间件/工厂生周期自动完成）。

---

## 2. 操作权限

### 2.1 声明权限点

通过 `[Permission]` 声明，可打在**类型**（适用于全部操作）或**工厂方法**上
（仅对应操作生效），两者取并集；可用多次（多权限 = AND，多角色 = OR）。

```csharp
// 方法级：仅插入要求 order:create
public class Order : EditableObject<Order>
{
    [FactoryInsert]
    [Permission("order:create")]
    protected override async Task InsertAsync(CancellationToken cancellationToken = default)
    {
        // ...
    }

    [FactoryUpdate]
    [Permission("order:update")]
    protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        // ...
    }
}

// 类级：全部操作都要求 admin
[Permission("admin")]
public class AdminSettings : EditableObject<AdminSettings>
{
    // ...
}
```

方法级要求的收集范围与工厂方法的查找范围**完全一致**（同一套判定规则），无论用哪种方式声明
工厂方法，方法上的 `[Permission]` 都会生效：

- 标记了 `[FactoryXxx]` 的方法：**不限定方法名**
- 未标记的方法：**严格按约定名称匹配**（大小写敏感），即
  `Update`、`UpdateAsync`、`FactoryUpdate`、`FactoryUpdateAsync` 之一

因此，若方法名不属于上述任何一种写法，它既不会被工厂调用，其上的 `[Permission]` 也不会被收集。

工厂方法特性与业务操作的对应关系：

| 操作 | 工厂方法特性 | 虚方法 |
|---|---|---|
| 读取 | `[FactoryFetch]` | `CanReadObject()` |
| 创建（Insert 态保存） | `[FactoryCreate]`、`[FactoryInsert]` | `CanCreateObject()` |
| 更新 | `[FactoryUpdate]` | `CanUpdateObject()` |
| 删除 | `[FactoryDelete]` | `CanDeleteObject()` |
| 命令执行 | `[FactoryExecute]` | `CanExecuteObject()` |

### 2.2 用户如何被授予

默认检查器 `SubjectPermissionChecker` 从**授权数据**读取权限码（经 `IScopeSubjectResolver` 实时解析、
按请求缓存），**不再从令牌声明读取**：

```csharp
public sealed class MySubjectResolver : IScopeSubjectResolver
{
    public async ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;

        return ScopeSubjectSet.CreateBuilder()
                              .AddCodes(await _roles.GetPermissionsAsync(userId, ct))   // ["order:create", ...]
                              .AddSelf(userId)
                              .Build();
    }
}
```

**为什么不放令牌里**：

- 权限码数量可能很大，放进令牌会**撑爆 Token**；
- 更关键的是**取消授权必须立即生效**。令牌在过期前一直有效，把权限固化在里面意味着
  **撤销后旧令牌仍可通行**。改为数据来源后，撤销只需改数据，下一次解析（通常是下一个请求）即生效，
  **不需要重新签发令牌**。

支持以 `*` 结尾的前缀通配（持有 `order:*` 可通过 `order:create`），大小写不敏感。

**角色仍来自声明**（`UserPrincipal.IsInRole`）：角色数量少而稳定，不构成令牌膨胀问题。
**细粒度授权一律使用权限码**，不要用角色承载。

> `ClaimPermissionChecker`（读 `"perm"` 声明）仍保留但已标记 `[Obsolete]` 且不再是默认实现，
> 仅为显式回退存在。

### 2.3 强制执行

`BusinessObjectFactory` 的所有入口（`Create`、`Fetch`、`InsertAsync`、`UpdateAsync`、
`DeleteAsync`、`SaveAsync`、`ExecuteAsync` 等）在调用业务方法前统一执行
`ObjectAuthorization.EnsureAuthorized`：不满足要求即抛 `SecurityException`。
`SaveAsync` 会根据对象状态映射操作（New→`Create`、Changed→`Update`、Deleted→`Delete`，
命令对象→`Execute`）。行为约定：

- 没有任何 `[Permission]` → 放行（也不会强制要求对象接线）
- **有要求却无法判定** → 抛 `InvalidOperationException`：目标未接入 `BusinessContext`，
  或容器里没注册 `IPermissionChecker`。**「判定不了」不等于「没有要求」**，静默放行会让
  「忘记给对象接上下文」变成一条无声的越权通道。
- 有要求且判定得出，但未认证/未授权 → 拒绝（抛 `SecurityException`）

数据权限同理由工厂边界的 `ScopeAuthorization` 裁决（见 [2.3](#23-强制执行)）：它负责
「这一行在不在范围内」，同样抛 `SecurityException`，且**不受任何规则绕过开关影响**。
越权不再经由规则通道表达——验证线只做数据校验（见 [4.5](#45-与-rule-体系的边界)）。

### 2.4 自定义授权逻辑

重写 `BusinessObject` 的对象级虚方法，实现更细的控制；基类默认委托给类型/方法上的要求。

```csharp
public class Order : EditableObject<Order>
{
    public override bool CanDeleteObject()
    {
        // 结合业务判断
        return HasPermission("order:delete") && !_isArchived;
    }
}
```

对象内部还可使用契约保护的辅助方法（在属性访问器或业务方法中）：

- `protected bool HasPermission(string permission)`
- `protected bool HasRole(string role)`

---

## 3. 数据权限

### 3.1 核心不变式：单一真值来源

数据权限的判定**只有一处实现**。策略被编译成一对表达式：

```
Allows(resource) ≡ Allow(resource) && !Deny(resource)
query            ≡ source.Where(Allow).Where(!Deny)
```

用户被授予的主体集合在编译时**烘进表达式**（例如 `deptIds.Contains(x.DeptId)`），
因此「查询过滤掉了哪些行」与「单行判定放行哪些行」共用同一棵表达式树，
**在数学上不可能得出不同结论**。

```csharp
IScopeGuard guard = ...;

// 读侧：下推到数据库（生成的仍是表达式，由 EF/提供程序翻译成 WHERE）
IQueryable<Order> visible = guard.Apply(dbContext.Orders);

// 单行判定：编译同一对表达式后求值
bool allowed = guard.Allows(order);

// 审计：为什么可访问 / 为什么被拒绝
ScopeDecision decision = guard.Explain(order);
Console.WriteLine(decision);   // 判定：拒绝；成立的允许条件：Grant(dept)；成立的拒绝条件：Where(x => x.Level == "secret")
```

> **不要**把 `Allow`/`Deny` 塞进 EF 的全局查询过滤器（`HasQueryFilter` / `SetQueryFilter`）。
> EF 的模型（含全局过滤器）**按 DbContext 类型缓存**，而这两个表达式捕获了「每个用户不同」的集合常量，
> 一旦被烘进缓存模型就会被跨请求、跨用户复用——**这是数据泄漏**。
> 本仓 `DataContextExtensions.SetTombstoneQueryFilter` 之所以安全，只是因为它过滤的是常量 `!IsDeleted`。
> 正确做法是**逐查询应用**（`guard.Apply(query)`）；若必须用全局过滤器，表达式只能引用 DbContext 实例成员，
> 绝不能烘入用户相关的常量。

### 3.2 用户侧：授权值从数据实时解析

```csharp
public sealed class TeamScopeResolver : IScopeSubjectResolver
{
    private readonly ITeamMemberRepository _members;
    private readonly IOrgTree _org;

    public async ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;
        if (userId == null)
        {
            return ScopeSubjectSet.Empty;   // 无用户 / 匿名 → 空集合（fail-closed）
        }

        var deptIds = await _org.ExpandWithDescendantsAsync(await _members.GetDeptAsync(userId, ct), ct);

        return ScopeSubjectSet.CreateBuilder()
                              .AddSelf(userId)                                  // 「本人」= owner 维度的一个授予
                              .AddRange(ScopeDimensions.Dept, deptIds)          // 层级在解析期展开为扁平集合
                              .Add(ScopeDimensions.Region, await _members.GetRegionAsync(userId, ct))
                              .Build();
    }
}
```

要点：

- **层级在解析期展开**。框架只看到扁平集合，因此判定与下推永远只是集合成员判断（数据库侧即 `IN (...)`），
  框架不需要理解任何层级语义。
- **`Self()` 并不特殊**：它等价于 `Grant(owner)`。要让它成立，解析器必须调用 `AddSelf(userId)`。
  这是有意为之——所有者关系因此**可撤销**（不授予即不可访问本人数据）。
- 未认证用户返回空集合即自然 fail-closed，不需要额外的特判接口。

注册：

```csharp
services.AddScoped<IScopeSubjectResolver, TeamScopeResolver>();
```

### 3.3 资源侧：模型即声明

模型与策略写在**同一个类型**里，因此结构上不可能出现「声明了模型却忘了写策略」。

```csharp
public sealed class OrderScope : ScopeModel<Order>
{
    public override void Define(ScopeModelBuilder<Order> builder)
    {
        builder.Map(ScopeDimensions.Owner, x => x.OwnerId)     // 维度 → 属性表达式（可翻译）
               .Map(ScopeDimensions.Dept, x => x.DeptId)
               .Map(ScopeDimensions.Region, x => x.RegionCode)
               .Classify("level", x => x.Level);               // 分类属性：不参与授权
    }

    public override ScopePolicy<Order> Policy =>
        ScopePolicy<Order>.All(
            ScopePolicy<Order>.Any(
                ScopePolicy<Order>.Self(),
                ScopePolicy<Order>.Grant(ScopeDimensions.Dept)),
            ScopePolicy<Order>.Deny(
                ScopePolicy<Order>.Where(x => x.Level == "secret")));
}
```

映射必须是**表达式**（`Expression<Func<T,string>>`）而不是委托——这是能够下推到数据库的前提。
映射的值应当是资源的**自身数据列**，列值一变归属立即变化。

**按权限码声明行级策略**（表达「同一用户、同一类型、不同行权限不同」）：
把资源标识本身也映射为维度，再为不同权限码声明各自的行范围。

```csharp
public sealed class RepoScope : ScopeModel<Repo>
{
    public override void Define(ScopeModelBuilder<Repo> builder)
    {
        builder.Map("repo", x => x.RepoId)                  // 资源标识作为维度
               .Map(ScopeDimensions.Dept, x => x.TeamId);
    }

    // 默认策略：未单独声明的码都用它
    public override ScopePolicy<Repo> Policy => ScopePolicy<Repo>.Grant("repo");

    public override void Declare(ScopePolicySet<Repo> policies)
    {
        policies.ForOperation(BusinessOperation.Create, ScopePolicy<Repo>.Where(_ => true));  // 新建不受既有行约束
        policies.For("repo:push",   ScopePolicy<Repo>.Grant("repo"));                 // 行级 push
        policies.For("repo:delete", ScopePolicy<Repo>.Grant("repo"));                 // 行级 delete
    }
}
```

配套的授权数据（解析器侧）：

```csharp
.AddGrant("repo:push",   "repo", ["A1", "A2"])   // 这两个仓库可 push
.AddGrant("repo:delete", "repo", ["A1"])         // 只有 A1 可 delete
```

于是同一用户、同一类型下：A1 可 push+delete，A2 仅可 push，A3 都不可。

**策略键的三条硬规则**：

1. **保留命名空间 `@`**：默认键为 `@default`，操作的默认码为 `@read`/`@create`/`@update`/`@delete`/`@execute`。
   应用声明的权限码不得以 `@` 开头（启动期拒绝）。
2. **码级授予是「覆盖」，不是「并集」**：`(码, 维度)` 有授予就用它，**否则才**回落到默认键。
   若做并集，默认授予会把某个码上被收窄的行集合重新撑开，行级差异直接失效。
3. **通配不参与维度查找**：持 `repo:*` 可通过 `repo:push` 的**类型级闸门**，但**不会**让
   `(repo:*, repo)` 的授予落到 `(repo:push, repo)` 上——否则给整个命名空间授权会顺带泄漏行级授予。

**策略键如何确定**（`ScopeKeyResolver`，全框架唯一出口）：
键只由**操作**决定，操作只由 `ObjectEditState → 业务操作名` 这一条映射决定。
声明了权限码且模型为该码声明了策略 → 用该码；否则用该操作的默认键。
同一操作若解析出多个有策略的码，属配置歧义，**启动期直接失败**。

> 维度选择器的值类型目前固定为 `string`。若列是 `Guid`/`long`，请在模型里提供一个字符串投影
> （例如把 `TeamId` 声明为字符串列，或映射到一个 `string` 形式的属性）。
> 子表维度（§3.8）同理：`MapMany` 的元素取值也必须是字符串。

### 3.4 匹配语义与允许/拒绝代数

策略编译为 `(Allow, Deny)` 一对表达式，最终判定恒为 `Allow && !Deny`：

| 策略 | Allow | Deny | 是否提供允许条件 |
|---|---|---|---|
| `Grant(d)` | 单值维度：`用户在该维度被授予的值.Contains(x.D)`；子表维度（§3.8）：`x.集合.Any(v => 授予值.Contains(v.D))` | `false` | 是 |
| `Self()` | 等价于 `Grant(owner)` | `false` | 是 |
| `Where(p)` | `p` | `false` | 是 |
| `Deny(p)` | — | `p` 的成立条件 | **否** |
| `All(p…)` | 各分支允许条件的「与」 | 各分支拒绝条件的「或」 | 任一分支提供 |
| `Any(p…)` | 各分支允许条件的「或」 | 各分支拒绝条件的「或」 | 任一分支提供 |

**两条必须记住的语义**：

1. **`Deny` 是「否决」，不是布尔取反。** 它压过一切允许条件。需要真正的取反请用 `Where(x => !...)`。
   因此 `Deny(Deny(p))` 无意义，框架会直接抛异常拒绝这种写法。
2. **`Deny` 一律上浮（拒绝优先）。** 策略树中任意位置的 `Deny` 都作用于整个策略，
   包括写在 `Any` 某个分支里的。例如 `Any(Grant("dept"), Deny(x => x.Banned))` 的语义是
   「我部门的行，且任何 Banned 行都不可见」，**不是**「我部门的行 ∪ 非 Banned 的行」。
   这是有意的保守选择（防火墙式 deny 优先）。

其它规则：

- `All` / `Any` 至少需要一个子策略；「无约束」必须显式写成 `Where(_ => true)`。
- `Any` 之下若**全是**拒绝条件，结果是拒绝一切；这种策略会在启动期被拒绝，避免误配。
- 用户在某维度上没有任何授予时，生成的是**恒假常量**，不会生成空 `IN ()`。
- **没有通配符 `*`**：值空间保持纯净（不再有"数据库标识恰好等于 `*` 就全局放行"的隐患）。
  「全部放行」由 `Where(_ => true)` 或解析器返回全集显式表达。

### 3.5 用户身份与放行规则

| 当前用户 | 判定 |
|---|---|
| 未接入用户上下文（`BusinessContext.User == null`） | **全部拒绝**——授权数据一律视为空，连解析器都不调用 |
| 未认证用户（已接入 `UserPrincipal` 但未认证） | 同上 |
| 已认证用户 | 按策略判定 |

> **无用户即拒绝**：解析器一旦返回了授予集合，而调用方其实并无身份，就会退化成「匿名即放行」。
> 需要以系统 / 后台身份判定的宿主（后台任务、作业、迁移脚本），请给该场景的 `UserPrincipal`
> 一个认证身份——这比让引擎去猜「无用户大概是想放行」安全。

匿名可访问的数据（注册、密码重置等）由策略显式表达，不再需要专门的特例接口：

```csharp
public override ScopePolicy<Registration> Policy =>
    ScopePolicy<Registration>.Any(
        ScopePolicy<Registration>.Where(x => x.IsPublic),   // 显式公开
        ScopePolicy<Registration>.Self());
```

### 3.6 缓存契约

`IScopeGuard` 按请求（Scoped）注册，**每个请求只解析一次**用户主体集合，
并按类型缓存已编译的策略，读写路径共享同一份快照。因此：

- 同一请求内的多次判定结论必然一致；
- 一次列表查询不会对授权数据发起与行数相同次数的查询（N+1 的根治点）。

若在长生命周期作用域（后台 worker、单例）中使用，授权数据变化后需显式失效：

```csharp
guard.Refresh();                              // 同步：清空缓存，下次访问重新解析
await guard.RefreshAsync(cancellationToken);  // 异步：清空并立即重新解析
```

**并发行为**（`Task.WhenAll` 之类的场景）：

- 并发的首次访问**只会真正解析一次**（解析被闸门串行化），不会重复查库；
- **失效不会被在途解析回滚**：若解析进行中发生了 `Refresh()`（例如刚撤销完授权），
  那份「撤销前读到」的结果会被丢弃并重新解析，而不是覆盖失效。
  没有这条保证，一次撤销可能在竞态下被静默撤销掉。

### 3.7 启动期校验

`AddObjectPermission`（即 `AddPermission`）会在**注册期**扫描权限模型并完成校验，配置错误一律在启动时暴露，
不会等到运行期才变成「看似启用了数据权限、实际没有生效」：

- 同一资源类型存在多个权限模型 → 失败
- 模型未声明任何维度 → 失败
- **策略引用了模型中未映射的维度** → 失败（这是「策略写了却没映射 ⇒ 静默放行」的根治点）
- 策略结构性恒不放行（`Any` 之下全是拒绝条件）→ 失败
- 子表维度（§3.8）的取值形状不受支持 → 失败（只有「导航集合 + 可选 `Where` + 取字符串值」能下推为 `EXISTS`）
- 同一维度被 `Map` 与 `MapMany` 重复声明 → 失败
- `All`/`Any` 无子策略、`Deny` 嵌套 `Deny`、`Deny(null)` → 在**构造策略时**即失败

校验只在「声明了模型」时生效：没有任何 `ScopeModel<T>` 的应用照常启动，只是全部资源都不受数据权限约束。

未注册 `IScopeSubjectResolver` 但存在模型时，会在**首次判定**以明确错误抛出，绝不静默放行；
调用 `ValidatePermissionSetup()` 可让它在启动时暴露。同理，「已声明模型或权限码却没注册 `UserPrincipal`」
也会在启动期报错——其表现是所有人都被拒却毫无提示，最容易被误判成策略写错。

### 3.8 子表维度（关系表作为取值来源）

「查询我加入的团队 / 家庭 / 组织」这类权限，关系在**子表**里（`team_member(team_id, user_id, status)`），
资源却是父行（`team`）。用 `MapMany` 把子表声明为**集合维度**，判定即下推为 `EXISTS` 相关子查询：

```csharp
public sealed class TeamScope : ScopeModel<Team>
{
    public override void Define(ScopeModelBuilder<Team> builder)
        => builder.Map(ScopeDimensions.Owner, t => t.LeaderId)                     // 行内的列
                  .MapMany(ScopeDimensions.Member,                                 // 子表
                           t => t.Members.Where(m => m.Status == "active").Select(m => m.UserId));

    public override ScopePolicy<Team> Policy
        => ScopePolicy<Team>.Any(ScopePolicy<Team>.Grant(ScopeDimensions.Member),
                                 ScopePolicy<Team>.Grant(ScopeDimensions.Owner));
}
```

解析器授予的是「子表里应当出现的值」，通常就是当前用户标识：`builder.Add(ScopeDimensions.Member, userId)`。
语义与单值维度完全一致——`Grant(d)` 即「资源在该维度上的取值集合 ∩ 授予集合 ≠ ∅」；
`Deny` 之下编译为 `NOT EXISTS`。子表属性（如 `status`）写在选择器里，由数据库**实时**求值，
成员增减下一次查询即生效，不必等授权数据刷新。

三条必须知道的事：

- **单行判定要求子集合已加载**：工厂边界（`ScopeAuthorization`）与 `Allows` / `Explain` 在内存中求值同一棵表达式，
  子集合为 `null` 时抛 `InvalidOperationException`（**不是** `SecurityException`）并指明修法——
  仓储返回的实体通常不带子表，写侧尤其要注意。实体把集合初始化成空集合（`= []`）时，
  「未加载」与「没有成员」不可区分，表现为**静默拒绝**（已知边界）。
- **只支持一种取值形状**：「导航集合（可带 `Where` 过滤）再取字符串值」；其余形状在注册期被拒绝。
- **成员表的写入口必须由操作权限把守**：子表维度把「谁属于这个资源」的判定权交给了业务数据。

完整说明（含与「解析器反向展开」的取舍）见
[`Euonia.Security/README.md` §5.9](../Euonia.Security/README.md) 与
[`Euonia.Security/DESIGN.md` §1.9](../Euonia.Security/DESIGN.md)。

---

## 4. 写侧强制与已知边界

数据权限在 `BusinessObjectFactory` 的边界上与操作权限并列强制执行，失败抛 `SecurityException`。

**判定时机分为前置与后置**，依据是「目标对象在调用业务方法之前是否已经承载数据」：

| 入口 | 判定时机 | 原因 |
|---|---|---|
| `SaveAsync(target)`（New/Changed/Deleted） | **前置 + 后置** | 目标是调用方提供且已填充，可前置拒绝（无副作用）；保存后再判一次以覆盖业务方法改动范围列的情况 |
| `ExecuteAsync(target)` | **前置** | 目标是调用方提供 |
| `Create` / `CreateAsync` | **不判定** | 只构造对象、不落库，且按设计由调用方随后填充字段；此时判定会误杀正常流程，且保护不了任何东西 |
| `InsertAsync` | **后置** | 会落库；工厂方法填充完成后判定 |
| `Fetch` / `FetchAsync` | **后置** | 加载完成后才谈得上数据范围 |
| `UpdateAsync` / `DeleteAsync` / `ExecuteAsync`(criteria) | **后置** | 同上 |

> **后置检查发生在业务方法返回之后。** 若业务方法内部已经落库，它阻止的是「越权对象返回给调用方」，
> 而不是「越权数据写入」。真正的预提交强制应由持久化层（例如 EF 的 `SaveChanges` 拦截器）
> 或数据库约束保证，`Euonia.Osba` 不提供这一层。

### 4.5 与 Rule 体系的边界

**权限不通过规则表达**。早期版本有 `PermissionRule` / `ScopePolicyRule` 并对已声明模型的类型
自动注入，让越权以 `ValidationException` 的表单错误出现；这两者均已删除。理由见
[PERMISSION-DESIGN §1.2](PERMISSION-DESIGN.md#12-权限与验证是两条线越权一律抛-securityexception)。

现在两条线各管一件事，互不相干：

| | 验证线（`Rules`） | 权限线（工厂边界） |
|---|---|---|
| 回答 | 这份数据**合不合法** | 这个用户**能不能**做这件事 / 碰这行 |
| 失败 | `ValidationException` | `SecurityException` |
| 可否绕过 | 可（`SuspendRuleChecking()` / `BypassRuleChecks()` / `WithRuleChecksOnDelete()`） | **不可** |

因此**不要**用规则做授权。需要在业务方法里做条件分支（而不只是报错）时，用权限线提供的
显式查询 API：

```csharp
protected async Task CloseAsync(CancellationToken cancellationToken)
{
    if (!CanAccessRow("repo:delete"))          // 行级
    {
        throw new InvalidOperationException("无权关闭该仓库。");
    }

    if (!await CheckPermissionAsync("repo:admin", cancellationToken))   // 权限码
    {
        // ...
    }
}
```

> **越权形态一致**：新增、更新、删除、命令执行一律抛 `SecurityException`。
> `EditableObject<T>` 在 `IsDeleted` 时默认跳过的是**验证规则**，与权限无关。
> 需要让**验证**规则也覆盖删除时：调用方改用 `MarkAsDeleted(true)`（`CheckObjectRulesOnDelete`
> 是只读属性，无法重写），或走执行器时加 `.WithRuleChecksOnDelete()`。

> **越权不再出现在表单错误列表里**。需要「表单预提示」的场景，在调用侧显式捕获
> `SecurityException`，或先用 `CanAccessRow` / `CheckPermissionAsync` 查再跳。

> **范围列的"搬迁"不受保护**：业务方法可以把 `TeamId` 改到用户不属于的团队，
> 后置检查能发现并抛出，但无法阻止已经发生的写入。

未声明 `ScopeModel<T>` 的资源类型不受数据权限约束。

---

## 5. 场景示例：Dev / TeamA / TeamB / Repo

> 想一次看**多个不同业务**的完整落地（后台管理、组织部门树、行级 ACL、个人数据、
> 机密与公开、命令对象），见 [PERMISSION-SAMPLE.md](PERMISSION-SAMPLE.md)。

需求：`Dev` 属于 `TeamA`、`TeamB`（含其下级部门），可访问两团队下的仓库；
不在 `TeamC`，无法访问其仓库；机密仓库任何人都不可见；本人创建的仓库始终可访问。

```csharp
// 资源
public class Repo : EditableObject<Repo>
{
    public string OwnerId { get; set; }
    public string TeamId { get; set; }      // 仓库所属团队，取自本行数据列
    public string Level { get; set; }

    [FactoryInsert]
    protected override async Task InsertAsync(CancellationToken cancellationToken = default) { }

    [FactoryUpdate]
    protected override async Task UpdateAsync(CancellationToken cancellationToken = default) { }

    [FactoryDelete]
    protected override async Task DeleteAsync(CancellationToken cancellationToken = default) { }
}

// 模型 + 策略（同一工件）
public sealed class RepoScope : ScopeModel<Repo>
{
    public override void Define(ScopeModelBuilder<Repo> builder)
    {
        builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
               .Map(ScopeDimensions.Dept, x => x.TeamId)
               .Classify("level", x => x.Level);
    }

    public override ScopePolicy<Repo> Policy =>
        ScopePolicy<Repo>.All(
            ScopePolicy<Repo>.Any(
                ScopePolicy<Repo>.Self(),
                ScopePolicy<Repo>.Grant(ScopeDimensions.Dept)),
            ScopePolicy<Repo>.Deny(
                ScopePolicy<Repo>.Where(x => x.Level == "confidential")));
}

// 授权值来源：查成员关系表，部门树在解析期展开
public sealed class TeamScopeResolver : IScopeSubjectResolver
{
    private readonly AppDb _db;
    private readonly IOrgTree _org;

    public async ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;
        if (userId == null)
        {
            return ScopeSubjectSet.Empty;
        }

        var depts = await _org.ExpandAsync(_db.Memberships.Where(m => m.UserId == userId).Select(m => m.TeamId), ct);

        return ScopeSubjectSet.CreateBuilder().AddSelf(userId).AddRange(ScopeDimensions.Dept, depts).Build();
    }
}
```

调用：

```csharp
services.AddBusinessObject(typeof(Repo).Assembly);
services.AddObjectPermission(typeof(Repo).Assembly);
services.AddScoped<IScopeSubjectResolver, TeamScopeResolver>();

// 组织 DI + 用户后：
var guard = provider.GetRequiredService<IScopeGuard>();

// 1. 查询过滤：只返回可访问的仓库（下推到数据库）
var visible = await guard.Apply(dbContext.Repos).ToListAsync();

// 2. 单行判定
guard.Allows(repo);                       // 或 guard.Explain(repo) 看原因

// 3. 保存：越权数据在工厂边界被拒绝
var stealing = new Repo { TeamId = "TeamC", Level = "normal" };
stealing.BusinessContext = provider.GetRequiredService<BusinessContext>();
stealing.MarkAsNew();
await stealing.SaveAsync();               // TeamC 不在授予范围内 → SecurityException

// 4. 授权变更立即生效：只改成员关系数据
db.Memberships.Add(new Membership("dev", "TeamC"));
guard.Refresh();                          // 请求级缓存需显式失效（新请求自动是新快照）
guard.Allows(repoInTeamC);                // → true
```

---

## 6. 最佳实践

1. **值别进 Token/代码**：所有可能变化的授权值（团队、仓库、区域……）都从数据解析。
2. **层级在解析期展开**：部门树、组织树展开成扁平集合，保住 `IN` 下推能力。
3. **组合判定**：操作权限管「能不能做这个操作」，数据权限管「能碰到哪些行」，二者不可互相替代。
4. **读侧一律走 `guard.Apply(query)`**：不要手动拼 `IN`，也不要塞进 EF 全局查询过滤器。
5. **`Deny` 是拒绝优先，不是取反**：需要取反用 `Where(x => !...)`。
6. **写侧后置检查不是预提交校验**：关键路径请配合持久化层拦截器或数据库约束。
7. **只在需要时才声明模型**：未声明模型的资源不受约束，这是当前的边界。

---

## 7. 类型速查

按**所属程序集**分组——「这个类型我该从哪个包拿到」在解耦之后是第一个要回答的问题。

### `Euonia.Core`（权限的基础词汇，任何宿主都拿得到）

| 类型 | 用途 |
|---|---|
| `PermissionAttribute` | 声明操作权限点（类级 / 方法级） |
| `BusinessOperation` | 操作词汇（`read` / `create` / `update` / `delete` / `execute`，只是常量字符串） |
| `UserPrincipal` / `UserClaimTypes` | 判定主体与其声明类型 |

### `Euonia.Osba`（约定 + 契约 + 强制点，不引用引擎）

| 类型 | 位置 | 用途 |
|---|---|---|
| `IPermissionRequirementProvider` | `Permission/` | 契约：某类型在某操作上有哪些要求 |
| `ObjectPermissionRequirementProvider` | `Permission/` | 默认实现：按工厂约定扫描（特性或约定名） |
| `IOperationPermissionChecker` | `Permission/` | 契约：操作权限判定（由宿主提供） |
| `IObjectScopeAuthorizer` | `Permission/` | 契约：行级数据权限判定（由宿主提供） |
| `ScopeOperationMap` | `Permission/` | `ObjectEditState → BusinessOperation` 的唯一映射（适配包也用它） |
| `ObjectAuthorization` / `ScopeAuthorization` | `Permission/` | 工厂边界的两个闸门（越权抛 `SecurityException`，判定不了抛 `InvalidOperationException`） |
| `BusinessObject.CanXObject()` / `HasPermission` / `HasRole` / `CanAccessRow` / `ExplainRowAccess` / `CheckPermissionAsync` | `Core/BusinessObject.cs` | 业务对象内的权限查询（**查询语义**：无从判定时返回 `true`，拦截只在工厂边界） |

### `Euonia.Security`（策略引擎）

| 类型 | 用途 |
|---|---|
| `ScopeModel<T>` / `ScopeModelBuilder<T>` | 行级模型与维度声明（`Map` 行内列 / `MapMany` 子表） |
| `ScopePolicy<T>` / `ScopePolicySet<T>` | 策略组合子与按权限码声明的行级策略 |
| `IScopeGuard` / `ScopeGuard` | 数据权限判定入口（按请求缓存） |
| `IScopeSubjectResolver` / `ScopeSubjectSet` | 授权值来源与主体集合 |
| `IPermissionChecker` / `SubjectPermissionChecker` | 操作权限判定与其默认实现（权限码来自授权数据） |
| `ClaimPermissionChecker` | `[Obsolete]` 回退：读 `"perm"` 声明（不推荐） |
| `IPermissionCodeSource` / `IPermissionRequirementSource` | 注册期校验用的权限码 / 要求来源 |
| `IScopeKeyResolver` / `ScopeKeyResolver` / `ScopeKeys` | 策略键的解析出口与保留命名空间 |
| `ScopeFilter` / `CompiledScopePolicy<T>` / `ScopeDecision` | 下推、内存过滤、单行判定与审计 |
| `ScopeDimensions` | 维度名常量（`Owner` / `Dept` / `Member` / `Region` / `Project`） |
| `ScopeModelRegistry` / `PermissionSetup` / `ValidatePermissionSetup()` | 注册表与启动期校验 |

### `Euonia.Osba.Security`（适配包）

| 类型 | 用途 |
|---|---|
| `AddObjectPermission(assemblies)` | 唯一入口：注册 Osba 契约的引擎实现 + 把 Osba 的工厂约定交给引擎 |
| `ObjectScopeKeyResolver` / `EngineRequirementProvider` / `EngineOperationPermissionChecker` / `EngineObjectScopeAuthorizer` | 适配实现（内部类型，无需直接使用） |

---

## 8. 关键成员速查

### 授权数据（应用实现）

```csharp
public interface IScopeSubjectResolver
{
    ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default);
}

public sealed class ScopeSubjectSetBuilder
{
    ScopeSubjectSetBuilder AddCode(string code);                                  // 类型级权限码
    ScopeSubjectSetBuilder AddCodes(IEnumerable<string> codes);
    ScopeSubjectSetBuilder AddSelf(string userId);                                // = Add(owner, userId)
    ScopeSubjectSetBuilder Add(string dimension, string value);                   // 默认键上的维度授予
    ScopeSubjectSetBuilder AddRange(string dimension, IEnumerable<string> values);
    ScopeSubjectSetBuilder AddGrant(string code, string dimension, string value); // 按码的行级授予
    ScopeSubjectSetBuilder AddGrant(string code, string dimension, IEnumerable<string> values);
    ScopeSubjectSet Build();
}

public sealed class ScopeSubjectSet
{
    IReadOnlyCollection<string> Codes { get; }                     // 用户持有的权限码
    bool HoldsPermission(string code);                             // 支持 * 前缀通配
    bool Contains(string code, string dimension, string value);
    IReadOnlyCollection<string> ValuesOf(string code, string dimension);
}
```

### 资源声明（使用方）

```csharp
public abstract class ScopeModel<T>
{
    public abstract void Define(ScopeModelBuilder<T> builder);     // 维度 → 属性表达式
    public abstract ScopePolicy<T> Policy { get; }                 // 必填：默认策略
    public virtual void Declare(ScopePolicySet<T> policies) { }    // 可选：按权限码覆盖
}

public sealed class ScopeModelBuilder<T>
{
    ScopeModelBuilder<T> Map(string dimension, Expression<Func<T, string>> selector);        // 行内的列
    ScopeModelBuilder<T> MapMany(string dimension,                                           // 子表（§3.8）
                                 Expression<Func<T, IEnumerable<string>>> selector);
    ScopeModelBuilder<T> Classify(string name, Expression<Func<T, object>> selector);       // 不参与授权
}

public sealed class ScopePolicySet<T>
{
    ScopePolicySet<T> For(BusinessOperation operation, ScopePolicy<T> policy);   // @read/@create/…
    ScopePolicySet<T> For(string code, ScopePolicy<T> policy);                   // 如 "repo:push"
}

public abstract class ScopePolicy<T>
{
    static ScopePolicy<T> Self();                                  // = Grant(owner)
    static ScopePolicy<T> Grant(string dimension);
    static ScopePolicy<T> Where(Expression<Func<T, bool>> predicate);   // ABAC 逃生舱
    static ScopePolicy<T> All(params ScopePolicy<T>[] policies);
    static ScopePolicy<T> Any(params ScopePolicy<T>[] policies);
    static ScopePolicy<T> Deny(ScopePolicy<T> policy);             // 否决，压过一切
}
```

### 判定入口

```csharp
public interface IScopeGuard
{
    ScopeSubjectSet GetSubjects();
    IReadOnlyCollection<string> Permissions { get; }
    IQueryable<T> Apply<T>(IQueryable<T> source, string code = null);      // 读侧下推
    bool Allows<T>(T resource, string code = null);                        // 单行判定
    ScopeDecision Explain<T>(T resource, string code = null);              // 审计
    bool AllowsObject(object resource, string code = null);                // 非泛型（写侧用）
    string ExplainObject(object resource, string code = null);
    void Refresh();                                                        // 显式失效
    ValueTask<ScopeSubjectSet> RefreshAsync(CancellationToken ct = default);
    ValueTask EnsureResolvedAsync(CancellationToken ct = default);
}
```

### 业务对象内的断言

```csharp
protected bool HasPermission(string permission);            // 权限码（同步，可能阻塞一次）
protected bool HasRole(string role);
protected ValueTask<bool> CheckPermissionAsync(string permission, CancellationToken ct = default);
protected bool CanAccessRow(string code = null);            // 本行是否在范围内
protected string ExplainRowAccess(string code = null);      // 判定原因
```

---

## 9. 故障排查

### 启动期异常（都是**有意**的 fail-fast）

| 消息关键词 | 原因 | 处理 |
|---|---|---|
| `使用了框架保留前缀` | 权限码以 `@` 开头 | 改名为不含 `@` 的码；框架自身的默认键只能由 `For(BusinessOperation, …)` 声明 |
| `解析出多个声明了行级策略的权限码` | 同一操作上多个 `[Permission]` 都配了策略 | 只保留一个；或把其余的策略去掉改为共用默认策略 |
| `引用了未映射的维度` | 策略里 `Grant("x")` 但模型没 `Map("x", …)` | 补 `Map`，或改用正确维度名 |
| `没有任何操作会解析到该码`（死策略） | `Declare` 里写的码与方法上 `[Permission]` 的码对不上（多半是拼写不一致） | 核对两处字面量；错误消息会列出实际解析到的码 |
| `未声明任何维度` | 模型没调用 `Map` | 至少映射一个维度 |
| `取值表达式 ... 不受支持` | 子表维度（§3.8）的选择器不是「导航集合 + 可选 `Where` + 取字符串值」 | 改成受支持的形状；行内的单值请用 `Map` |
| `维度 'x' 在类型 'Y' 的权限模型中重复声明` | 同一维度被 `Map` 与 `MapMany` 各声明了一次 | 一个维度只能有一个取值来源 |
| `结构性恒不放行` | `Any` 之下全是 `Deny` 分支 | 补上 `Grant`/`Where`/`Self` 等允许条件 |
| `存在多个权限模型` | 同一资源类型有两个 `ScopeModel<T>` | 合并为一个 |
| `未注册 IScopeSubjectResolver` | 声明了模型或 `[Permission]` 却没接解析器 | 注册实现，并调用 `provider.ValidatePermissionSetup()` |

### 运行期异常

| 异常 | 含义 |
|---|---|
| `InvalidOperationException`：未注册 `IScopeSubjectResolver` | 启动期校验被跳过，首次判定时兜底暴露 |
| `InvalidOperationException`：提示含 `BusinessContext` | 目标声明了权限要求/数据范围模型，却没接入 `BusinessContext`——多半是 `new` 出对象后忘了接线。请走工厂创建，或在调用前设置 `BusinessContext` |
| `InvalidOperationException`：提示含「子表维度」 | 单行判定遇到未加载的子集合（§3.8）。它不是越权，**不要**当成 `SecurityException` 捕获；按下推/加载/去掉初始化器三条修法处理 |
| `SecurityException` | 工厂边界判定越权——操作权限或数据范围不满足（新增/更新/删除/命令**一致**） |

### 判定结果不符合预期

- **「本人怎么也不可见」**：`Self()` 等价于 `Grant(owner)`，解析器必须调用 `AddSelf(userId)`。
  这是有意的——所有权因此可撤销；漏了是 fail-closed，不会反向放行。
- **「deny 好像影响了别的分支」**：这是设计使然。`Deny` 是**全局否决**且一律上浮，
  `Any(Grant("dept"), Deny(x))` 的语义是「我部门的行，且任何命中 x 的行都不可见」，
  **不是**「我部门的行 ∪ 非 x 的行」。需要真取反请用 `Where(x => !...)`。
- **「码上写了 {A1}，但 A2 也通过了」**：检查是否在默认键上给了更大的集合——
  码级授予是**覆盖**默认键，但只有在**该码上确实有该维度的授予**时才覆盖。
- **「持有了 `repo:*`，行级却是空的」**：这是有意的。权限码通配只影响**类型级闸门**，
  不参与维度查找——否则给整个命名空间授权会顺带泄漏行级授予。
- **「某类型完全不受限」**：该类型没有声明 `ScopeModel<T>`。未声明即不受数据权限约束，
  这是当前边界（框架无法自动识别「哪些类型应受控」）。

### 下推相关问题

- **列表查询没有过滤**：读侧必须显式走 `guard.Apply(query)`，框架不会自动介入。
- **不要**把 `Allow`/`Deny` 塞进 EF 全局查询过滤器——见 §3.1 的警告。
- 生成的 SQL 里出现空 `IN ()` 是不可能的：码下无授予时直接产出恒假常量。
- 子表维度（§3.8）产出的是 `EXISTS` 相关子查询，因此 `Apply` 必须作用在**实体查询**上、
  且在投影之前：投影成 DTO 后集合已被物化，无法再翻译。
- 子表维度在内存判定（`Allows`）前多一次加载探测；大批量对象请走 `Apply` 而不是逐行判定。

---

## 10. 性能与下推注意事项

**按请求缓存**：`IScopeGuard` 是 Scoped，授权数据与已编译策略在一次请求内只解析/编译一次，
读写共用。因此**撤销的生效时机是「下一次解析」**；同一作用域内需显式 `Refresh()`。
长生命周期作用域（后台 worker、单例）必须自行 `Refresh()`。

**下推是首选**：`guard.Apply(query)` 产出的是表达式树，由 EF/提供程序翻成 `WHERE`。
`ScopeFilter.Filter(IEnumerable<T>, …)` 只在数据已在内存时使用。

**烘入常量与参数化**：用户被授予的值在编译期被烘成表达式常量
（`ids.Contains(x.DeptId)`）。同一请求内同一码只会编译一次，因此 `IN` 列表是稳定的。

**行级 ACL 的反向展开成本**：行级策略要求解析器回答「此用户在此码下能碰哪些资源 id」。
若 ACL 正向存储（「哪些用户能操作 A1」），需要翻成 id 列表——列表可能很大。
建议**授权尽量授「组 id」而非「行 id」**；行数巨大时改用
`Where(x => aclQuery.Contains(x.Id))` 逃生舱，并自行评估执行成本。
若关系就在同库的子表里（成员表 / 关系表），改用子表维度（§3.8）可免去反向展开。

**子表维度的下推成本**：产出的 `EXISTS` 子查询按子表的过滤列取数，
请在子表上建 `(父标识, 值)` 组合索引（例如 `(team_id, user_id)`），否则外层每一行都要扫一遍子表。

**同步与异步**：`IPermissionChecker` 是同步接口，首次判定会走一次 sync-over-async
（每作用域仅一次）。工厂的异步入口可优先用 `CheckPermissionAsync` 避免阻塞线程。

## 11. 从旧数据权限迁移

旧的一套（`ScopeTag` / `IDataScoped` / `IUserScopeProvider` / `IDataScopeService` / `DataScopeRule` /
`ClaimsUserScopeProvider` / `IAnonymousAccessible`）已被**整体替换**，不再提供。对照关系：

| 旧 | 新 |
|---|---|
| `IDataScoped.ScopeTags` 运行时拼标签 | `ScopeModel<T>.Define` 声明维度 → 属性**表达式**（可下推） |
| `IDataScoped.OwnerId` 硬编码特例 | `ScopeDimensions.Owner` 普通维度；`Self()` 是其语法糖，可撤销 |
| `IUserScopeProvider.ResolveScopes(user)` | `IScopeSubjectResolver.ResolveAsync(ClaimsPrincipal, ct)`（异步、可取消） |
| `IDataScopeService.CanAccess(row)` | `IScopeGuard.Allows(row)` |
| `IDataScopeService.CreateScopePredicate<T>()` / `Filter<T>()`（仅内存） | `IScopeGuard.Apply(IQueryable<T>)`（下推）+ `ScopeFilter.Filter`（内存） |
| 固定「跨维度 AND / 同维度 OR」 | `All` / `Any` 任意嵌套 + `Deny` |
| 无 deny | `Deny` 一等公民，拒绝优先 |
| `DataScopeRule`（需手工 `AddRule` 注册） | 工厂边界**自动**强制（`SaveAsync` 前置+后置），不再需要注册任何规则 |
| `IAnonymousAccessible` 特例接口 | 策略里的 `Where(x => x.IsPublic)`（显式、可审计） |
| `"*"` 通配（占用值空间） | 已移除；用 `Where(_ => true)` 或解析器返回全集 |
| `ClaimsUserScopeProvider`（从声明解析） | 已移除；请实现基于授权数据的 `IScopeSubjectResolver` |
| `ClaimPermissionChecker`（权限码读 `"perm"` 声明） | `SubjectPermissionChecker`（权限码读授权数据，撤销立即生效）；旧类保留但已 `[Obsolete]` |
| 类型级 `[Permission]` 唯一粒度 | 同一类型内可按权限码声明行级策略（`Declare`），行与行之间权限可不同 |

**迁移检查项**：

- 旧实现里「实现 `IAnonymousAccessible` 即可匿名读取」的类型，在新体系下若没有显式的
  `Where(...)` 允许条件，将对匿名用户**完全不可见**——必须补上。
- 旧的「所有者快速路径」无条件放行，新体系下需要解析器 `AddSelf(userId)` 才成立；漏了会导致
  「本人数据也不可见」（fail-closed，不会反向放行）。
- **权限码必须改由解析器提供**：原先写在令牌 `"perm"` 声明里的码，若既不迁移到授权数据、
  也不注册解析器，启动期校验会直接失败（不会静默放行）。
- **不要用规则做授权**：早前提供过 `PermissionRule` / `ScopePolicyRule` 让越权以
  `ValidationException` 暴露，两者已删除。捕获 `ValidationException` 的调用方需要改为捕获
  `SecurityException`——表单错误列表里不再含越权信息。
