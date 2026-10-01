# Euonia.Osba 权限接线设计说明

> 面向**维护者与评审者**的设计决策记录。本文只记录**「引擎与宿主之间」**的决策：
> 策略引擎自身的语义与取舍见 [`Euonia.Security/DESIGN.md`](../Euonia.Security/DESIGN.md)，
> 使用说明见 [PERMISSION.md](PERMISSION.md)。

`Euonia.Security` 只回答「能不能」，**不拦截任何调用**。因此「在哪里裁决、以什么异常形态裁决」
必然是宿主框架的决定。以下决策就是关于这个接缝的。

---

## 1. 核心决策

### 1.1 无法判定时必须失败

**问题**：目标声明了权限要求，却取不到 `BusinessContext` 时，解析不出 `IPermissionChecker`。
早期实现此时返回「放行」，理由是「没有检查器就当没有权限体系」。后果是：
调用方 `new` 出对象、忘了设 `BusinessContext`，`factory.SaveAsync(obj)` 会**静默跳过整条授权链**——
用户哪怕一个权限码都没有也能保存成功。这不是「配置缺失」，而是「忘了接线」，
比配置缺失更隐蔽，因为代码看起来是对的。

**决策**：在强制点（`ObjectAuthorization` / `ScopeAuthorization`）改为**抛
`InvalidOperationException`**，消息指明缺的是 `BusinessContext`。原则与其它几处一致：
数据范围无法判定即失败、缺解析器即抛、启动期校验缺失即失败。

**边界**：只对**声明了要求**的类型生效——没有任何 `[Permission]`、也没有 `ScopeModel<T>` 的类型
不强制接线，避免给不关心权限的应用加无谓约束。

**注意**：`BusinessObject.CanPerformOperation(...)` **仍是查询**（无从判定时返回
`true`，不抛异常）；闸门在强制点。这与数据权限侧的分工相同。

### 1.2 权限与验证是两条线，越权一律抛 `SecurityException`

**问题**：早期版本把越权也表达成规则（`ScopePolicyRule` / `PermissionRule` + 对已声明类型的
自动注入），让越权以 `BrokenRules` → `ValidationException` 的表单错误出现。看上去更「UI 友好」，
实则四重代价：

1. **形态不对称**：删除路径默认跳过对象级规则，于是越权新增/更新抛 `ValidationException`、
   越权删除抛 `SecurityException`——同一个「越权」出现两种异常类型，调用方无法只按一个类型分支。
2. **绕过即放行**：`SuspendRuleChecking()` / 执行器 `BypassRuleChecks()` 能整体跳过规则；
   而 `Rules.RunAsync` 把所有异常转成错误，规则**结构上**就抛不出 `SecurityException`。
3. **缓存粒度冲突**：为让注入不受 `RuleManager`（按类型的进程级静态缓存）影响，只能改成
   按实例、每次接线判定，引入一层纯粹为了绕开缓存的复杂度（§2.4）。
4. **概念混淆**：「这份数据不合法」与「你不许碰它」是两件事。把越权混进表单错误列表，
   调用方会按字段提示渲染它，而不是当成授权失败。

**决策**：删除 `PermissionRule` / `ScopePolicyRule` 与自动注入，**权限只走工厂边界**：

| | 验证线 | 权限线 |
|---|---|---|
| 回答 | 这份数据**合不合法** | 这个用户**能不能**做这件事 / 碰这行 |
| 裁决者 | 规则（属性级 / 对象级） | `ObjectAuthorization`（操作权限）+ `ScopeAuthorization`（数据范围） |
| 位置 | `EditableObject.SaveAsync`；命令对象由 `ObjectRuleGuard` 在命令体之前 | `BusinessObjectFactory` 调用边界（前置 + 后置） |
| 失败 | `ValidationException`（`Errors` 带属性名） | `SecurityException` |
| 可否绕过 | 可（`SuspendRuleChecking` / `BypassRuleChecks` / `WithRuleChecksOnDelete`） | **不可**，无任何开关 |

**收益**：越权在所有操作上一致（新增 / 更新 / 删除 / 命令都是 `SecurityException`），
§2.2 的形态不对称随之消失；「权限可以被绕过」这个提法本身不再成立。

**代价**（有意的）：越权不再出现在 `ValidationException.Errors` 里。需要「表单预提示」的场景，
在调用侧显式捕获 `SecurityException`，或先用 `CanPerformOperation` / `IScopeGuard.AllowsObject` 查再跳。
这是把一条**静默的口头约定**换成**显式的 API**。

---

### 1.3 权限契约归 Osba，实现由宿主选择

**问题**：早期 `Euonia.Osba` 直接引用 `Euonia.Security`：工厂边界调用引擎的两个闸门，
「哪个方法对应哪个操作」由引擎的 `OperationCodeSource` 扫描。于是**对象模型库把策略引擎当成了必需依赖**——
不想用引擎的宿主也得把它装进来，也无法换上自己的鉴权实现；而且依赖方向是反的：
引擎反过来要求 Osba 使用它自己的概念（`BusinessOperation`、`PermissionAttribute` 都由引擎定义）。

**决策**：把权限拆成「对象模型的知识」与「鉴权实现的知识」两半，各归其主：

- **基础词汇**（`PermissionAttribute`、`BusinessOperation`）下沉到 `Euonia.Core`，命名空间不变——
  不装引擎的宿主也能在业务对象上声明要求；
- **「要求从哪来」与「操作权限判定」下沉到 Core**（`IPermissionCodeSource`、`IPermissionChecker`），
  与 `[Permission]`、`BusinessOperation` 同层：它们是两边共同的基础概念，各只声明一次——若宿主与引擎
  各定义一遍形状相同的接口，中间就得有胶水来回翻译，那是抽象放错了层。
  两个接口都只要求实现必需成员（来源给 `AllOperations` / `CodesFor`，判定给 `IsGranted` /
  `IsInRole` / `EnsureResolvedAsync`），默认实现只覆盖来源的要求折算与判定的异步入口；
  Osba 只提供来源的**默认实现**（按工厂约定扫描，兜底静态单例，见 §1.1）；
- **只有行级判定留在 Osba**（`IObjectScopeAuthorizer`）：它要读宿主的作用域（`BusinessContext`）与对象
  状态，引擎无法实现；工厂边界保留**强制**（`SecurityException` / 判定不了抛
  `InvalidOperationException`）；
- **跨边界的契约全部下沉到 Core**（`IPermissionCodeSource`、`IPermissionChecker`、`IObjectScopeAuthorizer`、
  `IObjectOperationResolver`），**由两边各自实现自己懂的那一半**：Osba 提供来源的默认实现与
  「对象状态 → 操作」，引擎提供策略编译、行级判定与按操作选取行级策略。
  `Euonia.Osba` 与 `Euonia.Security` 之间没有边，**也不再需要任何适配包**——这正是本条判据的由来
  （跨边界契约放在中间某一侧，就必然长出一个翻译者）。

**收益**：宿主可以接引擎、也可以只注册自己的三个实现（`Euonia.Osba.Standalone.Tests` 是这种用法的
可运行证明）；依赖方向变成 `适配包 → (Osba, Security)`，两边谁都不认识谁。

**代价（需要使用者动作）**：宿主把 `AddObjectPermission(asm)` 换成
`AddPermission(p => { p.Scan(asm); p.Source(ObjectPermissionRequirementProvider.Instance); })`
（两行，各自属于一个库）；
`[Permission]`、`BusinessOperation` 的命名空间不变，因此**源码兼容**，但二进制不兼容（类型换了程序集）。

**判据（评审时用）**：适配代码可以存在，但要盯住两类信号——
① 适配层里出现「两端形状相同、只为翻译」的代码 ⇒ 抽象放错了层，把这个概念下沉到双方都依赖的最底层
（本条就是这么发现并消灭了要求来源的重复声明、以及「权限判定」与「要求来源」两处重叠接口——
它们现在都只有一份声明，住在 Core；默认实现也各只有一处，宿主框架与引擎都不再各写一遍）；
② 适配层开始 reach-in（用某一方的 internal，或复制它的判定逻辑）⇒ 契约划错了，说明该由那一方自己实现。
留下的适配若是「纯翻译 + 只碰公开 API + 宿主选择才引入」，那它就是两个独立库组合时的固有成本；
反过来，把适配写进任一方（本次之前的做法：胶水在 Osba 里，且只能接一个引擎）才是真正的通用性缺口。

**行为收窄（有意）**：要求来源改为与**工厂查找方法**同一套候选口径（`ObjectReflector.GetFactoryMethods`）——
工厂只在「当前类型这一层没有候选」时才上溯基类。因此被派生类型遮蔽的基类方法上的权限声明不再被收集：
那些方法不会被工厂调用，为它们收集要求只会产生永远无法满足的闸门。旧文档声称「扫描口径与工厂查找一致」，
实际上两条口径不同（引擎侧扫描整个继承链、无 DeclaredOnly）；本次**把这句声明变成真的**，
并把收窄钉在 `PermissionScanScopeTests`。

**被否决的方案**：

| 方案 | 否决理由 |
|---|---|
| 维持 `Euonia.Osba → Euonia.Security` | 见「问题」：对象模型被策略引擎绑死，宿主无从替换鉴权实现 |
| 契约留在引擎、Osba 实现（现状的反向版） | 契约是「对象模型的知识」（对象状态 → 操作），放在引擎里等于引擎继续认识对象模型 |
| 只把 `Permission/` 拆成新包、不反转依赖 | `BusinessObject` / `BusinessObjectFactory` 里的调用点仍在 Osba，包拆分减少不了耦合，只是把引用换了地方 |
| Osba 自定义一套标记、由适配层翻译 | 全库会出现两个 `[Permission]`（业务对象用一个、引擎模型可能用另一个），多一层映射与两套文档；下沉到 Core 只有一个 |

---

### 1.4 等待点放在宿主入口，不藏在判定路径深处

**问题**：引擎的同步读若在冷缓存时隐式等待解析，等待就发生在**判定路径深处**——工厂的异步入口
会因此退化成 sync-over-async，且「哪里可能阻塞线程」不可枚举。

**决策**：引擎的同步读**只读已解析的快照**，冷缓存时抛 `InvalidOperationException`（绝不阻塞）；
「什么时候可以等」交给宿主入口，由 `AuthorizationWarmup` 统一处理：

- **异步入口**（`BusinessObjectFactory` 的 `*Async`）`await` 预热（`EnsureResolvedAsync`），全链路不阻塞；
- **同步入口**（`Create` / `Fetch` 等同步重载）本就运行在同步契约上，在入口用 `AsyncContext.Run`
  阻塞一次（每作用域仅一次）——等待点可枚举，且只出现在宿主自己的入口。

**收益**：判定路径不再隐含 I/O 等待（负载下不再表现为线程池饥饿）。预热入口刻意放在 Core 的契约上
（`IPermissionChecker.EnsureResolvedAsync` / `IObjectScopeAuthorizer.EnsureResolvedAsync`），
Osba 不引用引擎也能做到。

**回归护栏**：`EnsureAuthorized_Sync_Should_Warm_At_The_Entry` /
`ScopeAuthorization_Sync_Should_Warm_At_The_Entry`（`AuthorizationWarmupTests`），
钉住「同步入口在进入判定前预热一次」。

---

## 2. 已知边界与取舍

这些是**有意接受**的限制，不是待办事项。

### 2.1 后置检查不是预提交校验

读侧与 criteria 入口（`UpdateAsync(criteria)` 等）的目标对象在调用前是空对象，
范围列尚未赋值——此时判定会误杀一切。因此这些入口的判定在业务方法**返回之后**执行。

**例外：`Create` / `CreateAsync` 不做判定**。它们只构造对象、不落库，且按设计由调用方随后填充字段
（框架自带的 `User.CreateAsync` 也只填 `Username`，其余字段由 `.Handle(...)` 补）。
在这种「按设计就不完整」的时刻判定，只会误杀正常流程，却保护不了任何东西——
真正需要拦截的落库发生在 `SaveAsync`（新增）与 `InsertAsync`。

若业务方法内部已经落库，后置检查阻止的是「越权对象返回给调用方」，而**不是「越权数据写入」**。
真正的预提交强制需要持久化层拦截器或数据库约束，Osba 不提供。
范围列的「搬迁」（把 `TeamId` 改到无权团队）能发现并抛出，但无法阻止已发生的写入。

### 2.2 删除路径默认跳过的是「验证规则」，与权限无关

`EditableObject<T>` 在 `IsDeleted` 时默认不跑对象级规则。这只影响**验证线**：
越权删除照样由工厂边界的 `ScopeAuthorization.EnsureAuthorizedBefore` 拦下并抛 `SecurityException`。
换言之，删除路径**没有**任何权限上的例外。

需要让**验证**规则也覆盖删除时有两条路：调用方改用 `MarkAsDeleted(true)`，或走执行器时加
`.WithRuleChecksOnDelete()`。（`CheckObjectRulesOnDelete` 是只读属性，**无法重写**；
它由 `MarkAsDeleted` 的入参驱动。）
两条路径都有断言钉住（`ActuatorRuleTests` / `UserGeneralBusinessTests`），
验证线行为变化会让测试转红；越权形态由 `ScopeTests` / `ScopeRowPermissionTests` 钉住。

---

## 3. 回归护栏

以下测试钉住上面每一条决策。改动若让它们转红，说明决策被无意推翻了：

| 测试 | 钉住的决策 |
|---|---|
| `SaveAsync_Update_OutOfScope_ShouldFailWithSecurityException` + `SaveAsync_OutOfScope_OnDelete_ShouldFailWithSecurityException` | §1.2 越权形态一致（都抛 `SecurityException`） |
| `PermissionLineIndependenceTests` 三例 | §1.2 权限线不受规则通道影响 |
| `SaveAsync_WithRequirementsButNoBusinessContext_ShouldFailInsteadOfBypassing` + `SaveAsync_ModeledTypeWithoutBusinessContext_ShouldFailInsteadOfBypassing` | §1.1 无法判定即失败 |
| `ActuatorRuleTests` / `UserGeneralBusinessTests` | §2.2 验证线行为（删除路径默认不跑规则） |
| `ScopeTests` / `ScopeRowPermissionTests` | §1.2 越权形态、§2.2 删除路径无权限例外 |
| `Euonia.Osba.Standalone.Tests`（整个项目） | §1.3 Osba 不依赖引擎：只引用 `Euonia.Osba` 即可用权限；`Osba_Assembly_Should_Not_Reference_Security` 连间接引用一起守 |
| `PermissionScanScopeTests` | §1.3 扫描口径与工厂查找同源（含有意收窄） |
| `ObjectPermissionOptInTests.AddBusinessObject_Alone_Should_Not_Register_Permission_Engine` | §1.3 `AddBusinessObject` 不注册任何权限服务（含三个契约） |

---

## 4. 与引擎的对应关系

| 本文的决策 | 引擎侧的对应物 |
|---|---|
| §1.1 无法判定即失败 | `IScopeSubjectResolver` 缺席时 `IScopeGuard` 拒绝；首次解析 `IScopeGuard` 时的启动校验 |
| §1.2 越权一律 `SecurityException` | 引擎侧的 `IScopeGuard` / `IPermissionChecker` 只返回结论，形态由本库的 `ObjectAuthorization` / `ScopeAuthorization` 决定 |
| §1.3 权限契约 | 四个跨边界契约都在 Core：`IPermissionCodeSource` / `IPermissionChecker`（引擎与 Osba 各自实现一半）、`IObjectScopeAuthorizer` ↔ `IScopeGuard`（引擎实现）、`IObjectOperationResolver` ↔ `ScopeOperationMap`（Osba 实现）。任一都可替换为宿主自己的实现 |
| §2.1 后置检查 | `AllowsOperation` 的调用时机由本库决定 |
| §2.2 删除路径 | `ScopeOperationMap` 把删除状态映射为 `BusinessOperation.Delete`（本库公开的类型，引擎经 `IObjectOperationResolver` 消费它） |
