# Euonia.Osba 权限控制使用说明

`Euonia.Osba` 提供两套相辅相成的权限控制：

| | 操作权限（Operation Permission） | 数据权限（Data Permission） |
|---|---|---|
| 回答的问题 | 当前用户**能否执行某项操作** | 当前用户**能看到/操作哪些数据行** |
| 作用对象 | 操作 × 对象类型（类级/方法级） | 声明了 `ScopeModel<T>` 的资源类型 |
| 判定依据 | 权限声明（或角色），一般来自用户声明 | 资源属性 × 用户从**授权数据**实时解析出的主体集合 |
| 强制执行点 | `BusinessObjectFactory` 调用边界 | `IScopeGuard.Apply(IQueryable)` + 工厂保存边界 |
| 失败形态 | 抛 `System.Security.SecurityException` | 查询排除该行 / 保存抛 `SecurityException` |

> 设计动因、被否决的方案与已知边界见 [DESIGN.md](DESIGN.md)（面向维护者）。

> **核心原则：能预定义的进代码，不能预定义的走数据。**
> 操作类型、维度、判定语义可以预定义；而"用户属于哪些团队、能访问哪些仓库"这类授权值
> 随时可变（团队/资源会新建删除、人员会调整），**必须从应用数据实时解析，绝不能固化
> 在声明/Token/代码字面量里**。这决定了操作权限用声明，数据权限用数据源。

---

## 1. 准备工作

```csharp
var services = new ServiceCollection();

// 注册 Osba 基础设施
services.AddBusinessObject(typeof(Order).Assembly);
```

`AddBusinessObject` 会注册：

- `BusinessContext` / `BusinessContextAccessor` / `IActuator`
- `IObjectFactory` → `BusinessObjectFactory`
- `IPermissionChecker` → `ClaimPermissionChecker`（基于权限声明）
- `ScopeModelRegistry`（数据权限模型注册表，注册期即完成校验）
- `IScopeGuard` → `ScopeGuard`（数据权限判定入口，按请求缓存）

若使用权限（操作权限的权限码或数据权限），还必须**由应用注册一个 `IScopeSubjectResolver`**
（见 [3.2](#32-用户侧授权数据实时解析)），框架不提供默认实现，以免把授权值固化。

容器构建后请调用一次启动期校验，使「声明了权限却忘了接解析器」在启动时失败：

```csharp
var provider = services.BuildServiceProvider();
provider.ValidatePermissionSetup();   // 缺少 IScopeSubjectResolver 时在此抛出
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

工厂方法特性与 `BusinessOperation` 的对应关系：

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

- 没有任何 `[Permission]` → 放行
- 未注册 `IPermissionChecker` → 放行
- 有要求但未认证/未授权 → 拒绝（抛异常）

除工厂边界外，框架还会对**已声明权限模型的类型**自动注入数据范围规则（见 [4.5](#45-与-rule-体系的适配)），
使越权在保存前以验证错误暴露。两者分工：**工厂边界是权威强制点，规则是前置的、UI 友好的补充信号**。

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
        policies.For(BusinessOperation.Create, ScopePolicy<Repo>.Where(_ => true));  // 新建不受既有行约束
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
键只由**操作**决定，操作只由 `ObjectEditState → BusinessOperation` 这一条映射决定。
声明了权限码且模型为该码声明了策略 → 用该码；否则用该操作的默认键。
同一操作若解析出多个有策略的码，属配置歧义，**启动期直接失败**。

> 维度选择器的值类型目前固定为 `string`。若列是 `Guid`/`long`，请在模型里提供一个字符串投影
> （例如把 `TeamId` 声明为字符串列，或映射到一个 `string` 形式的属性）。

### 3.4 匹配语义与允许/拒绝代数

策略编译为 `(Allow, Deny)` 一对表达式，最终判定恒为 `Allow && !Deny`：

| 策略 | Allow | Deny | 是否提供允许条件 |
|---|---|---|---|
| `Grant(d)` | `用户在该维度被授予的值.Contains(x.D)` | `false` | 是 |
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
| 未接入用户上下文（`BusinessContext.User == null`，如后台任务） | 不做限制（无从判定） |
| 匿名用户（已接入 `UserPrincipal` 但未认证） | 解析器通常返回空集合 ⇒ `Grant` 一律不成立 ⇒ 默认拒绝 |
| 已认证用户 | 按策略判定 |

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

### 3.7 启动期校验

`AddBusinessObject` 会在**注册期**扫描权限模型并完成校验，配置错误一律在启动时暴露，
不会等到运行期才变成「看似启用了数据权限、实际没有生效」：

- 同一资源类型存在多个权限模型 → 失败
- 模型未声明任何维度 → 失败
- **策略引用了模型中未映射的维度** → 失败（这是「策略写了却没映射 ⇒ 静默放行」的根治点）
- 策略结构性恒不放行（`Any` 之下全是拒绝条件）→ 失败
- `All`/`Any` 无子策略、`Deny` 嵌套 `Deny`、`Deny(null)` → 在**构造策略时**即失败

校验只在「声明了模型」时生效：没有任何 `ScopeModel<T>` 的应用照常启动，只是全部资源都不受数据权限约束。

未注册 `IScopeSubjectResolver` 但存在模型时，会在**首次判定**以明确错误抛出，绝不静默放行。

---

## 4. 写侧强制与已知边界

数据权限在 `BusinessObjectFactory` 的边界上与操作权限并列强制执行，失败抛 `SecurityException`。

**判定时机分为前置与后置**，依据是「目标对象在调用业务方法之前是否已经承载数据」：

| 入口 | 判定时机 | 原因 |
|---|---|---|
| `SaveAsync(target)`（New/Changed/Deleted） | **前置 + 后置** | 目标是调用方提供且已填充，可前置拒绝（无副作用）；保存后再判一次以覆盖业务方法改动范围列的情况 |
| `ExecuteAsync(target)` | **前置** | 目标是调用方提供 |
| `Create` / `CreateAsync` / `InsertAsync` | **后置** | 目标是工厂新建的空对象，范围列由业务方法填充 |
| `Fetch` / `FetchAsync` | **后置** | 加载完成后才谈得上数据范围 |
| `UpdateAsync` / `DeleteAsync` / `ExecuteAsync`(criteria) | **后置** | 同上 |

> **后置检查发生在业务方法返回之后。** 若业务方法内部已经落库，它阻止的是「越权对象返回给调用方」，
> 而不是「越权数据写入」。真正的预提交强制应由持久化层（例如 EF 的 `SaveChanges` 拦截器）
> 或数据库约束保证，`Euonia.Osba` 不提供这一层。

### 4.5 与 Rule 体系的适配

框架对**已声明权限模型的类型**自动注入 `ScopePolicyRule`，使越权在保存前以**验证错误**暴露，
无需手工 `AddRule`。也可以手工注册：

```csharp
protected override void AddRules()
{
    Rules.AddRule(new PermissionRule("repo:force-push"));   // 断言权限码
    Rules.AddRule(new ScopePolicyRule());                    // 断言当前行在范围内
}
```

业务方法内可直接断言（做条件分支，而不只是报错）：

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

**失败形态的分工**（重要）：

| 路径 | 结果 |
|---|---|
| 规则阶段（`SaveAsync` 的新增/更新） | `ValidationException`（可携带字段级错误，适合表单） |
| 工厂边界（criteria 入口、规则被跳过时） | `SecurityException`（越权） |

> **删除路径不对称**：`EditableObject<T>` 在 `IsDeleted` 时**默认跳过对象级规则**，
> 因此越权**删除**由工厂边界兜住，抛 `SecurityException` 而非 `ValidationException`。
> 需要规则覆盖删除时，重写 `CheckObjectRulesOnDelete` 返回 `true`。

> **规则不是强制点**：`SuspendRuleChecking()` 与 `BypassRuleChecks` 都能跳过规则，
> 且规则只覆盖保存路径。**工厂边界始终是权威强制点**。
>
> **范围列的"搬迁"不受保护**：业务方法可以把 `TeamId` 改到用户不属于的团队，
> 后置检查能发现并抛出，但无法阻止已经发生的写入。

未声明 `ScopeModel<T>` 的资源类型不受数据权限约束。

---

## 5. 场景示例：Dev / TeamA / TeamB / Repo

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

| 类型 | 位置 | 用途 |
|---|---|---|
| `BusinessOperation` | `Permission/` | 操作类型枚举（Read/Create/Update/Delete/Execute） |
| `PermissionAttribute` | `Permission/` | 声明操作权限点（类级/方法级） |
| `IPermissionChecker` | `Permission/` | 权限判断抽象 |
| `SubjectPermissionChecker` | `Permission/` | **默认**实现：权限码来自授权数据（撤销立即生效） |
| `ClaimPermissionChecker` | `Permission/` | `[Obsolete]` 回退：读 `"perm"` 声明（不推荐） |
| `PermissionRequirements` | `Permission/` | 权限要求收集（运行期判定与启动期校验共用） |
| `ScopeKeys` / `ScopeKeyResolver` | `Permission/` | 策略键的保留命名空间与**唯一**解析出口 |
| `ScopeOperationMap` | `Permission/` | `ObjectEditState → BusinessOperation` 的唯一映射 |
| `ScopePolicySet<T>` | `Permission/Scope/` | `ScopeModel<T>.Declare` 入参：按权限码声明行级策略 |
| `PermissionSetup` / `ValidatePermissionSetup()` | `Permission/` | 启动期检查解析器是否齐备 |
| `PermissionRule` / `ScopePolicyRule` | `Permission/Rules/` | 权限的规则化（验证错误而非异常） |
| `ScopeDimensions` | `Permission/Scope/` | 维度名常量（`Owner`/`Dept`/`Region`/`Project`）与校验入口 |
| `ScopeSubject` / `ScopeSubjectSet` | `Permission/Scope/` | 用户被授予的主体及集合（维度名大小写不敏感，值精确比较） |
| `ScopeSubjectSetBuilder` | `Permission/Scope/` | 解析器构造主体集合（`Add`/`AddRange`/`AddSelf`） |
| `IScopeSubjectResolver` | `Permission/Scope/` | 授权值来源（应用实现，实时解析） |
| `ScopeModel<T>` / `IScopeModel<T>` | `Permission/Scope/` | 资源模型 + 策略的声明基类 |
| `ScopeModelBuilder<T>` | `Permission/Scope/` | `Map` 维度、`Classify` 分类属性 |
| `ScopePolicy<T>` | `Permission/Scope/` | 策略组合子（`Self`/`Grant`/`All`/`Any`/`Deny`/`Where`） |
| `CompiledScopePolicy<T>` | `Permission/Scope/` | 编译结果：`Allow`/`Deny` 一对表达式 |
| `ScopePolicyCompiler` | `Permission/Scope/` | 唯一编译出口 |
| `ScopeFilter` | `Permission/Scope/` | `Apply`（下推）/ `Allows`（单行）/ `Explain`（审计） |
| `ScopeDecision` | `Permission/Scope/` | 判定结果与命中路径 |
| `IScopeGuard` / `ScopeGuard` | `Permission/Scope/` | 按请求缓存的统一入口 |
| `ScopeModelRegistry` | `Permission/Scope/` | 模型注册表与启动期校验 |
| `UserClaimTypes.Permission` | `Euonia.Core` | 操作权限声明类型（`"perm"`） |

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
    ScopeModelBuilder<T> Map(string dimension, Expression<Func<T, string>> selector);
    ScopeModelBuilder<T> Classify(string name, Expression<Func<T, object>> selector);   // 不参与授权
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
| `未声明任何维度` | 模型没调用 `Map` | 至少映射一个维度 |
| `结构性恒不放行` | `Any` 之下全是 `Deny` 分支 | 补上 `Grant`/`Where`/`Self` 等允许条件 |
| `存在多个权限模型` | 同一资源类型有两个 `ScopeModel<T>` | 合并为一个 |
| `未注册 IScopeSubjectResolver` | 声明了模型或 `[Permission]` 却没接解析器 | 注册实现，并调用 `provider.ValidatePermissionSetup()` |

### 运行期异常

| 异常 | 含义 |
|---|---|
| `InvalidOperationException`：未注册 `IScopeSubjectResolver` | 启动期校验被跳过，首次判定时兜底暴露 |
| `ValidationException` | 自动注入的范围规则判定越权（新增/更新路径） |
| `SecurityException` | 工厂边界判定越权（criteria 入口、删除路径、或规则被跳过时） |

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

**同步与异步**：`IPermissionChecker` 是同步接口，首次判定会走一次 sync-over-async
（每作用域仅一次）。工厂的异步入口可优先用 `CheckPermissionAsync` 避免阻塞线程。

## 11. 从旧数据权限迁移

旧的一套（`ScopeTag` / `IDataScoped` / `IUserScopeProvider` / `IDataScopeService` / `DataScopeRule` /
`ClaimsUserScopeProvider` / `IAnonymousAccessible`）已被**整体替换**，不再提供。对照关系：

| 旧 | 新 |
|---|---|
| `IDataScoped.ScopeTags` 运行时拼标签 | `ScopeModel<T>.Define` 声明维度 → 属性**表达式**（可下推） |
| `IDataScoped.OwnerId` 硬编码特例 | `ScopeDimensions.Owner` 普通维度；`Self()` 是其语法糖，可撤销 |
| `IUserScopeProvider.ResolveScopes(user)` | `IScopeSubjectResolver.ResolveAsync(claims, ct)`（异步、可取消） |
| `IDataScopeService.CanAccess(row)` | `IScopeGuard.Allows(row)` |
| `IDataScopeService.CreateScopePredicate<T>()` / `Filter<T>()`（仅内存） | `IScopeGuard.Apply(IQueryable<T>)`（下推）+ `ScopeFilter.Filter`（内存） |
| 固定「跨维度 AND / 同维度 OR」 | `All` / `Any` 任意嵌套 + `Deny` |
| 无 deny | `Deny` 一等公民，拒绝优先 |
| `DataScopeRule`（需手工 `AddRule` 注册） | 工厂边界**自动**强制（`SaveAsync` 前置+后置） |
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
- **运行期行为变化**：越权**更新**现在抛 `ValidationException`（自动注入的范围规则先命中），
  越权**删除**仍抛 `SecurityException`。按异常类型做 400/403 区分的调用方需要相应调整。
