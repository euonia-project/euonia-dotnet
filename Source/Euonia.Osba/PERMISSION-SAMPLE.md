# Euonia.Osba 权限体系：多场景应用示例

> 面向需要「照着改就能用」的读者。每个场景都是独立的一段业务：给出领域需求、
> 资源与操作声明、授权数据解析器、判定调用与运行结果，最后列关键语义。
> 概念、类型速查与故障排查见 [PERMISSION.md](PERMISSION.md)，设计取舍见
> [`Euonia.Security/DESIGN.md`](../Euonia.Security/DESIGN.md)，
> 体系架构总览（图）见 [`Euonia.Security/DESIGN.md`](../Euonia.Security/DESIGN.md) §0.1。

**文中 API 与框架实现一致，判定语义均与单元测试（`PermissionTests`、`ScopeTests`、
`ScopeRowPermissionTests`、`SubTableDimensionTests`）验证过的行为对齐**。示例里的存储与授权数据是内存模拟
（真实系统里是数据库表），请替换为你自己的数据访问层。

| 场景 | 需求一句话 | 主要能力 |
|---|---|---|
| [一、后台管理](#场景一后台管理操作权限) | 管理员操作后台，运营按模块细分 | 操作权限：类级 / 方法级、多码 AND、角色 OR、通配 |
| [二、组织部门树](#场景二组织部门树数据可见范围) | 按部门与区域看仓库 | 数据权限：层级展开、跨维度 OR、读侧下推 |
| [三、行级资源授权](#场景三行级资源授权同一类型不同行权限不同) | 同一仓库，push 与 delete 权限因人而异 | 数据权限：`AddGrant` 行级、`Declare` 按码策略、写侧强制 |
| [四、个人数据](#场景四个人数据self--可撤销) | 自己创建的内容自己可见 | `Self()`、所有者可撤销、缓存失效 |
| [五、机密与公开](#场景五机密与公开deny--匿名公开) | 公开的匿名可见，机密的连本人也不见 | `Deny` 全局否决、`Where(IsPublic)`、匿名 fail-closed |
| [六、命令对象](#场景六命令对象--授权变更实时生效) | 导出报表是允许就执行、撤销立即生效 | 命令对象权限、撤销 vs Token、移除即生效 |
| [七、子表维度](#场景七子表维度查询我加入的团队) | 查询我加入的团队 / 家庭 / 组织 | 子表维度：`MapMany`、`EXISTS` 下推、写侧对象图要求 |

---

## 0. 场景通用装配

每个场景都需要这段（其中 `AuthzStore` 是「授权数据」的内存模拟，见各场景）。
`AddPermission` 是策略引擎自己的入口，规则来源直接用 Osba 的工厂约定——**不需要任何适配包**：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

var services = new ServiceCollection();
services.AddBusinessObject(typeof(Order).Assembly);             // 扫描业务对象（不启用权限）
services.AddPermission(ObjectPermissionRequirementProvider.Instance, typeof(Order).Assembly);           // 显式启用权限：Osba 的码来源 + 策略键推断
services.AddSingleton<AuthzStore>();                            // 授权数据（模拟数据库表，见各场景）
services.AddScoped<IScopeSubjectResolver, /* 各场景的解析器 */>();  // 授权值来源
services.AddSingleton(DemoUser.Dev);                            // 当前用户（UserPrincipal）

var provider = services.BuildServiceProvider();
provider.ValidatePermissionSetup();        // 声明了权限/模型却忘了接解析器 → 启动即失败

using var scope = provider.CreateScope();
BusinessContextAccessor.SetCurrent(scope.ServiceProvider);     // 先设上下文，再取服务

var factory = scope.ServiceProvider.GetRequiredService<IObjectFactory>();
var guard   = scope.ServiceProvider.GetRequiredService<IScopeGuard>();
```

> 每段场景的装配不同点只有一处：**注册哪个解析器**。直接对照各场景替换上面那行。

不含任何权限声明的用户（权限码只能来自授权数据，见各场景的解析器）：

```csharp
public static class DemoUser
{
    public static UserPrincipal Dev => Create("dev");

    private static UserPrincipal Create(string userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(UserClaimTypes.Subject, userId)],
            "Bearer", ClaimTypes.Name, UserClaimTypes.Role);
        return new UserPrincipal(new ClaimsPrincipal(identity));
    }
}
```

> 手动作用域的顺序：**先** `SetCurrent(...)`，**再**解析 `BusinessContext` / 工厂；用完
> `BusinessContextAccessor.Clear()`（静态 AsyncLocal）。ASP.NET Core 下由中间件自动完成。

各场景的解析器都依赖 `IAuthzData`——它就是「授权数据库」的抽象：
权限码/角色表、用户的自属关系（`AddSelf` 的来源）、以及行级 ACL。
示例里用内存模拟，真实项目把它换成对自身权限表的仓储查询即可
（`GetCodesAsync(userId)` 查权限码、`RemoveSelf/RevokeCode` 是撤销授权的数据改动）。

---

## 场景一：后台管理（操作权限）

**需求**：管理后台的所有操作只有 `admin` 能做（类级）；订单模块的
创建/更新/取消进一步细分到操作；其中「取消」还必须同时持有财务审批码（多码 AND）。

```csharp
// 类级要求：适用于该类型的所有操作
[Permission("admin")]
public sealed class AdminSettings : EditableObject<AdminSettings>
{
    [FactoryUpdate]
    protected override async Task UpdateAsync(CancellationToken cancellationToken = default) { }
}

public sealed class Order : EditableObject<Order>
{
    [FactoryInsert] [Permission("order:create")]
    protected override async Task InsertAsync(CancellationToken cancellationToken = default) { }

    [FactoryUpdate] [Permission("order:update")]
    protected override async Task UpdateAsync(CancellationToken cancellationToken = default) { }

    [FactoryDelete]
    [Permission("order:cancel")]        // ← 两个 [Permission] = AND：两码必须同时持有
    [Permission("finance:approve")]
    protected override async Task DeleteAsync(CancellationToken cancellationToken = default) { }
}
```

授权值来源（解析器从数据实时读取，不写进令牌）：

```csharp
public sealed class MySubjectResolver(IAuthzData authz) : IScopeSubjectResolver
{
    public async ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;
        if (userId == null)
        {
            return ScopeSubjectSet.Empty;                    // 匿名 → 空集合，fail-closed
        }

        return ScopeSubjectSet.CreateBuilder()
                              .AddCodes(await authz.GetCodesAsync(userId, ct))   // ["order:create", ...]
                              .Build();
    }
}
```

判定与结果：

```csharp
// 用户甲（权限码 order:*）：通配通过所有 order: 闸门，但没有 finance:approve
var settings = new AdminSettings { BusinessContext = provider.GetRequiredService<BusinessContext>() };
settings.MarkAsChanged();
await settings.SaveAsync();               // SecurityException（没有 admin）

var order = new Order { BusinessContext = provider.GetRequiredService<BusinessContext>() };
order.MarkAsNew();
await order.SaveAsync();                  // OK（order:* 通过了 order:create 的类型级闸门）

order.MarkAsDeleted();
await order.SaveAsync();                  // SecurityException（order:cancel 与 finance:approve 缺一）
```

要点：

- **类级 + 方法级取并集**：同一个类型上，方法级满足时不需要类级，反之亦然。
- **多 `[Permission]` = AND**（多个码同时要求）；**多角色 = OR**（满足其一即可）；
  码与角色同时出现时，要求“持有码 且 属于某角色”。角色来自声明（`UserPrincipal.IsInRole`）。
- **前缀通配**：持有 `order:*` 可过 `order:create` / `order:update` / `order:cancel` 的
  类型级闸门（`*` 只能作为后缀）。
- **判定不了就失败**：目标没接 `BusinessContext` 时抛 `InvalidOperationException` 并指明，
  不会静默放行；没有任何要求的类型则不受影响。
- 业务方法内可用 `HasPermission(code)` / `HasRole(role)` 做细粒度分支（见场景三的
  `CanAccessRow` 用法）。

---

## 场景二：组织部门树（数据可见范围）

**需求**：`dev` 属于 `TeamA`（含其所有下级团队），因此能看这些团队下的仓库；
同时 `dev` 还负责 `east` 区域，**区域或部门命中其一即可**（跨维度 OR）；
机密仓库不可见；本人创建的始终可见。

```csharp
public sealed class Repo : EditableObject<Repo>
{
    public string OwnerId { get; set; }
    public string TeamId { get; set; }     // 仓库所属团队，取自本行数据列
    public string Region  { get; set; }
    public string Level   { get; set; }

    [FactoryInsert] protected override async Task InsertAsync(CancellationToken ct = default) { }
    [FactoryUpdate] protected override async Task UpdateAsync(CancellationToken ct = default) { }
    [FactoryDelete] protected override async Task DeleteAsync(CancellationToken ct = default) { }
}

public sealed class RepoScope : ScopeModel<Repo>
{
    public override void Define(ScopeModelBuilder<Repo> builder)
    {
        builder.Map(ScopeDimensions.Owner,  x => x.OwnerId)
               .Map(ScopeDimensions.Dept,   x => x.TeamId)
               .Map(ScopeDimensions.Region, x => x.Region)
               .Classify("level", x => x.Level);          // 分类属性：不参与授权
    }

    public override ScopePolicy<Repo> Policy =>
        ScopePolicy<Repo>.Any(                            // 跨维度 OR：命中其一即放行
            ScopePolicy<Repo>.Grant(ScopeDimensions.Dept),
            ScopePolicy<Repo>.Grant(ScopeDimensions.Region));
}
```

解析器：**部门树在解析期展开成扁平集合**，框架只看到扁平 id，判定永远只是
集合成员判断（数据库侧即 `IN (...)`）：

```csharp
public sealed class OrgResolver : IScopeSubjectResolver
{
    private readonly IMembershipStore _members;
    private readonly IOrgTree _tree;

    public async ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;
        if (userId == null)
        {
            return ScopeSubjectSet.Empty;
        }

        var root    = await _members.GetDeptIdAsync(userId, ct);
        var deptIds = await _tree.ExpandWithDescendantsAsync(root, ct);     // TeamA, TeamA-Sub1, …

        return ScopeSubjectSet.CreateBuilder()
                              .AddSelf(userId)                              // 「本人」
                              .AddRange(ScopeDimensions.Dept, deptIds)      // 层级展开为扁平集合
                              .Add(ScopeDimensions.Region, await _members.GetRegionAsync(userId, ct))
                              .Build();
    }
}
```

判定：

```csharp
// 读侧：下推到数据库（返回的仍是 IQueryable，由 EF/提供程序翻成 WHERE）
IQueryable<Repo> visible = guard.Apply(dbContext.Repos);
var list = await visible.ToListAsync();     // select * from repos where dept in (...) or region = 'east'

// 单行判定（与查询过滤共用同一棵表达式，结论必然一致）
guard.Allows(repoInTeamA);                                  // true（部门命中）
guard.Allows(repoInTeamC);                                  // false（部门 & 区域都不命中）
guard.Allows(repoInTeamC, "repo:delete");                   // 指定权限码的单行判定

// 审计：为什么被拒绝
Console.WriteLine(guard.Explain(repoInTeamC));              // 判定：拒绝；成立的允许条件：…

// 写侧：把不属于自己范围的团队填进新行
var stealing = new Repo { TeamId = "TeamC", Level = "normal" };
stealing.BusinessContext = provider.GetRequiredService<BusinessContext>();
stealing.MarkAsNew();
await stealing.SaveAsync();       // SecurityException（工厂边界的 ScopeAuthorization 拦下）
```

**读模型也要单独声明**，否则查询侧不受约束（未声明模型即不拦截）：

```csharp
public sealed record RepoRecord(string RepoId, string TeamId, string Region, string Level);

public sealed class RepoRecordScope : ScopeModel<RepoRecord>
{
    public override void Define(ScopeModelBuilder<RepoRecord> builder)
        => builder.Map(ScopeDimensions.Dept, x => x.TeamId)
                  .Map(ScopeDimensions.Region, x => x.Region);

    public override ScopePolicy<RepoRecord> Policy =>
        ScopePolicy<RepoRecord>.Any(
            ScopePolicy<RepoRecord>.Grant(ScopeDimensions.Dept),
            ScopePolicy<RepoRecord>.Grant(ScopeDimensions.Region));
}
```

要点：

- **层级在解析期展开**保住下推能力——框架不理解层级，只做 `IN` 成员判断。
- 需要「区域 **且** 部门」时用 `All(...)` 而不是 `Any(...)`。
- `guard.Apply` 返回表达式树，千万不要把它烘进 EF 全局查询过滤器（会跨用户泄漏）。

---

## 场景三：行级资源授权（同一类型、不同行权限不同）

**需求**：仓库级 ACL——`a1` 可 push + delete，`a2` 仅可 push，`a3` 都不可；
同一用户、同一类型，**按行**给出不同结论。这就是 `[Permission]` 的码同时充当
**行级策略的键**的用法。

```csharp
public sealed class Repo : EditableObject<Repo>
{
    public string RepoId { get; set; }
    public string TeamId { get; set; }

    [FactoryUpdate] [Permission("repo:push")]
    protected override async Task UpdateAsync(CancellationToken ct = default) { }

    [FactoryDelete] [Permission("repo:delete")]
    protected override async Task DeleteAsync(CancellationToken ct = default) { }
}

public sealed class RepoScope : ScopeModel<Repo>
{
    public override void Define(ScopeModelBuilder<Repo> builder)
    {
        builder.Map("repo", x => x.RepoId)                // 资源标识也作为维度
               .Map(ScopeDimensions.Dept, x => x.TeamId);
    }

    // 默认策略：未单独声明的操作都用它
    public override ScopePolicy<Repo> Policy => ScopePolicy<Repo>.Grant("repo");

    // 按权限码声明各自的行范围
    public override void Declare(ScopePolicySet<Repo> policies)
    {
        policies.For("repo:push",   ScopePolicy<Repo>.Grant("repo"));
        policies.For("repo:delete", ScopePolicy<Repo>.Grant("repo"));
    }
}
```

解析器：类型级码用 `AddCodes`，行级用 `AddGrant`（把 ACL 表读成两个 id 集合）：

```csharp
public sealed class AclResolver(RepoAcl acl) : IScopeSubjectResolver
{
    public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;
        if (userId == null)
        {
            return ValueTask.FromResult(ScopeSubjectSet.Empty);
        }

        var builder = ScopeSubjectSet.CreateBuilder()
                                     .AddCodes(["repo:create", "repo:push", "repo:delete"])   // 类型级闸门
                                     .AddSelf(userId);

        foreach (var operation in new[] { "repo:push", "repo:delete" })
        {
            builder.AddGrant(operation, "repo",
                acl.Entries.Where(e => e.UserId == userId && e.Operation == operation)
                   .Select(e => e.RepoId));              // 这个用户在这枚码下行级可碰的 id
        }

        return ValueTask.FromResult(builder.Build());
    }
}
```

判定与结果（`dev` 的 ACL 里：a1 push/delete，a2 push）：

```csharp
guard.Allows(a2, "repo:push");       // True
guard.Allows(a2, "repo:delete");     // False  ← 同一用户、同一对象类型、不同行的「行级操作权限」

// 读侧：push 视图与 delete 视图各自过滤
var pushable   = await guard.Apply(dbContext.Repos, "repo:push").ToListAsync();     // a1, a2
var deletable  = await guard.Apply(dbContext.Repos, "repo:delete").ToListAsync();   // a1

// 写侧：越权删除被工厂边界拦下 → SecurityException
try
{
    a2.MarkAsDeleted();
    await a2.SaveAsync();
}
catch (SecurityException ex)
{
    // Data scope denied. Delete (before): Repo. [code=repo:delete] …
}

// 越权新增/更新同样走工厂边界 → SecurityException（形态与删除完全一致）
var stealing = await factory.CreateAsync<Repo>("a2");   // Create 只构造、不落库，不判定
stealing.TeamId = "TeamB";                              // 填充后才落库
stealing.MarkAsNew();
await stealing.SaveAsync();                             // SecurityException（数据范围）
```

要点：

- **越权形态一致**：新增、更新、删除、命令执行一律 `SecurityException`。
  早前靠 `ScopePolicyRule` 注入让新增/更新抛 `ValidationException` 的做法已移除
  （见 [PERMISSION-DESIGN §1.2](PERMISSION-DESIGN.md#12-权限与验证是两条线越权一律抛-securityexception)）。
- **`[Permission]` 的码就是行级策略的键**：`DeleteAsync` 上若漏写 `[Permission("repo:delete")]`，
  删除会解析到默认键 `@delete` 并回落到模型的 `Policy`——你在 `Declare` 里为
  `"repo:delete"` 写的行级策略**根本不生效**。
- **码级授予是「覆盖」默认键，不是并集**——否则默认授予会把某枚码上被收窄的行集合重新撑开。
- **通配不发维度**：持 `repo:*` 能过 `repo:push` 的类型级闸门，但不会把
  `(repo:*, repo)` 的行级授予落到 `(repo:push, repo)`——否则给整个命名空间授权会顺带泄漏行级数据。
- ACL 表大时建议授「组 id」而非「行 id」，行数巨大时改用 `Where(x => aclQuery.Contains(x.Id))`。

---

## 场景四：个人数据（`Self()` + 可撤销）

**需求**：工单的管理——用户只能看到**自己创建**的工单；工单的所有者关系
**不是硬编码特例**，管理员可以随时撤销（删掉授权数据即可）。

```csharp
public sealed class Ticket : EditableObject<Ticket>
{
    public string OwnerId { get; set; }
    public string Title   { get; set; }

    [FactoryInsert] protected override async Task InsertAsync(CancellationToken ct = default) { }
    [FactoryUpdate] protected override async Task UpdateAsync(CancellationToken ct = default) { }
}

public sealed class TicketScope : ScopeModel<Ticket>
{
    public override void Define(ScopeModelBuilder<Ticket> builder)
        => builder.Map(ScopeDimensions.Owner, x => x.OwnerId);

    public override ScopePolicy<Ticket> Policy => ScopePolicy<Ticket>.Self();   // = Grant(owner)
}
```

```csharp
public sealed class TicketResolver(IAuthzData authz) : IScopeSubjectResolver
{
    public async ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;
        if (userId == null)
        {
            return ScopeSubjectSet.Empty;
        }

        return ScopeSubjectSet.CreateBuilder()
                              .AddSelf(userId)          // = Add(owner, userId)；漏了即本人也不可见
                              .Build();
    }
}
```

判定与撤销：

```csharp
guard.Allows(devTicket);              // True（本人持有 owner 授予）

// 撤销：授权数据里删掉该用户（管理员操作），不用改代码、不用重签令牌
authz.RemoveSelf("dev");
guard.Refresh();                      // 长作用域需显式失效；新请求自动是新快照
guard.Allows(devTicket);              // False ← 本人也不可见（fail-closed，不会反向放行）
```

要点：

- **`Self()` 并不特殊**，它等价于 `Grant(owner)`；解析器必须 `AddSelf(userId)` 才成立。
  「所有者一定能看自己的数据」不是内置保证——这正是它**可撤销**的原因。
- **缓存生效时机是「下一次解析」**：同一作用域内授权数据变了要 `guard.Refresh()`
  （同步清缓存）或 `await guard.RefreshAsync(ct)`（清缓存并立即重新解析）。
- **并发安全**：首次访问只解析一次；若解析在途时发生撤销 + `Refresh()`，那份「撤销前读到」
  的陈旧结果会被丢弃，不会覆盖失效。

---

## 场景五：机密与公开（`Deny` + 匿名公开）

**需求**：文档系统——**公开文档**匿名用户也能看；`secret` 密级的文档
**任何人（包括作者本人）都不可见**；其余按所有者可见。

```csharp
public sealed class Doc : EditableObject<Doc>
{
    public string OwnerId  { get; set; }
    public bool   IsPublic { get; set; }
    public string Level    { get; set; }

    [FactoryInsert] protected override async Task InsertAsync(CancellationToken ct = default) { }
    [FactoryUpdate] protected override async Task UpdateAsync(CancellationToken ct = default) { }
}

public sealed class DocScope : ScopeModel<Doc>
{
    public override void Define(ScopeModelBuilder<Doc> builder)
    {
        builder.Map(ScopeDimensions.Owner, x => x.OwnerId)
               .Classify("level", x => x.Level);
    }

    public override ScopePolicy<Doc> Policy =>
        ScopePolicy<Doc>.All(
            ScopePolicy<Doc>.Any(
                ScopePolicy<Doc>.Where(x => x.IsPublic),   // ABAC 逃生舱：显式公开
                ScopePolicy<Doc>.Self()),                  // 或本人创建
            ScopePolicy<Doc>.Deny(ScopePolicy<Doc>.Where(x => x.Level == "secret")));
}
```

匿名用户的解析器返回空集合（fail-closed），但 `Where(IsPublic)` 是**显式的允许条件**：

```csharp
public sealed class AnonymousResolver : IScopeSubjectResolver
{
    public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
        => ValueTask.FromResult(ScopeSubjectSet.Empty);
}
```

判定（置匿名用户为当前用户后）：

```csharp
guard.Allows(publicDoc);            // true   —— 空授予下仍由 Where(IsPublic) 显式放行
guard.Allows(secretDoc);            // false  —— 空授予 + Deny
guard.Allows(privateDoc);           // false  —— 非公开、非本人的行不成立
```

再换回 `dev`（本人，持有 owner 授予）：

```csharp
guard.Allows(myOwnDoc);             // true   —— Self() 成立
guard.Allows(mySecretDoc);          // false  ← 作者本人也不行：Deny 压过 Self
```

要点：

- **`Deny` 是「否决」，不是布尔取反**，而且**一律上浮**：策略树任意位置的 `Deny` 作用于
  整个策略，`All(Any(...), Deny(p))` 是「符合任一允许条件 **且** 不命中 p」，不是去重合并。
- **匿名可读不要用专门的特例接口**，用策略里的 `Where(x => x.IsPublic)` 显式表达——
  可审计、可组合。旧的 `IAnonymousAccessible` 已移除。
- 空授予时生成的是**恒假常量**（不会产生空 `IN ()`）；「全部放行」要显式 `Where(_ => true)`。

---

## 场景六：命令对象 + 授权变更实时生效

**需求**：报表导出是命令对象；有 `report:export` 才能执行。取消授权——
只改授权数据，**不重新签发令牌**，下一次解析立即生效。

```csharp
public sealed class ExportReportCommand : CommandObject<ExportReportCommand>
{
    public bool Executed { get; private set; }

    [FactoryExecute]
    [Permission("report:export")]
    protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Executed = true;                          // 命令体
        await Task.CompletedTask;
    }
}
```

```csharp
public sealed class ReportResolver(IAuthzData authz) : IScopeSubjectResolver
{
    public async ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;
        if (userId == null)
        {
            return ScopeSubjectSet.Empty;
        }

        return ScopeSubjectSet.CreateBuilder()
                              .AddCodes(await authz.GetCodesAsync(userId, ct))   // 含 report:export
                              .Build();
    }
}
```

调用：直接经工厂边界执行（命令体由工厂裁决后才跑）：

```csharp
var ctx     = provider.GetRequiredService<BusinessContext>();
var command = new ExportReportCommand { BusinessContext = ctx };

await factory.ExecuteAsync(command, CancellationToken.None);
Console.WriteLine(command.Executed);              // True（持 report:export）

// —— 授权数据变了：把 dev 的 report:export 从库里删掉 ——
authz.RevokeCode("dev", "report:export");
guard.Refresh();                                  // 长作用域显式失效（新请求自动失效）

var again = new ExportReportCommand { BusinessContext = ctx };
try
{
    await factory.ExecuteAsync(again, CancellationToken.None);
}
catch (System.Security.SecurityException)
{
    Console.WriteLine(again.Executed);            // False：命令体没有被执行
}
```

带业务入参的命令走执行器链时，需要**同时提供 `[FactoryCreate]` 与 `[FactoryExecute]`**：

```csharp
public sealed class ExportReportCommand : CommandObject<ExportReportCommand>
{
    public string ReportId { get; private set; }
    public bool   Executed { get; private set; }

    [FactoryCreate]                                 // 执行器先造实例
    private Task CreateAsync(string reportId, CancellationToken ct = default)
    {
        ReportId = reportId;
        return Task.CompletedTask;
    }

    [FactoryExecute]
    [Permission("report:export")]
    protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Executed = true;
        await Task.CompletedTask;
    }
}

await actuator.For<ExportReportCommand>()
              .Execute("R-2024", CancellationToken.None)     // criteria：定位 [FactoryCreate]
              .ExecuteAsync(CancellationToken.None);         // 终结阶段调 [FactoryExecute]
```

要点：

- **裁决发生在命令体之前**：操作权限与数据范围都由工厂边界先判，不满足抛 `SecurityException`
  （命令体不执行）；命令对象的**验证规则**也由工厂边界在命令体之前裁决（`ValidationException`）。
- **撤销 vs Token**：权限码从授权数据解析、按请求缓存。撤销只改数据，下一次解析即生效，
  不需要等旧令牌过期——这是权限码不进令牌的根本原因。
- `IObjectFactory.ExecuteAsync(criteria)` 这类低层入口**不做对象级验证规则判定**
  （规则覆盖的是「保存」与「执行命令」两条路径），**操作权限与数据范围仍然强制**。

---

## 场景七：子表维度（查询我加入的团队）

**需求**：`dev` 能查询**自己已加入**的团队。成员关系存在**子表**里
（`team_member(team_id, user_id, status)`），`expired` 的关系不算成员；团队负责人（行内的列）始终可见。
成员表本身的写入由操作权限把守——本场景只回答「谁能看到哪些团队」。

```csharp
/// 子表行：团队成员（真实系统里就是 team_member 表的一行）
public sealed class TeamMember
{
    public string TeamId { get; set; }
    public string UserId { get; set; }
    public string Status { get; set; }              // active / expired
}

public sealed class Team : EditableObject<Team>
{
    public string Id { get; set; }
    public string LeaderId { get; set; }

    /// 子表行。刻意不初始化：未加载时为空引用，单行判定会明确报错而不是静默拒绝。
    public List<TeamMember> Members { get; set; }

    [FactoryUpdate]
    [Permission("team:edit")]
    protected override async Task UpdateAsync(CancellationToken ct = default) { }
}

public sealed class TeamScope : ScopeModel<Team>
{
    public override void Define(ScopeModelBuilder<Team> builder)
        => builder.Map(ScopeDimensions.Owner, t => t.LeaderId)                    // 行内的列：单值维度
                  .MapMany(ScopeDimensions.Member,                                // 子表：集合维度
                           t => t.Members.Where(m => m.Status == "active")
                                         .Select(m => m.UserId));

    public override ScopePolicy<Team> Policy =>
        ScopePolicy<Team>.Any(ScopePolicy<Team>.Grant(ScopeDimensions.Member),
                              ScopePolicy<Team>.Grant(ScopeDimensions.Owner));
}
```

解析器：**不查关系表**——授予的值是「子表里应当出现的值」，也就是当前用户标识：

```csharp
public sealed class TeamSubjectResolver(IAuthzData authz) : IScopeSubjectResolver
{
    public async ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        var userId = user?.FindFirst(UserClaimTypes.Subject)?.Value;
        if (userId == null)
        {
            return ScopeSubjectSet.Empty;                          // 匿名 → 空集合，fail-closed
        }

        return ScopeSubjectSet.CreateBuilder()
                              .AddCodes(await authz.GetCodesAsync(userId, ct))
                              .AddSelf(userId)                         // 负责人 / 所有者（owner 维度）
                              .Add(ScopeDimensions.Member, userId)     // 团队成员（子表维度）
                              .Build();
    }
}
```

判定与结果（`dev` 在 `t1` 的关系有效、在 `t2` 已失效，`t3` 与 `dev` 无关）：

```csharp
// 读侧：下推成 EXISTS 相关子查询——关系表多大都与解析成本无关
var visible = await guard.Apply(dbContext.Teams).ToListAsync();
// select * from team
// where exists (select 1 from team_member
//               where team_id = team.id and status = 'active' and user_id = 'dev')
//    or leader_id = 'dev'
// → t1

// 单行判定：要求实例上子集合已加载（真实系统里由仓储 Include）
guard.Allows(await LoadWithMembersAsync("t1"));      // True
guard.Allows(await LoadWithMembersAsync("t3"));      // False

// 子表是实时求值的：删掉 t1 里 dev 的那行关系，下一次判定即变，不需要 Refresh()
membership.Leave("t1", "dev");
guard.Allows(await LoadWithMembersAsync("t1"));      // False

// 写侧（工厂边界）：仓储没加载子表时——判定不了就失败，绝不是「越权」
var team = await LoadAsync("t1");                    // 只取 team 行，Members 为 null
team.BusinessContext = provider.GetRequiredService<BusinessContext>();
team.MarkAsChanged();
await team.SaveAsync();                              // InvalidOperationException：
// 数据权限判定失败：资源类型 'Team' 的子表维度 'member'（取值来源：x.Members）在单行判定时不可用——
// 对应的子集合未加载（为空引用）。…修法：① 读侧改用 IScopeGuard.Apply 下推；② 加载对象时一并
// 加载该子集合（例如 EF Core 的 Include）；③ 去掉集合属性的初始化器…
```

要点：

- **授予的值是「子表里应当出现的值」，不是资源标识**：成员维度授 `userId`，团队维度才授 `teamId`。
  方向授反的后果是恒不放行（fail-closed，不会泄漏），但极难排查——`guard.Explain` 会把该叶子显示为
  `Grant(member[])`，先核对这一点。
- **解析器不需要反向展开**：关系表有多大都与解析成本无关（对比场景二的部门树展开）。
- **子表属性（`status`）由数据库实时求值**：它写在选择器里，而不是在解析期过滤成快照；
  成员关系一改，下一次查询即生效，**不需要 `Refresh()`**。
- **单行判定要求子集合已加载**：`Allows` 与工厂边界在内存中求值同一棵表达式，未加载时抛
  `InvalidOperationException`（**不是** `SecurityException`，别混捕）；实体把集合初始化成 `= []` 时会
  退化成**静默拒绝**（已知边界，见 DESIGN §2.5）。
- **取值形状只有一种**：「导航集合（可带 `Where` 过滤）再取字符串值」，其余形状在**启动期**被拒绝。
- **成员表的写入口必须由操作权限把守**：子表维度把「谁属于这个团队」的判定权交给了业务数据，
  给自己加一行就等于给自己授权。

> 完整语义、边界，以及与「解析器反向展开」的取舍见
> [`Euonia.Security/README.md` §5.9](../Euonia.Security/README.md) 与
> [`DESIGN.md` §1.9](../Euonia.Security/DESIGN.md)；
> 本场景的行为由 `SubTableDimensionTests`、`CollectionDimensionTests`、`EfCoreCollectionDimensionTests` 钉住。

---

## 7. 组合使用：操作权限 × 数据权限

两套权限回答不同问题，通常**同时启用**：场景三就是典型——`[Permission("repo:delete")]`
管“用户能不能做删除”，`Declare("repo:delete", Grant("repo"))` 管“能删哪些行”。

业务方法内可以在**操作权限闸门之外**再做条件分支：

```csharp
protected async Task ArchiveAsync(CancellationToken cancellationToken)
{
    if (!CanAccessRow("repo:delete"))                       // 本行在不在该码的范围内
    {
        throw new InvalidOperationException("无权归档该仓库。");
    }

    if (!await CheckPermissionAsync("repo:admin", cancellationToken))
    {
        throw new InvalidOperationException("需要 repo:admin 权限。");
    }
}
```

**不要把权限断言写进 `AddRules()`**。规则属于验证线，失败形态是 `ValidationException`，
且可被 `SuspendRuleChecking()` / `BypassRuleChecks()` 绕过——用它做授权等于留了一条旁路。
需要「先查再跳」的分支，用上面 `CanAccessRow` / `CheckPermissionAsync`；
需要「越权即拒」，交给工厂边界即可。

写侧失败形态小结（**只有一条强制线**）：

| 路径 | 结果 |
|---|---|
| 越权新增 / 更新（`SaveAsync`） | `SecurityException`（工厂边界前置或后置判定） |
| 越权删除 | `SecurityException`（删除跳过的只是验证规则） |
| 越权命令执行 | `SecurityException`（命令体不执行） |
| 判定不了（缺 `BusinessContext` / 缺检查器） | `InvalidOperationException`（绝不静默放行） |
| 数据不合法（与权限无关） | `ValidationException`（`Errors` 带属性名） |

---

## 8. 怎么选场景

| 需求 | 去场景 |
|---|---|
| 只有少量稳定的权限形态，主要是管理员/运营区分 | [场景一](#场景一后台管理操作权限) |
| 页面/行按组织、区域、项目等维度过滤 | [场景二](#场景二组织部门树数据可见范围) |
| 同一资源行与行之间权限不同（ACL） | [场景三](#场景三行级资源授权同一类型不同行权限不同) |
| 仅看自己创建的数据，且所有权可被收回 | [场景四](#场景四个人数据self--可撤销) |
| 有公开、机密等密级，需要 deny 与匿名 | [场景五](#场景五机密与公开deny--匿名公开) |
| 操作本身是命令（导出、推送、结算），要权限守卫 | [场景六](#场景六命令对象--授权变更实时生效) |
| 可见性由子表（成员表 / 关系表）决定，如「我加入的团队」 | [场景七](#场景七子表维度查询我加入的团队) |
| 对象既按码判操作、又按行判范围 | [场景三](#场景三行级资源授权同一类型不同行权限不同) + [§7](#7-组合使用操作权限--数据权限) |

> 两个心智模型：
> - **能预定义的进代码**（操作类型、维度、判定语义），**不能预定义的走数据**
>   （用户属于哪些团队、能访问哪些仓库）。
> - 操作权限回答「能不能做这个操作」，数据权限回答「能碰到哪些行」，二者不可互相替代。