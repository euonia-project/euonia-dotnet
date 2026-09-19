# Euonia.Osba 权限控制使用说明

`Euonia.Osba` 提供两套相辅相成的权限控制：

| | 操作权限（Operation Permission） | 数据权限（Data Permission） |
|---|---|---|
| 回答的问题 | 当前用户**能否执行某项操作** | 当前用户**能看到/操作哪些数据行** |
| 作用对象 | 操作 × 对象类型（类级/方法级） | 声明了 `ScopeModel<T>` 的资源类型 |
| 判定依据 | 权限声明（或角色），一般来自用户声明 | 资源属性 × 用户从**授权数据**实时解析出的主体集合 |
| 强制执行点 | `BusinessObjectFactory` 调用边界 | `IScopeGuard.Apply(IQueryable)` + 工厂保存边界 |
| 失败形态 | 抛 `System.Security.SecurityException` | 查询排除该行 / 保存抛 `SecurityException` |

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

若使用数据权限，还必须**由应用注册一个 `IScopeSubjectResolver`**（见 [3.2](#32-用户侧授权值从数据实时解析)），
框架不提供默认实现，以免把授权值固化。

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

默认检查器 `ClaimPermissionChecker` 从 `UserClaimTypes.Permission`（声明类型 `"perm"`）读取权限：

- 精确匹配：用户 `"order:create"` → 可通过 `order:create`
- 前缀通配：用户 `"order:*"` → 可匹配任何 `order:xxx`
- 大小写不敏感

构造带权限声明的用户（示例）：

```csharp
var identity = new ClaimsIdentity(
    new[]
    {
        new Claim(UserClaimTypes.Subject, "u1"),
        new Claim(UserClaimTypes.Permission, "order:create"),
        new Claim(UserClaimTypes.Role, "Admin"),
    },
    "Bearer",
    ClaimTypes.Name,
    UserClaimTypes.Role);
var user = new UserPrincipal(new ClaimsPrincipal(identity));
```

### 2.3 强制执行

`BusinessObjectFactory` 的所有入口（`Create`、`Fetch`、`InsertAsync`、`UpdateAsync`、
`DeleteAsync`、`SaveAsync`、`ExecuteAsync` 等）在调用业务方法前统一执行
`ObjectAuthorization.EnsureAuthorized`：不满足要求即抛 `SecurityException`。
`SaveAsync` 会根据对象状态映射操作（New→`Create`、Changed→`Update`、Deleted→`Delete`，
命令对象→`Execute`）。行为约定：

- 没有任何 `[Permission]` → 放行
- 未注册 `IPermissionChecker` → 放行
- 有要求但未认证/未授权 → 拒绝（抛异常）

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
| `ClaimPermissionChecker` | `Permission/` | 默认实现（读 `"perm"` 声明，支持 `*` 前缀通配） |
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

## 8. 从旧数据权限迁移

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

**迁移检查项**：

- 旧实现里「实现 `IAnonymousAccessible` 即可匿名读取」的类型，在新体系下若没有显式的
  `Where(...)` 允许条件，将对匿名用户**完全不可见**——必须补上。
- 旧的「所有者快速路径」无条件放行，新体系下需要解析器 `AddSelf(userId)` 才成立；漏了会导致
  「本人数据也不可见」（fail-closed，不会反向放行）。
