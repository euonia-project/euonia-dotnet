# Euonia.Osba 权限控制使用说明

`Euonia.Osba` 提供两套相辅相成的权限控制：

| | 操作权限（Operation Permission） | 数据权限（Data Permission） |
|---|---|---|
| 回答的问题 | 当前用户**能否执行某项操作** | 当前用户**能看到/操作哪些数据行** |
| 作用对象 | 操作 × 对象类型（类级/方法级） | 数据行（实现 `IDataScoped` 的类型） |
| 判定依据 | 权限声明（或角色），一般来自用户声明 | 行所属范围 × 用户从**授权数据**实时解析出的范围 |
| 强制执行点 | `BusinessObjectFactory` 调用边界 | 查询过滤 + `DataScopeRule`（写入前） |
| 失败形态 | 抛 `System.Security.SecurityException` | 排除该行 / 规则失败 |

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
- `IDataScopeService` → `DataScopeService`（数据范围判定引擎）

若使用数据权限，还必须**由应用注册一个 `IUserScopeProvider`**（见 [4 数据权限](#4-数据权限)），
框架不提供默认实现，以免把授权值固化。

使用时机说明：`BusinessContext` 在构造时会捕获当前用户
（来自 `BusinessContextAccessor.Current`，AsyncLocal）。请确保查询/请求开始时先设置：
`BusinessContextAccessor.SetCurrent(scope.ServiceProvider)`（依赖注入下由中间件/工厂生周期自动完成）。

---

## 2. 操作权限

### 2.1 声明权限点

通过 `[PermissionRequirement]` 声明，可打在**类型**（适用于全部操作）或**工厂方法**上
（仅对应操作生效），两者取并集；可用多次（多权限 = AND，多角色 = OR）。

```csharp
// 方法级：仅插入要求 order:create
public class Order : EditableObject<Order>
{
    [FactoryInsert]
    [PermissionRequirement("order:create")]
    protected override async Task InsertAsync(CancellationToken cancellationToken = default)
    {
        // ...
    }

    [FactoryUpdate]
    [PermissionRequirement("order:update")]
    protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        // ...
    }
}

// 类级：全部操作都要求 admin
[PermissionRequirement("admin")]
public class AdminSettings : EditableObject<AdminSettings>
{
    // ...
}
```

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

- 没有任何 `[PermissionRequirement]` → 放行
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

### 3.1 模型

```csharp
// 一个"维度-值"对，值通常为数据库标识，框架不解读
public record ScopeTag(string Dimension, string Value);

// 数据行自身声明归属：OwnerId + 范围标签
public interface IDataScoped
{
    string OwnerId { get; }
    IReadOnlyList<ScopeTag> ScopeTags { get; }
}
```

行范围值应**直接来自该行自身的数据列**，是数据而非固化标签，例如：

```csharp
public class Repo : EditableObject<Repo>, IDataScoped
{
    public string OwnerId { get; set; }
    public string TeamId { get; set; }   // 仓库的团队列

    public IReadOnlyList<ScopeTag> ScopeTags => [new ScopeTag("team", TeamId)];
}
```

### 3.2 用户侧：授权值从数据实时解析

值来源由 `IUserScopeProvider` 提供，**必须由应用实现**。典型实现是查询授权关系表：

```csharp
public class TeamScopeProvider : IUserScopeProvider
{
    private readonly TeamMemberRepository _members;
    public TeamScopeProvider(TeamMemberRepository members) => _members = members;

    public IReadOnlyList<ScopeTag> ResolveScopes(UserPrincipal user)
    {
        // 每次判定实时查询：Dev 被加入/移出团队，立即生效
        return _members.Query(user.UserId)
                       .Select(m => new ScopeTag("team", m.TeamId))
                       .ToArray();
    }
}

services.AddSingleton<IUserScopeProvider, TeamScopeProvider>();
```

> 警告：框架自带一个 `ClaimsUserScopeProvider`（从 `scope:{维度}` 声明解析）仅供特殊场景参考。
> 声明固定在 Token 里，无法反映数据变化，**不要**把它当作数据权限的默认来源。

### 3.3 判定引擎：`IDataScopeService`

```csharp
// 单行判定
bool can = service.CanAccess(repo);

// 列表过滤（查询层）
IEnumerable<Repo> visible = service.Filter(repos);

// 获取谓词，直接交给查询
Func<Repo, bool> predicate = service.CreateScopePredicate<Repo>();
IEnumerable<Repo> visible2 = repos.Where(predicate);

// 解析用户当前范围（用于自己拼 DB 过滤，如 TeamId IN (解析结果)）
IReadOnlyList<ScopeTag> granted = service.ResolveScopes();

// 单个标签判断
bool granted3 = service.IsGranted(new ScopeTag("team", "TeamA"));
```

### 3.4 匹配语义

- **跨维度 AND**：行声明 `region` 与 `team` 两个维度时，两个维度都必须满足
- **同维度 OR**：用户同维度多值、或行同维度多标签，满足任意一个即可
- **本人快速路径**：`OwnerId == 当前用户 UserId`（忽略大小写）→ 直接放行
- **通配**：值 `"*"` 匹配该维度任意值；`ScopeTag.Any`（`"*"/"*"`）全局通配
- **边界**：`CanAccess(null)` → 拒绝；未认证用户 → 全放行；
  行无标签且无所有者 → 放行；行无标签但有所有者 → 仅本人

### 3.5 写入前拦截：`DataScopeRule`

把数据权限融入规则体系：保存前校验目标行是否在当前用户范围内，越权数据使规则失败、阻止落库。

```csharp
public class Repo : EditableObject<Repo>, IDataScoped
{
    // ...
    protected override void AddRules()
    {
        Rules.AddRule(new DataScopeRule());
        // 可与其它业务规则并存
    }
}
```

- 对象未实现 `IDataScoped`、或数据范围服务不可用 → 规则自动放行
- 该规则与查询过滤互补：查询时排除越权行，写入时阻止越权数据

---

## 4. 场景示例：Dev / TeamA / TeamB / Repo

需求：`Dev` 属于 `TeamA`、`TeamB`，可访问两团队下的仓库；不在 `TeamC`，无法访问其仓库；
仓库的操作类型（read / push / create_branch / create_pr）代码预定义，但"哪些仓库可访问"是数据。

### 4.1 数据行

```csharp
public class Repo : EditableObject<Repo>, IDataScoped
{
    public string OwnerId { get; set; }
    public string TeamId { get; set; }

    public IReadOnlyList<ScopeTag> ScopeTags => [new ScopeTag("team", TeamId)];

    [FactoryInsert]
    [PermissionRequirement("repo:create")]
    protected override async Task InsertAsync(CancellationToken cancellationToken = default)
    {
        // 写入前规则校验：Dev 不能把仓库建到 TeamC
    }

    protected override void AddRules()
    {
        Rules.AddRule(new DataScopeRule());
    }
}
```

### 4.2 数据权限值来源（查成员关系表）

```csharp
public class TeamScopeProvider : IUserScopeProvider
{
    private readonly ITeamMemberRepository _members;
    public TeamScopeProvider(ITeamMemberRepository members) => _members = members;

    public IReadOnlyList<ScopeTag> ResolveScopes(UserPrincipal user)
    {
        return _members.GetTeams(user.UserId).Select(t => new ScopeTag("team", t)).ToArray();
    }
}
```

`Dev` 的解析结果即为 `[("team", TeamA), ("team", TeamB)]`。此时：

- `CanAccess(repo(TeamA))`、`CanAccess(repo(TeamB))` → `true`
- `CanAccess(repo(TeamC))` → `false`
- 把 `Dev` 加入 `TeamC`（只改数据表）→ **下一次判定立即** `true`，无需改 Token、无需改代码
- `Repo.TeamId` 改到 `TeamC`（只改数据列）→ 立即拒访

仓库级 ACL（`RepoA1 = full`、`RepoA2 = push + create_branch + create_pr`）同样是数据：
把"操作类型（代码预定义）"绑定到"具体仓库 ID（数据）"，由应用在授权数据里维护，
在操作判定时组合使用，与上述机制无关。

---

## 5. 最佳实践

1. **值别进 Token/代码**：所有可能变化的授权值（团队、仓库、区域……）都从数据解析。
2. **`IUserScopeProvider` 保持实时**：每次判定查询（或使用可失效缓存），不要长缓存。
3. **组合判定**：操作权限管"能不能做这个操作"，数据权限管"能碰到哪些行"，
   `DataScopeRule` 保证落库前再次拦截，二者分工明确，不要互相替代。
4. **维度命名约定**：同一维度（如 `team`、`region`）在行数据与用户侧解析中保持一致。
5. **未注册 `IUserScopeProvider` 时**：`IDataScopeService` 依赖注入激活会失败——
   这是有意的 fail-fast，防止"看似开了数据权限、实际没生效"。

## 6. 类型速查

| 类型 | 位置 | 用途 |
|---|---|---|
| `BusinessOperation` | `Permission/` | 操作类型枚举（Read/Create/Update/Delete/Execute） |
| `PermissionRequirementAttribute` | `Permission/` | 声明操作权限点（类级/方法级） |
| `IPermissionChecker` | `Permission/` | 权限判断抽象 |
| `ClaimPermissionChecker` | `Permission/` | 默认实现（读 `"perm"` 声明，支持 `*` 前缀通配） |
| `ScopeTag` | `Permission/` | 维度-值范围标签 |
| `IDataScoped` | `Permission/` | 数据行声明归属 |
| `IUserScopeProvider` | `Permission/` | 用户范围值来源（应用实现，数据实时解析） |
| `IDataScopeService` | `Permission/` | 数据范围判定引擎 |
| `DataScopeService` | `Permission/` | `IDataScopeService` 默认实现 |
| `DataScopeRule` | `Permission/` | 数据权限的规则化（写入前拦截） |
| `ClaimsUserScopeProvider` | `Permission/` | 可选：从声明解析范围（避免使用） |
| `UserClaimTypes.Permission` | `Euonia.Core` | 权限声明类型（`"perm"`） |
| `UserClaimTypes.ScopePrefix` | `Euonia.Core` | 范围声明前缀（`"scope:"`，供 `ClaimsUserScopeProvider` 使用） |