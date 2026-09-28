# Euonia.Security

数据权限（行级）与操作权限的**策略引擎**。本库只依赖 `Euonia.Core`，不认识任何对象模型：
它定义策略、编译策略、判定与下推，但**不决定强制执行点在哪里**。

> 设计动因、被否决的方案与已知边界见 [DESIGN.md](DESIGN.md)（面向维护者与评审者）。

---

## 1. 核心原则：能预定义的进代码，不能预定义的走数据

判定语义、维度、策略组合方式都可以预定义，因此进代码；而「用户属于哪些团队、能访问哪些仓库」
这类**授权值**随时可变（团队/资源会新建删除、人员会调整），必须由使用方从应用数据实时解析。

这条原则直接决定了两件事：

- **框架不提供默认授权数据源**。把授权值固化在声明、Token 或代码字面量里，会导致
  「取消授权后旧令牌仍然有效」。因此本库只定义 `IScopeSubjectResolver` 契约，不给实现。
- **判定一律 fail-closed**。取不到授权数据时拒绝，而不是放行。

| | 操作权限（Operation Permission） | 数据权限（Data Permission） |
|---|---|---|
| 回答的问题 | 当前用户**能否执行某项操作** | 当前用户**能看到/操作哪些数据行** |
| 作用对象 | 操作 × 类型（类级/方法级） | 声明了 `ScopeModel<T>` 的资源类型 |
| 判定依据 | 权限声明（或角色），一般来自用户声明 | 资源属性 × 用户从**授权数据**实时解析出的主体集合 |
| 引擎出口 | `IPermissionChecker` | `IScopeGuard` |
| 失败形态 | 断言失败 / 由调用方裁决 | 判定为不可访问 |

---

## 2. 需要使用方提供的两处映射

引擎有两处无从判断、必须由使用方回答的问题，因此把它们定义成接口。**脱离宿主框架单独使用时，
两者都可缺席**，此时行为已在各节标注，且一律 fail-closed。

| 接口 | 回答的问题 | 缺席时的行为 |
|---|---|---|
| `IPermissionCodeSource` | 「哪个方法对应哪个业务操作」 | 扫不到方法级权限码，故不存在方法级声明；写侧仍由调用方显式传入 |
| `IScopeKeyResolver` | 「这个资源实例当前代表哪个操作」 | 未显式指定权限码的判定回落到 `ScopeKeys.Default` |

判定主体直接取 `UserPrincipal`（其 `Claims` 即 `ClaimsPrincipal`），由宿主在容器中注册，
不再另立一层用户抽象：角色、认证状态、声明都能从它直接取到，多包一层反而要多处转换。

两者的存在是为了让引擎不必认识使用方的类型体系——**引擎不认识的东西，使用方自己回答**，
而不是让引擎去猜。

---

## 3. 装配

```csharp
var services = new ServiceCollection();

// 注册策略引擎：数据权限模型注册表（注册期即完成全部校验）、
// IPermissionChecker、IScopeGuard、PermissionSetup
services.AddPermission(EmptyCodeSource.Instance, typeof(Order).Assembly);

// 当前用户主体（判定主体）：由宿主注册，取其 Claims 即 ClaimsPrincipal
services.AddSingleton(UserPrincipal.Current);
```

第二个参数 `IPermissionCodeSource` 是**必填项**，见 [3.1](#31-为什么必须提供-ipermissioncodesource)。

`IPermissionCodeSource` 与 `IScopeKeyResolver` 都是 `TryAdd` 语义：宿主已注册的实现不会被覆盖。

若使用了权限（操作权限的权限码或数据权限），还必须**由应用注册一个 `IScopeSubjectResolver`**
（见 [4.2](#42-用户侧授权值从数据实时解析)）。框架不提供默认实现，以免把授权值固化。

容器构建后请调用一次启动期校验，使「声明了权限却忘了接解析器」在启动时失败：

```csharp
var provider = services.BuildServiceProvider();
provider.ValidatePermissionSetup();   // 缺少 IScopeSubjectResolver 或 UserPrincipal 时在此抛出
```

### 3.1 为什么必须提供 `IPermissionCodeSource`

方法级 `[Permission]` 写在方法上，而「哪个方法对应哪个业务操作」取决于使用方的约定——
有的框架用特性标记工厂方法，有的靠命名约定，引擎无从推断。因此 `IPermissionCodeSource` 没有默认实现。

应用确实**不使用方法级**权限码时，传入 `EmptyCodeSource.Instance`。这是一个显式的断言，
不是「忘了提供来源」的默认值。

代价是：模型里的按码声明（`ScopePolicySet<T>.For("code", …)`）与 `EmptyCodeSource` 不可同用。
因为没有任何操作能解析到应用自定义的码，注册期死策略校验会拒绝启动：

```
权限模型 'OrderModel' 为权限码 'order:edit' 声明了行级策略，
但没有任何操作会解析到该码（请核对方法上 [Permission] 的码与 Declare 里的码是否一致）。
```

这正是期望行为——**声明了按码策略就说明存在方法级权限码**，那就必须给出方法与操作的对应关系。

### 3.2 多个模块各自注册（按并集合并）

`AddPermission` **可以调用多次，每次的贡献都会被合并**：

```csharp
services.AddPermission(new OrderCodeSource(),  typeof(Order).Assembly);   // 订单模块
services.AddPermission(new ReportCodeSource(), typeof(Report).Assembly);  // 报表模块
```

合并规则：权限码来源合成一个（操作取并集，权限码取并集去重），程序集取并集，
最终只构建**一个** `ScopeModelRegistry`，所有模块的模型与权限码都在里面生效。

这不是可有可无的宽松设计。若采用「先到先得」，那么先注册的模块就决定了全局的码来源，
后注册模块的权限码会被**静默丢弃**——它对应的行级策略因此永远不会被解析到，
判定反而比作者本意**更宽松**。这类失败没有任何报错，只在生产环境表现为「权限没拦住」。

> 同一个 `IPermissionCodeSource` 实例被重复传入时按幂等处理（跳过），
> 因此 `EmptyCodeSource.Instance` 这样的共享单例可以被每个模块放心共用。
> 同一个程序集被多个模块传入同样只扫一次。

注意 `IScopeKeyResolver` 仍是 `TryAdd` 语义（**先到先得**）：它决定「某个资源实例当前代表哪个操作」，
属于**全局**语义，多个模块同时给出不同答案本身就是配置错误。需要按模块区分时，
请自行实现一个带分派的 `IScopeKeyResolver`。

### 3.3 用规则描述「哪个方法对应哪个操作」

绝大多数宿主的答案是同一种形状：入口方法要么打了某个特性，要么叫某个名字。
`OperationCodeSource` 把这件事写成**数据**而不是代码，换框架只需换一组规则：

```csharp
var codeSource = OperationCodeSource.Create()
    .OnAttributeOrName(BusinessOperation.Read,   "Order", typeof(FetchAttribute))
    .OnAttributeOrName(BusinessOperation.Create, "Order", typeof(CreateAttribute))
    .OnAttributeOrName(BusinessOperation.Update, "Order", typeof(UpdateAttribute))
    .OnAttributeOrName(BusinessOperation.Delete, "Order", typeof(DeleteAttribute))
    .Build();

services.AddPermission(codeSource, typeof(Order).Assembly);
```

第二个参数是**要从特性名里剥离的类型前缀**（`OrderFetchAttribute` → 候选名 `Fetch` / `FetchAsync` /
`OrderFetch` / `OrderFetchAsync`）。只按特性（`OnAttribute`）、只按名字（`OnMethodName`，可传多个）
或任意自定义谓词（`OnMethod`）也都可以；候选名也可由 `OperationConventions.Names` 自行推导。

它只**收集**方法上已有的 `[Permission]`，不生成权限码——**码由应用自己写在特性上**，
推导出来的码会与应用真正的鉴权口径悄悄分叉。

**业务操作不是固定枚举。** `BusinessOperation` 只是 `read` / `create` / `update` / `delete` / `execute`
的字符串常量集合，宿主可定义任意操作（如审批流里的 `approve`、`order:archive`）：

```csharp
const string approve = "approve";

var codeSource = OperationCodeSource.Create()
    .OnAttributeOrName(approve, "Order", typeof(ApproveAttribute))
    .Build();
```

每个操作的默认策略键由 `ScopeKeys.For(operation)` 派生为 `@<operation>`；操作名不得以保留前缀
`@` 开头。判定的其余部分与操作集合无关。

---

## 4. 操作权限

### 4.1 声明权限点

`[Permission]` 可打在**类型**上（适用于该类型支持的全部操作）或**方法**上
（仅当该方法被执行时生效），两者取并集；可用多次（多权限 = AND，多角色 = OR）。

```csharp
[Permission("order:read")]
[Permission("order:write", Roles = ["order-admin"])]
public class Order
{
    [Permission("order:cancel")]
    public void Cancel() { /* ... */ }
}
```

未指定 `Permission` 时仅校验角色，未指定角色时仅校验权限；两者均未指定则视为放行。

### 4.2 用户侧：授权值从数据实时解析

`IScopeSubjectResolver` 是本库与授权数据之间**唯一**的接口，实现由使用方提供：

```csharp
public sealed class MySubjectResolver : IScopeSubjectResolver
{
    public async ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal user, CancellationToken ct = default)
        => ScopeSubjectSet.CreateBuilder()
                          .AddCodes(await GetPermissionCodesAsync(user, ct))   // 权限码（类型级/方法级）
                          .AddSelf(userId)                                     // 本人
                          .AddGrant("order:delete", "order", await GetDeletableOrdersAsync(user, ct))
                          .Build();
}
```

`AddSelf` / `AddGrant` 对应策略里的 `Self()` / `Grant(...)` 维度；授予的**资源标识**应与
模型里 `Map` 的取值选择器同源，否则维度永不匹配。

### 4.3 判定

`IPermissionChecker` 提供按权限码/角色断言的入口。它只回答「能不能」，**不负责在哪里调用**——
本库刻意不拦截任何调用，因为拦截点属于使用方的类型体系：把调用点写进引擎会让引擎无法独立使用。
需要「越权一律同一种异常形态」时，由使用方在自己的强制点上统一裁决。

```csharp
var checker = provider.GetRequiredService<IPermissionChecker>();
checker.EnsurePermission("order:cancel");
```

---

## 5. 数据权限

### 5.1 核心不变式：单一真值来源

策略被编译成 `Allow` / `Deny` **一对**表达式，**读侧下推**与**单行判定**共用同一份编译结果。
这是刻意的：若两条路径各写一遍判定，它们迟早会漂移，而「下推放行、单行拒绝」这类不一致极难排查。

```csharp
var visible = guard.Apply(dbContext.Orders);   // 表达式下推到数据库
guard.Allows(order, "order:delete");           // 单行判定，与上面必然同答案
```

`Apply` 与 `Allows` 的区别只在于「判定的是集合还是单个对象」，不在于判定规则。

### 5.2 资源侧：模型即声明

模型与策略写在同一个声明类型里，不允许拆开——拆开后就会出现「模型写了、策略没写」的静默放行。

```csharp
public sealed class OrderScope : ScopeModel<Order>
{
    public override void Define(ScopeModelBuilder<Order> builder)
        => builder.Map(ScopeDimensions.Dept, x => x.DeptId)
                  .Map("order", x => x.Id);

    public override ScopePolicy<Order> Policy => ScopePolicy<Order>.Grant(ScopeDimensions.Dept);

    public override void Declare(ScopePolicySet<Order> policies)
        => policies.For("order:delete", ScopePolicy<Order>.Grant("order"));
}
```

模型在程序集扫描时自动发现，并在启动期校验：同一资源类型存在多个模型、模型不可实例化、
未声明任何维度、策略引用了未映射的维度、同一操作解析出多个有策略的权限码、
声明了策略却没有任何操作解析到该码（死策略）——都会导致启动失败。

### 5.3 匹配语义与允许/拒绝代数

最终结论是 `Allow && !Deny`：

- `HasAllow` 不可省。只有 `Deny` 而没有 `Allow` 时**不是**「全放行」，而是「全拒绝」——
  否则 `Deny` 会被当成摆设。
- `Deny` 一律上浮，且在嵌套 `Any` 里同样优先。`Allow.Any(x => Deny(x).Or(...))` 仍是拒绝。
- 空授权数据生成常量 `false`，不会生成空的 `IN ()`。

### 5.4 用户身份与放行规则

| 条件 | 结果 |
|---|---|
| 策略为空 / 无维度 | 不受数据权限约束（放行） |
| 用户未认证（含无用户主体来源） | 拒绝——授权数据一律视为空，不调用解析器 |
| 解析器缺席且已声明模型或权限码 | 拒绝，并抛出明确错误——绝不静默放行 |
| 资源类型未注册模型 | 不受数据权限约束（放行） |

### 5.5 缓存契约

`IScopeGuard` 按请求（Scoped）注册，授权数据与已编译策略**在一次请求内只解析/编译一次**，
读写共用同一份快照。这既保证同一请求内多处判定一致，也避免逐行查询授权数据。

因此**撤销授权的生效时机是「下一次解析」**：同一请求内需显式 `Refresh()`。在长生命周期作用域
（例如后台 worker）中使用时，授权数据变化后必须自行调用 `Refresh()`。

`Refresh()` 的代数语义保证：在途解析若发现代数已变，其结果会被丢弃，不会把新结果回滚掉。

### 5.6 启动期校验

权限模型在**注册期**校验，只校验「声明了模型」的情况。没有任何 `IScopeModel<T>` 的应用照常启动，
只是全部资源都不受数据权限约束。

所有问题**一次报全**（`ScopeModelValidationException.Diagnostics`），而不是修一个跑一次——
启动期配置错误往往同时存在多处，逐个报出等于让人反复重启。

授权数据源与用户主体是否齐备由 `provider.ValidatePermissionSetup()` 检查（必须在容器构建**之后**调用，
因为注册顺序不受约束）。若遗漏该调用，首次判定时同样会以明确错误暴露。

### 5.7 模型的注册方式

程序集扫描是默认路径（`AddPermission(codeSource, assemblies)`）。模型也可以**程序化注册**，
适合模型需要构造参数、或按配置动态生成的宿主：

```csharp
// 需要构造参数 ⇒ 用实例
new ScopeModelRegistryBuilder()
    .Add(new TenantModel(tenantId, departmentId))
    .AddFrom(typeof(Order).Assembly)      // 两者可混用
    .Build(codeSource);
```

两条路径走**同一套校验**（`Build` 是唯一校验入口），因此不存在「扫描进来的查得严、手动注册的查得松」。
同一资源类型注册两个模型是**配置错误**（会在 `Build` 报出），而不是「后者胜出」——后者会让作者
误以为先注册的那个生效了。

---

## 6. 关键成员速查

### 授权数据（使用方实现）

| 成员 | 用途 |
|---|---|
| `IScopeSubjectResolver` | 实时解析当前用户的权限码与行级授予（入参为 `ClaimsPrincipal`） |
| `ScopeSubjectSet` | 授权数据快照（不可变） |
| `ScopeSubjectSetBuilder` | 构造授权数据：`AddCodes` / `AddSelf` / `AddGrant` |

### 资源声明（使用方编写）

| 成员 | 用途 |
|---|---|
| `ScopeModel<T>` | 数据权限模型基类，模型与策略写在一起 |
| `ScopeModelBuilder<T>` | 声明维度取值与分类属性 |
| `ScopeModelRegistryBuilder` | 程序化注册模型（实例 / 类型 / 程序集） |
| `OperationCodeSource` | 通用的「方法 → 业务操作」规则化来源（按特性或命名约定） |
| `ScopePolicy<T>` | 策略组合：`Grant` / `Deny` / `Self` / `Any` / `All` / `Not` |
| `ScopePolicySet<T>` | 按权限码声明行级策略 |
| `ScopeDimensions` | 预置维度（`Self` / `Dept` / …） |
| `ScopeKeys` | 保留键（`@default` / `@read` / `@create` / …）与键解析 |
| `PermissionAttribute` | 声明操作权限点（类型级或方法级） |

### 判定入口

| 成员 | 用途 |
|---|---|
| `IScopeGuard` | 数据权限判定与下推（按请求缓存） |
| `IScopeGuard.Apply` | 把数据权限下推为 `IQueryable` 过滤条件 |
| `IScopeGuard.Allows` / `AllowsObject` | 单行判定 |
| `IScopeGuard.Explain` / `ExplainObject` | 审计：给出命中路径与拒绝原因 |
| `IPermissionChecker` | 操作权限与角色断言 |
| `ScopeDecision` | 判定结论与命中路径 |

### 扩展点

| 成员 | 用途 |
|---|---|
| `IPermissionCodeSource` | 提供「某类型在某操作上声明了哪些权限码」，用于注册期校验 |
| `IScopeKeyResolver` | 把资源实例解析为策略键 |
| `ScopeKeyResolver.Resolve` | 由「注册项 + 操作 + 权限码来源」解析策略键（唯一出口） |
| `ScopeModelRegistryBuilder` | 换掉程序集扫描，改为程序化注册 |
| `OperationCodeSource` / `OperationConventions` | 换掉权限码来源的识别规则 |

---

## 7. 故障排查

### 启动期异常（都是**有意**的 fail-fast）

| 现象 | 原因 |
|---|---|
| 「同一操作解析出多个声明了行级策略的权限码」 | 同一操作上多个权限码都声明了策略，实际生效哪一个不允许靠猜 |
| 「声明了权限码但没有任何操作会解析到该码」 | `Declare` 里的码与方法上 `[Permission]` 的码对不上，行级策略会静默失效 |
| 「策略引用了未映射的维度」 | 策略引用的维度没有在 `Define` 里 `Map`——这是「写了却没映射 ⇒ 静默放行」的根治点 |
| 「未注册 IScopeSubjectResolver」 | 声明了模型或权限码却没接授权数据源 |
| 「同一资源类型存在多个模型」 | 一个类型只能有一个模型（程序化注册同样参与检查） |
| 「未注册 UserPrincipal」 | 判定主体取自 `UserPrincipal`；不注册则取用 `IScopeGuard` 直接失败 |
| 「权限模型注册期校验失败，共 N 处问题」 | 这是**汇总**异常，`Diagnostics` 列出了全部问题，按序号逐条修 |

### 判定结果不符合预期

1. 先看 `guard.Explain(entity, code)`：它会给出命中的维度与最终结论。
2. 再确认**授权值的形状**与维度选择器同源：授予的 `AddGrant(code, "order", ids)` 中的 `ids`
   与 `Map("order", x => x.Id)` 取到的必须是同一套值。
3. 再确认用的是**同一个权限码**：默认键与 `policies.For(code, ...)` 声明的键不是同一套策略，
   参见 [DESIGN §1.6](DESIGN.md)。

### 下推相关问题

- `Apply` 必须下推为**表达式**（`Where` 带 `Expression`），而不是先物化再过滤——
  后者会把全表拉进内存。可以用 `ToQueryString()` 确认 SQL 里确实带了过滤条件。
- 策略表达式里若引用了无法翻译的成员，EF 会抛异常；此时考虑改用 `Apply` 之后再做其他过滤，
  或调整维度选择器为可翻译形式。
- 上下文要求：`IScopeGuard` 依赖当前 `UserPrincipal`。在后台任务里没有请求作用域时，
  需自行提供 `UserPrincipal` 并在数据变化后调用 `Refresh()`。

---

## 8. 性能注意事项

- 授权数据每请求只解析一次，但**下推到数据库的过滤条件是每次调用重建的**：
  同一请求内多次 `Apply` 会重复构造表达式树，热点路径上建议缓存 `IQueryable`。
- 已编译策略按（资源类型，权限码）缓存，因此不同权限码不会互相顶掉。
- `Refresh()` 会丢弃已编译策略；授权数据频繁变化时应把变更频率与 `Refresh` 频率一并考虑。
- 策略表达式的复杂度直接决定 SQL 的复杂度：嵌套 `Any` 会被翻译成相关子查询，
  层数越深越慢。

---

## 9. 从「写在别处的行级条件」迁移

若此前行级条件散落在各处（各自的查询过滤、控制器里的手写判断），本库要求把它们集中到
`ScopeModel<T>` 的 `Policy` / `Declare` 中。集中之后有两个直接好处：
判定时机统一（读侧下推与单行判定共用一份编译结果），
授权值也终于只有一个入口（`IScopeSubjectResolver`）。

迁移路径：

1. 把各处手写的行级条件改写为 `ScopeModel<T>` 的 `Policy` / `Declare`。
2. 把原本写死在代码里的授权值改为由 `IScopeSubjectResolver` 实时解析。
3. 把原先分散的判定位置改为在统一的边界调用 `IScopeGuard`。
