# Euonia.Security

数据权限（行级）与操作权限的**策略引擎**。本库只依赖 `Euonia.Core` 与微软的 DI / 配置抽象包
（`Microsoft.Extensions.DependencyInjection.Abstractions`、`Microsoft.Extensions.Configuration.Abstractions`），
不认识任何对象模型：它定义策略、编译策略、判定与下推，但**不决定强制执行点在哪里**。

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

// 注册策略引擎并声明操作入口规则：数据权限模型注册表（注册期即完成全部校验）、
// IPermissionChecker、IScopeGuard、PermissionSetup
services.AddPermission(options => options
        .OnAttributeOrName(BusinessOperation.Read,   "Order", typeof(FetchAttribute))
        .OnAttributeOrName(BusinessOperation.Create, "Order", typeof(CreateAttribute)),
    typeof(Order).Assembly);

// 当前用户主体（判定主体）：由宿主注册，取其 Claims 即 ClaimsPrincipal
services.AddSingleton(UserPrincipal.Current);
```

规则必须**显式**给出——引擎不猜「哪个方法对应哪个业务操作」。载体有三种，语义完全一致：

| 载体 | 用法 | 何时用 |
|---|---|---|
| **回调**（推荐） | `AddPermission(o => o.OnAttributeOrName(…), assemblies)` | 绝大多数情况：规则与代码同源、可导航、随代码评审发布；模块可各自贡献（[§3.2](#32-多个模块各自注册按并集合并)） |
| **配置节** | `AddPermission(configuration.GetSection("Permission"), assemblies)` | 部署期要换命名约定时（[§3.4](#34-规则的载体回调--配置节--自定义来源)） |
| **自定义来源** | `AddPermission(new MyCodeSource(), assemblies)` | 规则不在代码也不在配置里（例如来自数据库） |

确实**没有方法级权限码**时，用 `AddPermission(EmptyCodeSource.Instance, assemblies)` 做断言——那是显式的
「本应用没有方法级权限码」，不是默认值（见 [3.1](#31-为什么必须显式给出规则)）。

`IPermissionCodeSource` 与 `IScopeKeyResolver` 都是 `TryAdd` 语义：宿主已注册的实现不会被覆盖。

若使用了权限（操作权限的权限码或数据权限），还必须**由应用注册一个 `IScopeSubjectResolver`**
（见 [4.2](#42-用户侧授权值从数据实时解析)）。框架不提供默认实现，以免把授权值固化。

容器构建后请调用一次启动期校验，使「声明了权限却忘了接解析器」在启动时失败：

```csharp
var provider = services.BuildServiceProvider();
provider.ValidatePermissionSetup();   // 缺少 IScopeSubjectResolver 或 UserPrincipal 时在此抛出
```

### 3.1 为什么必须显式给出规则

方法级 `[Permission]` 写在方法上，而「哪个方法对应哪个业务操作」取决于使用方的约定——
有的框架用特性标记工厂方法，有的靠命名约定，引擎无从推断。因此没有任何默认规则：
**忘了给出规则必须是一个错误，而不是一次静默放行**（空回调/空配置会在注册处直接报错，见 §3.4）。

应用确实**不使用方法级**权限码时，传入 `EmptyCodeSource.Instance`。这是一个显式的断言，
不是「忘了提供来源」的默认值。

代价是：模型里的按码声明（`ScopePolicySet<T>.For("code", …)`）与 `EmptyCodeSource` 不可同用。
因为没有任何操作能解析到应用自定义的码，注册期死策略校验会拒绝启动：

```
权限模型 'OrderModel' 为权限码 'order:edit' 声明了行级策略，
但没有任何操作会解析到该码（请核对方法上 [Permission] 的码与 Declare 里的码是否一致）。
```

这正是期望行为——**声明了按码策略就说明存在方法级权限码**，那就必须给出方法与操作的对应关系。

> 只追加扫描范围请用 [`AddPermissionModels`](#32-多个模块各自注册按并集合并)，但注意它**不提供来源**，
> 因此**不构成**上面这个断言。用它代替 `EmptyCodeSource` 时，方法级 `[Permission]` 会被静默忽略
> （按码声明的策略仍会被死策略校验拦住）。

### 3.2 多个模块各自注册（按并集合并）

`AddPermission` **可以调用多次，每次的贡献都会被合并**——每个模块在自己的
`ConfigureServices` 里贡献自己的规则即可（回调载体让这件事成为自然写法）：

```csharp
// 订单模块
services.AddPermission(o => o.OnAttributeOrName(BusinessOperation.Read, "Order", typeof(FetchAttribute)),
                       typeof(Order).Assembly);

// 报表模块
services.AddPermission(o => o.OnAttributeOrName(BusinessOperation.Read, "Report", typeof(ReportFetchAttribute)),
                       typeof(Report).Assembly);
```

合并规则：权限码来源合成一个（操作取并集，权限码取并集去重），程序集取并集，
最终只构建**一个** `ScopeModelRegistry`，所有模块的模型与权限码都在里面生效。

这不是可有可无的宽松设计。若采用「先到先得」，那么先注册的模块就决定了全局的码来源，
后注册模块的权限码会被**静默丢弃**——它对应的行级策略因此永远不会被解析到，
判定反而比作者本意**更宽松**。这类失败没有任何报错，只在生产环境表现为「权限没拦住」。

> 每个模块的回调各自成组地合并（操作取并集、权限码取并集去重）；
> 同一个 `IPermissionCodeSource` 实例被重复传入时按幂等处理（跳过），
> 因此 `EmptyCodeSource.Instance` 这样的共享单例可以被每个模块放心共用。
> 同一个程序集被多个模块传入同样只扫一次。

注意 `IScopeKeyResolver` 仍是 `TryAdd` 语义（**先到先得**）：它决定「某个资源实例当前代表哪个操作」，
属于**全局**语义，多个模块同时给出不同答案本身就是配置错误。需要按模块区分时，
请自行实现一个带分派的 `IScopeKeyResolver`。

**来源只有一处、模型却分散在多个程序集**时，用 `AddPermissionModels` 单独追加扫描范围，
不必重复传同一个来源实例：

```csharp
services.AddPermission(new OrderCodeSource(), typeof(Order).Assembly);  // 给出唯一的权限码来源
services.AddPermissionModels(typeof(Report).Assembly);                  // 该程序集只有模型，无方法级权限码
```

它只追加程序集（同样按幂等合并），**不提供权限码来源**；来源沿用此前注册的那个，
从未注册过则回落到 `EmptyCodeSource`（此时不要指望方法级权限码生效，见 §3.1）。
每次调用同样会重建并校验注册表，因此配置错误依旧在注册处抛出。

### 3.3 用规则描述「哪个方法对应哪个操作」

绝大多数宿主的答案是同一种形状：入口方法要么打了某个特性，要么叫某个名字。规则把这件事写成
**数据**而不是代码，换框架只需换一组规则：

```csharp
services.AddPermission(options => options
        .OnAttributeOrName(BusinessOperation.Read,   "Order", typeof(FetchAttribute))
        .OnAttributeOrName(BusinessOperation.Create, "Order", typeof(CreateAttribute))
        .OnAttributeOrName(BusinessOperation.Update, "Order", typeof(UpdateAttribute))
        .OnAttributeOrName(BusinessOperation.Delete, "Order", typeof(DeleteAttribute)),
    typeof(Order).Assembly);
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

services.AddPermission(options => options.OnAttributeOrName(approve, "Order", typeof(ApproveAttribute)),
    typeof(Order).Assembly);
```

每个操作的默认策略键由 `ScopeKeys.For(operation)` 派生为 `@<operation>`；操作名不得以保留前缀
`@` 开头。判定的其余部分与操作集合无关。

### 3.4 规则的载体：回调 / 配置节 / 自定义来源

三种载体产出的是**同一套规则**（同一份编译、同一份注册期校验），因此可以混用并取并集。
区别只在「规则写在哪里、随什么发布」：

| | 回调 | 配置节 | 自定义来源 |
|---|---|---|---|
| 规则位置 | 代码（组合根或各模块） | `appsettings` 等配置源 | 任意（数据库、远端…） |
| 编译期可见 / 可导航 | ✅ | ❌ 类型名是字符串 | ❌ |
| 表达力 | 全部（含自定义谓词） | 特性类型名 + 方法名 | 全部 |
| 适合 | **默认选择** | 部署期换命名约定 | 规则不在应用里 |

**配置节**的形状：

```json
{
  "Permission": {
    "Operations": {
      "read":    { "Attributes": ["MyApp.Web.OrderFetchAttribute"], "Names": ["Fetch", "Get"] },
      "approve": { "Names": ["Approve", "ApproveAsync"] }
    }
  }
}
```

```csharp
services.AddPermission(configuration.GetSection("Permission"), typeof(Order).Assembly);
```

- `Attributes` 是入口特性**类型名**（先按运行时能解析的名字找，再到传入的程序集里按完整名/短名唯一匹配），
  `Names` 是入口**方法名**；二者并存时为「命中其一即为入口」的或语义；
- **与 `OnAttributeOrName` 不同，配置不推导候选名**：后者会把 `OrderFetchAttribute` 展开成
  `Fetch` / `FetchAsync` / `OrderFetch` / `OrderFetchAsync`，配置只认 `Names` 里字面写出的名字。
  从回调/链式写法迁到配置时，请一并把候选名写全，否则「按命名」那一半会静默消失；
- **全部错误都在注册期暴露**：缺 `Operations` 节点、操作下有未知节点、某操作没有任何规则、
  写成标量而不是数组、数组里有空项、类型名解析不到或有歧义、类型不是特性或是泛型——
  都会在 `AddPermission` 处抛出并指明修法（含出错节点路径）；
- 读出来的规则与回调声明的规则合并，因此「一部分约定写代码、一部分放配置」是允许的。

> ⚠️ **配置驱动等于把鉴权口径交给配置文件**：把某个方法从「需要审批码」改成「无码」只是一次配置改动。
> 特性类型与自定义谓词天然属于代码；请让配置文件与代码走**同一套评审与发布流程**，
> 不要把它当成运维侧的可调开关。配置只在注册期读一次，**不订阅变更**——改配置不会改变门禁，需重启。

> **Osba 宿主**：用 `Euonia.Osba.Security` 包的 `AddObjectPermission` 接入本引擎
> （`Euonia.Osba` 自己不引用引擎——它只定义权限契约，见 [DESIGN §1.11](DESIGN.md)）。
> 接入后，运行期判定与注册期校验用**同一个来源**：你在这里补充的规则，工厂边界同样生效
> （与 Osba 自己的工厂约定取并集）。

**自定义来源**：两种载体都表达不了时（例如规则来自数据库、或需要按租户分派），实现
`IPermissionCodeSource` 并直接传入 `AddPermission(instance, assemblies)` 即可——它只回答
「某类型在某操作上声明了哪些权限码」这一个问题。

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

模型在程序集扫描时自动发现，并在启动期完成全部校验——配置错误一律 fail-fast，
校验项清单见 [§5.6](#56-启动期校验)。

维度取值默认取自资源行自身（`Map`）。若授权关系落在**子表**（成员表 / 关系表）里——
「我加入了哪些团队 / 家庭 / 组织」就是这种形状——用 `MapMany` 声明为集合维度，见 [§5.9](#59-子表维度关系表作为取值来源)。

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

下列问题全部在启动期暴露——它们的共同性质是「看似启用了数据权限、实际没有按预期生效」：

| 诊断 | 含义与修法 |
|---|---|
| 模型无法实例化 | 程序集扫描要求模型有公共无参构造；需要构造参数的模型改用程序化注册（§5.7） |
| 模型未声明任何维度 | `Define` 里至少调用一次 `ScopeModelBuilder<T>.Map`（取值来自子表时用 `MapMany`） |
| 集合维度的取值形状不受支持 | `MapMany` 的选择器不是「导航集合（可带 `Where` 过滤）再取字符串值」。只有这一种形状能下推为 `EXISTS`，故注册期直接拒绝（§5.9） |
| 同一维度被重复声明 | 同一个维度名被 `Map` 与 `MapMany`（或同名声明两次）声明——一个维度只能有一个取值来源 |
| 同一资源类型存在多个模型 | 一个类型只能有一个 `ScopeModel<T>`；程序化注册同样参与检查 |
| 未提供策略 | `Policy` 返回 `null`。模型与策略必须写在同一个声明类型里 |
| 声明了权限码但未提供策略 | `Declare` 里声明了该码，却没有对应的 `For(code, …)` |
| 策略引用了未映射的维度 | 策略里的 `Grant(d)` 没有在 `Define` 中 `Map(d, …)`——这是「写了却没映射 ⇒ 静默放行」的根治点 |
| 策略结构性恒不放行 | 归约后 `Allow` 恒假，通常是 `Any` 之下全是拒绝条件（§5.3） |
| 同一操作解析出多个有策略的权限码 | 实际生效哪一个不允许靠猜：请让同一操作最多只有一个码声明策略（§5.8） |
| 声明了策略却没有任何操作会解析到该码（死策略） | 方法上 `[Permission]` 的码与 `Declare` 里的码对不上；否则行级策略会静默失效并回落到默认策略 |

所有问题**一次报全**（`ScopeModelValidationException.Diagnostics`），而不是修一个跑一次——
启动期配置错误往往同时存在多处，逐个报出等于让人反复重启。

授权数据源与用户主体是否齐备由 `provider.ValidatePermissionSetup()` 检查（必须在容器构建**之后**调用，
因为解析器的注册顺序不受约束）。若遗漏该调用，首次判定时同样会以明确错误暴露。

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

### 5.8 策略键的解析

策略键是「哪一套行级策略生效」的标识。它只由**操作**决定，不受调用方状态直接影响
（[DESIGN §1.7](DESIGN.md)）：资源状态的改变只是改变了实际执行的操作，而每个操作各自应用自己的策略。

**模型侧**：某个码上声明了专属策略（`PolicySet<T>.For(code, …)` / `ForOperation(…)`）就用该码的策略，
否则回落到模型必填的默认策略 `Policy`。

**判定侧**，键按以下顺序确定：

| 顺序 | 来源 | 说明 |
|---|---|---|
| 1 | 调用方显式传入的码 | `guard.Allows(entity, "order:delete")` 这类带 `scopeKey` 的入口；非空即直接采用 |
| 2 | `IScopeKeyResolver.Resolve` | 由使用方回答「这个资源实例当前代表哪个操作」。未注册该实现时跳过本步 |
| 3 | `ScopeKeys.Default`（`@default`） | 兜底键。`Apply` / `GetPolicy` 直接取它，因为查询没有「当前操作」这一上下文（§5.4） |

**操作侧**：每个操作的默认键由 `ScopeKeys.For(operation)` 派生为 `@<operation>`（`@read` / `@create` / …，
外加宿主自定义的操作）。派生是纯字符串拼接而非查表，因此**任何**操作名都能得到确定的键；
操作名本身不得以保留前缀 `@` 开头。

**歧义即失败**：同一操作若能解析出多个「声明了策略」的权限码，注册期直接失败——
「实际生效的是哪一个」不允许靠猜。

> 权限码的 `*` 前缀通配只用于「是否持有该权限码」的布尔判定，**不参与**策略键与维度取值的解析
> （[DESIGN §1.6](DESIGN.md)）。

---

### 5.9 子表维度（关系表作为取值来源）

**问题**：「我加入了哪些团队 / 家庭 / 组织」这类权限，关系存在**子表**里
（`team_member(team_id, user_id, status)`），资源却是父行（`team`）。维度取值只能取自资源行自身时，
唯一的办法是让解析器把关系**反向展开**成一堆 id（[DESIGN §2.1](DESIGN.md)）：关系表与授权数据要互相同步、
每次解析多一次反查与一个 `IN (...)`，而且关系的属性（`status`、`expires_at`）无法参与行级判定。

**做法**：用 `MapMany` 把子表声明成一个**集合维度**——维度取值是「子表里的一组值」：

```csharp
public sealed class TeamScope : ScopeModel<Team>
{
    public override void Define(ScopeModelBuilder<Team> builder)
        => builder.Map(ScopeDimensions.Owner, t => t.LeaderId)                    // 行内的列：单值维度
                  .MapMany(ScopeDimensions.Member,                                // 子表：集合维度
                           t => t.Members.Where(m => m.Status == "active")
                                         .Select(m => m.UserId));

    public override ScopePolicy<Team> Policy
        => ScopePolicy<Team>.Any(ScopePolicy<Team>.Grant(ScopeDimensions.Member),
                                 ScopePolicy<Team>.Grant(ScopeDimensions.Owner));
}
```

解析器侧与其它维度没有区别——授予的是「子表里应当出现的值」，通常就是**当前用户的标识**：

```csharp
builder.Add(ScopeDimensions.Member, userId);      // 「我是这个团队的成员」
```

> `owner` 维度已被行内的列占用时（如上例的 `LeaderId`），成员关系必须另立维度名——
> 同一个维度只能声明一次。授予的值标识的是**子表里的那个值**，不是资源标识。

判定与查询：

```csharp
await guard.EnsureResolvedAsync(ct);
guard.Allows(team, "team:view");            // 单行判定：要求 team.Members 已加载（见下方边界）
guard.Apply(db.Teams, "team:view");         // 下推：交给数据库做 EXISTS 相关子查询
```

生成的 SQL 就是这个形状（真实提供程序实测，非示意）：

```sql
SELECT "t"."id", "t"."name" FROM "team" AS "t"
WHERE EXISTS (
    SELECT 1 FROM "team_member" AS "m"
    WHERE "t"."id" = "m"."team_id" AND "m"."status" = 'active' AND "m"."user_id" = 'dev')
```

**语义与单值维度是同一套**：`Grant(d)` 一律是「资源在该维度上的取值集合 ∩ 用户被授予的集合 ≠ ∅」
（单值只是集合的退化情形），因此 `Self()` / `Any` / `All` / `Deny` / 策略键 / 审计全部照旧，
`Explain` 里该叶子会带上 `[]` 标记（`Grant(member[])`）以便与行内列区分。
子表属性写在选择器里，由数据库**实时**求值——成员关系一改，下一次查询即生效，
不需要等授权数据刷新（对比 [§5.5](#55-缓存契约) 的解析快照）。

**边界**（每条都对应一类真实事故）：

| 边界 | 说明 |
|---|---|
| 单行判定要求子集合已加载 | **仅当该策略引用了子表维度**：`Allows` / `AllowsObject` / `Filter` / `Explain` 在内存中求值同一棵表达式，实例上子集合为 `null` 时**抛 `InvalidOperationException`**（指明维度与三条修法），绝不静默拒绝。写侧因此有个天然出路——把写侧策略写成只按行内列判定（如 `Grant(owner)`），它就不要求对象图完整。 |
| 初始化为空集合 ⇒ 静默拒绝 | 实体若把集合初始化成 `= []`，「未加载」与「确实没有成员」无法区分，表现为拒绝且无报错。这是**有意接受的边界**（[DESIGN §2.5](DESIGN.md)），要么去掉初始化器让它报错，要么就只走下推路径。 |
| `Apply` 必须作用在实体查询上 | 先投影成 DTO / 读模型再过滤时集合已被物化，无法翻译。读模型要参与下推，请只声明行内的列维度。 |
| 只支持存在量词 | 「子行**全部**命中」这类全称量词不支持——判定语义始终是集合成员判断。 |
| 值类型仍是 `string` | 与单值维度一致（[DESIGN §2.4](DESIGN.md)）；子表列是 `Guid`/`long` 时请提供字符串投影。 |
| 子表的写入口要单独设防 | 子表维度把「谁属于这个资源」的判定权交给业务数据，因此成员表的增删改必须由**操作权限**把守。 |

**取值形状只有一种**，其余在**注册期**被拒绝：

```csharp
x => x.Members.Select(m => m.UserId)                        // ✅
x => x.Members.Where(m => m.Status == "active").Select(m => m.UserId)   // ✅ 子表属性参与判定
x => x.MemberIds                                            // ✅ 元素本身就是字符串
x => x.Tags.Concat(x.OtherTags)                             // ❌ 注册期报错：形状不受支持
```

理由：可下推的只有「导航集合 + 过滤 + 元素谓词」这一种形状。与其让它在查询时被提供程序
抛出翻译失败，不如在启动时说清楚——这也是本库一贯的 fail-fast 取舍。

**与「解析器反向展开」怎么选**（[DESIGN §2.1](DESIGN.md)）：

| 关系在哪 | 用什么 |
|---|---|
| 与查询同库、关系行可枚举（成员表这类） | `MapMany`——`EXISTS` 下推，实时、无需同步、无需反向展开 |
| 关系在别处（跨库 / 外部服务），或需要「授组 id 而非行 id」的收窄 | 解析器反向展开为扁平集合（§2.1），框架只看到 `IN (...)` |
| 行数巨大且关系极稀疏 | 逃生舱 `Where(x => aclQuery.Contains(x.Id))`，自行评估成本 |

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
| `ScopeModelBuilder<T>` | 声明维度取值（`Map` 行内列 / `MapMany` 子表）与分类属性 |
| `ScopeModelRegistryBuilder` | 程序化注册模型（实例 / 类型 / 程序集） |
| `OperationCodeSource` | 通用的「方法 → 业务操作」规则化来源（按特性或命名约定） |
| `ScopePolicy<T>` | 策略组合：`Grant` / `Deny` / `Self` / `Any` / `All` / `Not` |
| `ScopePolicySet<T>` | 按权限码声明行级策略 |
| `ScopeDimensions` | 预置维度（`Owner` / `Dept` / `Member` / …） |
| `ScopeKeys` | 保留键（`@default` / `@read` / `@create` / …）与操作默认键的派生（§5.8） |
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
| `IPermissionCodeSource` | 提供「某类型在某操作上声明了哪些权限码」，用于注册期校验；也是自定义规则的扩展点（§3.4） |
| `IPermissionRequirementSource` | 在权限码之上回答「要求」（含角色）；`OperationCodeSource` 与合并来源已实现（DESIGN §1.11） |
| `IPermissionRequirementProvider`（Core） | 「某类型在某操作上有哪些要求」的唯一声明，宿主框架与引擎共用；`AddPermission` 会把容器里的权限码来源注册成它（只给码的来源折算为「有码、无角色」） |
| `IScopeKeyResolver` | 把资源实例解析为策略键（§5.8 的第 2 步） |
| `ScopeKeyResolver.Resolve` | 由「注册项 + 操作 + 权限码来源」解析策略键（唯一出口，§5.8） |
| `ScopeModelRegistryBuilder` | 换掉程序集扫描，改为程序化注册 |
| `OperationCodeSourceBuilder` | 声明规则：`OnAttribute` / `OnMethodName` / `OnAttributeOrName` / `OnMethod`（§3.3） |
| `OperationConventions` | 按特性名推导候选方法名（§3.3） |

---

## 7. 故障排查

### 启动期异常（都是**有意**的 fail-fast）

| 现象 | 原因 |
|---|---|
| 「同一操作解析出多个声明了行级策略的权限码」 | 同一操作上多个权限码都声明了策略，实际生效哪一个不允许靠猜 |
| 「声明了权限码但没有任何操作会解析到该码」 | `Declare` 里的码与方法上 `[Permission]` 的码对不上，行级策略会静默失效 |
| 「策略引用了未映射的维度」 | 策略引用的维度没有在 `Define` 里 `Map`——这是「写了却没映射 ⇒ 静默放行」的根治点 |
| 「模型未声明任何维度」 | `Define` 里没有调用 `Map`；维度是策略的取值来源，缺了它策略无从表达 |
| 「声明了权限码但未提供策略」 | `Declare` 里声明了该码，却没有对应的 `For(code, …)`；模型与策略必须写在一起 |
| 「策略结构性恒不放行」 | 归约后 `Allow` 恒假，通常是 `Any` 之下全是拒绝条件——先确认这是本意还是漏写了允许条件 |
| 「权限模型无法实例化」 | 程序集扫描要求模型有公共无参构造；需要构造参数时改用 §5.7 的程序化注册 |
| 「集合维度的取值形状不受支持」 | `MapMany` 只接受「导航集合（可带 `Where` 过滤）再取字符串值」这一种形状，其余无法下推为 `EXISTS`（§5.9） |
| 「维度 'x' 在类型 'Y' 的权限模型中重复声明」 | `Map` 与 `MapMany` 声明了同一个维度名——一个维度只能有一个取值来源 |
| 「未注册 IScopeSubjectResolver」 | 声明了模型或权限码却没接授权数据源 |
| 「同一资源类型存在多个模型」 | 一个类型只能有一个模型（程序化注册同样参与检查） |
| 「未注册 UserPrincipal」 | 判定主体取自 `UserPrincipal`；不注册则取用 `IScopeGuard` 直接失败 |
| 「权限模型注册期校验失败，共 N 处问题」 | 这是**汇总**异常，`Diagnostics` 列出了全部问题，按序号逐条修 |
| 「没有声明任何操作入口规则」（回调载体） | 回调是空的：补规则，或改用 `EmptyCodeSource.Instance` 断言 / `AddPermissionModels` 只追加扫描（§3.1） |
| 「权限规则配置缺少 'Operations' 节点」 | 传了根配置而不是配置节：改成 `configuration.GetSection("Permission")`（§3.4） |
| 「配置里的操作 'x' 没有声明任何入口规则」 | 该操作既没给 `Attributes` 也没给 `Names`（键名拼错也会落到这里，消息会列出可用键） |
| 「配置里的入口特性类型 'x' 无法解析 / 匹配到多个类型」 | 类型名写错或有歧义：改用完整类型名（命名空间 + 类型名），或直接给 `Names`（§3.4） |

完整的模型级校验项清单（含每一条的修法）见 [§5.6](#56-启动期校验)。

### 判定结果不符合预期

1. 先看 `guard.Explain(entity, code)`：它会给出命中的维度与最终结论。
2. 再确认**授权值的形状**与维度选择器同源：授予的 `AddGrant(code, "order", ids)` 中的 `ids`
   与 `Map("order", x => x.Id)` 取到的必须是同一套值。
3. 再确认用的是**同一个权限码**：默认键与 `policies.For(code, ...)` 声明的键不是同一套策略，
   参见 [DESIGN §1.6](DESIGN.md)。
4. 若结论来自**子表维度**（审计里显示 `Grant(member[])`，§5.9）：先核对授予的值与子表列里的值
   是不是同一套——最常见的是把**资源标识**授予了成员维度（父子的方向搞反了，结果是恒不放行）；
   再确认单行判定时子集合已加载（未加载会抛错而不是给出结论）。

### 下推相关问题

- `Apply` 必须下推为**表达式**（`Where` 带 `Expression`），而不是先物化再过滤——
  后者会把全表拉进内存。可以用 `ToQueryString()` 确认 SQL 里确实带了过滤条件。
- 策略表达式里若引用了无法翻译的成员，EF 会抛异常；此时考虑改用 `Apply` 之后再做其他过滤，
  或调整维度选择器为可翻译形式。
- 子表维度（§5.9）产生的条件是**相关子查询**（`EXISTS`），因此 `Apply` 必须作用在实体查询上、
  且在投影之前：投影成 DTO 之后集合已被物化，无法再翻译。
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
- 子表维度（§5.9）的 `EXISTS` 子查询按子表的过滤列取数，请在子表上建
  `(父标识, 值)` 组合索引（例如 `(team_id, user_id)`），否则每行都要扫一遍子表。
- 单行判定（`Allows` / `AllowsObject`）在子表维度上多了一次加载探测；
  大批量对象应改用 `Apply` 下推，而不是逐行判定。

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
