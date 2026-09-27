# Euonia.Osba 权限接线设计说明

> 面向**维护者与评审者**的设计决策记录。本文只记录**「引擎与宿主之间」**的决策：
> 策略引擎自身的语义与取舍见 [`Euonia.Security/DESIGN.md`](../Euonia.Security/DESIGN.md)，
> 使用说明见 [PERMISSION.md](PERMISSION.md)。

`Euonia.Security` 只回答「能不能」，**不拦截任何调用**。因此「在哪里裁决、以什么异常形态裁决」
必然是宿主框架的决定。以下四条决策就是关于这个接缝的。

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

**注意**：`BusinessObject.CanUpdateObject()` 之类的虚方法**仍是查询**（无从判定时返回
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
在调用侧显式捕获 `SecurityException`，或先用 `CanAccessRow` / `CheckPermissionAsync` 查再跳。
这是把一条**静默的口头约定**换成**显式的 API**。

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

---

## 4. 与引擎的对应关系

| 本文的决策 | 引擎侧的对应物 |
|---|---|
| §1.1 无法判定即失败 | `IScopeSubjectResolver` 缺席时 `IScopeGuard` 拒绝；`PermissionSetup` + `ValidatePermissionSetup()` |
| §1.2 越权一律 `SecurityException` | `IScopeGuard` / `IPermissionChecker` 只返回结论，形态由本库的 `ObjectAuthorization` / `ScopeAuthorization` 决定 |
| §2.1 后置检查 | `IScopeGuard.AllowsObject` 的调用时机由本库决定 |
| §2.2 删除路径 | `ScopeOperationMap` 把删除状态映射为 `BusinessOperation.Delete` |
