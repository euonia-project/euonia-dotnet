# Euonia.Osba 权限体系设计说明

> 面向**维护者与评审者**的设计决策记录。使用说明见 [README.md](README.md)。

本文记录「为什么这样设计」，以及**被否决的方案**与**已知边界**。每条决策都给出它所解决的问题；
若没有那个问题，该决策就不成立，可以重新评估。

---

## 0. 两条主线

权限体系分成两个互相独立又共享底座的部分：

| | 操作权限 | 数据权限 |
|---|---|---|
| 回答 | 当前用户**能否执行某项操作** | 当前用户**能看到/操作哪些数据行** |
| 粒度 | 类型级（`[Permission]`）+ 行级（按权限码的策略） | 行级 |
| 强制点 | `BusinessObjectFactory` 调用边界 | 查询下推 + 工厂保存边界 |

二者共享同一份**授权数据**（`ScopeSubjectSet`）与同一套**表达式引擎**，
由 `IScopeSubjectResolver` 一次解析、`IScopeGuard` 按请求缓存。

---

## 1. 核心决策

### 1.1 权限码是策略的键

**问题**：`[Permission]` 是类型级的——同一用户对同一类型的**所有行**权限相同。
真实场景需要「仓库 A1 可 push+delete、A2 仅可 push」。

**决策**：把数据权限的维度机制推广一层，`Grant(dimension)` 的语义从
「用户在该维度被授予的值」变为「用户**在该权限码下**于该维度被授予的值」。
行级操作权限因此成为「该码下的策略」，不需要引入任何新概念。

**收益**：行级权限直接获得表达式引擎的全部能力——可下推、可 deny、可审计、可组合。

**被否决的方案**：

| 方案 | 否决理由 |
|---|---|
| 行实现 ACL 接口（`IRowRights.GetRights(user)`） | 返回的是委托而非表达式，**无法下推**，列表查询做不了行级操作权限过滤；且与现有引擎是两套语义 |
| `Dictionary<code, ScopeSubjectSet>`（码级整体隔离） | 会让 `Self()`/`owner` 维度在码级集合存在时**静默失效**——本人删自己的数据被拒，且极难排查 |

### 1.2 操作权限不来自 Claims

**问题**：把权限码放在令牌的 `"perm"` 声明里有两个实际风险：

1. 权限码数量可能很大，**撑爆 Token**；
2. 更严重的是**取消授权后，旧令牌在过期前一直有效**——撤销不生效。

**决策**：权限码改由 `IScopeSubjectResolver` 从授权数据实时解析，随 `IScopeGuard` 按请求缓存。
`ClaimPermissionChecker` 保留但标记 `[Obsolete]`、不再是默认实现。

**收益**：撤销只需改数据，下一次解析即生效，**不需要重新签发令牌**。
测试 `RevokedPermission_ShouldTakeEffectWithoutReissuingToken` 用一个
**不含任何 `perm` 声明的同一个 `ClaimsPrincipal`** 钉住这一点。

**保留的部分**：**角色仍来自声明**。角色数量少而稳定，不构成令牌膨胀；
且 `ClaimsIdentity.IsInRole` 是 BCL 能力。细粒度授权一律走权限码——README 明确禁止用角色承载。

### 1.3 单一真值来源：只有一对表达式

**问题**：数据权限有两个落点——查询过滤（排除行）与写侧判定（拒绝保存）。
若各写一套判定逻辑，二者必然漂移：查询放过了某行，写侧却拒绝；或反之，
**后者是安全漏洞**。这正是重写前那套设计最致命的问题（它只有内存过滤，
真正的 DB 过滤交给使用方手写 `IN (...)`，语义完全不受框架约束）。

**决策**：策略**只编译成一对表达式**，不存在第二套判定逻辑：

```
Allows(resource) ≡ Allow(resource) && !Deny(resource)
query            ≡ source.Where(Allow).Where(!Deny)
```

`Allows` 把**同一个** `Allow`/`Deny` 表达式 `Compile()` 后求值，下推则直接搬运表达式树。
两者在数学上不可能给出不同结论。回归护栏见
`ScopeTests.Pushdown_And_InMemoryEvaluation_ShouldAgree`。

**推论**：用户被授予的值在编译期被**烘进表达式**（`ids.Contains(x.DeptId)`），
因此编译结果与当次授权数据绑定——这正是「按请求缓存」这一设计的由来。

### 1.4 allow/deny 代数

每个 `ScopePolicy<T>` 归约为三元组 `(Allow, Deny, HasAllow)`：

| 策略 | Allow | Deny | HasAllow |
|---|---|---|---|
| `Grant(d)` | `ids.Contains(x.D)` | `false` | true |
| `Self()` / `Where(p)` | 同 `Grant(owner)` / `p` | `false` | true |
| `Deny(p)` | — | `p.Allow ∨ p.Deny` | **false** |
| `All(p…)` | `∧{pᵢ.Allow \| pᵢ.HasAllow}`（全无则 `true`） | `∨{pᵢ.Deny}` | 任一 |
| `Any(p…)` | `∨{pᵢ.Allow \| pᵢ.HasAllow}`（全无则 `false`） | `∨{pᵢ.Deny}` | 任一 |

**`HasAllow` 标志不可省**。若把 `Deny(p)` 的 Allow 记作 `true`，则
`Any(Grant("dept"), Deny(secret))` 会退化成 `Allow = true` ⇒「除机密外全放行」——
一个静默提权。用 `HasAllow` 把 deny-only 分支从 `Any` 的「或」里排除，才得到预期的
`dept ∧ ¬secret`。回归用例：`ScopeTests.Any_WithDenyOnlyBranch_ShouldNotDegradeToAllowAll`。

**Deny 一律上浮**（`All` 与 `Any` 同规则），等价于防火墙式「拒绝优先」。
代价是表达力稍弱（`Any` 分支里的 deny 也作用于整体），换来的是保守的安全默认。
这是**有意的取舍**，不是实现疏忽。

**`Deny` 是否决，不是布尔取反**。需要取反请用 `Where(x => !...)`。
`Deny(Deny(p))` 无意义，构造期直接抛异常拒绝。

### 1.5 层级在解析期展开

**问题**：部门树、组织树是常见需求，但框架若理解层级就无法下推成 `IN (...)`。

**决策**：层级由 `IScopeSubjectResolver` 在解析期展开为**扁平集合**，框架只看到集合。
判定与下推因此永远只是集合成员判断。

**代价**：解析器要做一次闭包展开，且授予集合可能很大（见 §2.3）。

### 1.6 保留命名空间与覆盖语义

三条硬规则，每条都对应一个具体的错误：

| 规则 | 不这样做会怎样 |
|---|---|
| 保留前缀 `@`（`@default`/`@read`/…），不用字面量 `*` 作通配键 | `*` 在本仓已表示「权限码前缀通配」、也曾表示「维度值通配」。再加第三个含义会让排障变成猜谜 |
| 码级授予**覆盖**默认键，不做并集 | 默认授予会把某码上被收窄的行集合重新撑开，行级差异失效。用例：`RowLevel_CodeGrantShouldOverrideDefault_NotUnion` |
| 通配**不参与**维度查找 | 持有 `repo:*` 会顺带拿到 `(repo:*, repo)` 的授予，管理员无法把某个具体码收窄 |

### 1.7 策略键只由操作决定

**问题**：键若受调用方状态影响，就可能把同一变更路由到更宽松的键。

**决策**：键只由**操作**决定，操作只由 `ObjectEditState → BusinessOperation` 这**一条**映射
（`ScopeOperationMap`）决定。工厂、规则、`CanXObject()` 共用它——三处各写一遍必然漂移，
而「规则按 Update 判、工厂按 Create 判」会让规则静默失效。

`MarkAsNew/Changed/Deleted` 是公开方法，但它改变的是**实际执行的操作**，
每个操作各自应用自己的策略，不存在越权通道。

同一操作若解析出多个「声明了策略」的权限码 → 启动期失败，不允许靠猜。

### 1.8 规则是补充信号，不是强制点

**问题**：需要一个「以表单错误形式呈现」的通道，而不是让每个越权都变成异常。

**决策**：`PermissionRule` / `ScopePolicyRule` 把权限失败表达为 `BrokenRules` →
`ValidationException`；对已声明模型的类型**自动注入** `ScopePolicyRule`（消除「漏加规则」）。

**但强制点仍在工厂边界**，因为规则可被绕过：

- `SuspendRuleChecking()` / `BypassRuleChecks` 能跳过规则；
- 对象级规则只在 `EditableObject.SaveAsync` 执行，且 `IsDeleted` 时**默认跳过**；
- 规则抛不出 `SecurityException`（`Rules.RunAsync` 把所有异常转成错误）。

**规则实例是进程级、按类型共享的单例**（`RuleManager` 是静态字典）。
因此注入的规则被刻意设计成**无状态桥**：只持有资源类型，运行期从
`context.Target → BusinessContext` 解析一切。任何把注册表/守卫存进字段的写法都会
导致跨容器串味。注入逻辑**绝不抛异常**——它在属性 setter 上，抛出会把配置问题伪装成难定位的异常。

---

## 2. 已知边界与取舍

这些是**有意接受**的限制，不是待办事项。使用方需要知道它们。

### 2.1 后置检查不是预提交校验

读侧与 criteria 入口（`UpdateAsync(criteria)` 等）的目标对象在调用前是空对象，
范围列尚未赋值——此时判定会误杀一切。因此这些入口的判定在业务方法**返回之后**执行。

若业务方法内部已经落库，后置检查阻止的是「越权对象返回给调用方」，而**不是「越权数据写入」**。
真正的预提交强制需要持久化层拦截器或数据库约束，Osba 不提供。
范围列的「搬迁」（把 `TeamId` 改到无权团队）能发现并抛出，但无法阻止已发生的写入。

### 2.2 删除路径的失败形态不对称

`EditableObject<T>` 在 `IsDeleted` 时默认跳过对象级规则，因此：

| 场景 | 结果 |
|---|---|
| 越权新增/更新 | `ValidationException`（规则先命中） |
| 越权删除 | `SecurityException`（规则被跳过，工厂兜住） |

需要让规则覆盖删除时，重写 `CheckObjectRulesOnDelete` 返回 `true`。
两条路径都有断言钉住（`ScopeTests` / `ScopeRowPermissionTests`），行为变化会让测试转红。

### 2.3 行级 ACL 要求解析器做反向展开

行级策略的形式是 `Grant("repo")` ⇒ `repoIds.Contains(x.RepoId)`，
即解析器必须回答「此用户在此码下能碰**哪些资源 id**」。

若 ACL 是正向存储的（「哪些用户能 push 到 A1」），解析器要把它翻成 id 列表，
得到的是巨型 `IN (...)` + 每请求成本；编译器目前只支持 `Constant(List<string>)` 形态，
无法下推子查询。

**守则**：授权尽量授「组 id」而非「行 id」；行数巨大时改用
`Where(x => aclQuery.Contains(x.Id))` 逃生舱并自行评估成本。

### 2.4 规则的进程级共享 vs 容器级注册表

`RuleManager` 是静态的而 `ScopeModelRegistry` 是按容器的。自动注入的**判定**
只在首次初始化该类型时发生一次。多容器且注册表不同的场景下，某容器可能带着另一容器的注入结果运行——
由于规则运行期会重新查注册表、未声明时放行，行为仍然正确，但这是个需要知晓的耦合。

### 2.5 角色仍受令牌时效约束

角色来自声明，因此**角色的撤销仍需等令牌过期**（这与权限码不同）。
所以：细粒度授权一律用权限码；角色只用于粗粒度、稳定的人员分类。

### 2.6 维度选择器的值类型固定为 `string`

`Expression<Func<T,string>>` 是 `IN` 下推最自然的载体，但真实系统里 `TeamId` 常为 `Guid`/`long`。
当前只能把列声明为字符串（或提供一个字符串投影）。扩展到泛型值类型会显著增加编译复杂度与校验面，
暂不做。

---

## 3. 被否决的方案

| 方案 | 否决理由 |
|---|---|
| **EF 全局查询过滤器**（`HasQueryFilter`/`SetQueryFilter`）做读侧强制 | EF 的模型（含全局过滤器）**按 DbContext 类型缓存**，而 `Allow`/`Deny` 捕获了每用户不同的集合常量，一旦烘进缓存模型就会被跨请求复用——**数据泄漏**。本仓既有的 `SetTombstoneQueryFilter` 之所以安全，只是因为它过滤的是常量 `!IsDeleted` |
| **通用表达式 DSL**（策略写成字符串再解析，类似 Rego） | 会引入解析器 + 求值器 + 翻译器三份实现，正是本设计要消灭的漂移源 |
| **同步的解析器接口** | 解析器必然查库；同步签名会把同步 I/O 带进请求链路。用 `ValueTask` + 单次解析已足够 |
| **引用 `Euonia.Linq` 复用表达式组合子** | 其承重的 `ParameterRebinder` 是 `internal`，外部用不上；且 `Source/` 下没有任何项目在用该组合子。为三个方法引入 ProjectReference 不划算，改为 25 行的内部参数替换 + body 层归并 |
| **在 `AddBusinessObject` 里直接检查解析器是否已注册** | 解析器通常在该调用之后才注册，在那里检查会误报。改为记录 `PermissionSetup`，由容器构建后的 `ValidatePermissionSetup()` 检查 |
| **删除 `ClaimPermissionChecker`** | 它是已发布的公开 API。改为保留 + `[Obsolete]` + 不再是默认实现——同等达成「默认路径不依赖令牌」，且不破坏使用方 |

---

## 4. 回归护栏

以下测试不是覆盖率填充，而是**钉住上面每一条决策**。改动若让它们转红，说明决策被无意推翻了：

| 测试 | 钉住的决策 |
|---|---|
| `Pushdown_And_InMemoryEvaluation_ShouldAgree` | §1.3 单一真值来源 |
| `Apply_ShouldProduceExpressionBasedWhere_NotEnumerableWhere` | §1.3 可下推（是表达式而非委托） |
| `Any_WithDenyOnlyBranch_ShouldNotDegradeToAllowAll` | §1.4 `HasAllow` 不可省 |
| `Any_OfOnlyDenyBranches_ShouldDenyEverything` | §1.4 fail-closed |
| `Deny_ShouldOverrideGrant_EvenInNestedAny` | §1.4 deny 上浮 |
| `EmptySubjects_ShouldProduceConstantFalse_NotEmptyIn` | §1.4 不生成空 `IN ()` |
| `RowLevel_SameUserSameType_DifferentRowsDifferentRights` | §1.1 行级权限 |
| `RowLevel_CodeGrantShouldOverrideDefault_NotUnion` | §1.6 覆盖而非并集 |
| `RevokedPermission_ShouldTakeEffectWithoutReissuingToken` | §1.2 撤销立即生效 |
| `Permissions_ShouldComeFromResolver_NotClaims` | §1.2 权限码不来自令牌 |
| `AutoInjectedScopeRule_ShouldFailUpdateWithValidationError` + `SaveAsync_OutOfScope_OnDelete_ShouldFailWithSecurityException` | §2.2 删除路径不对称 |
| `ValidatePermissionSetup_ShouldFailWhenResolverMissing` | §3 启动期校验 |
| `PolicySet_ShouldRejectReservedPermissionCode` / `PolicySet_ShouldAllowFrameworkDefaultKeys` | §1.6 保留命名空间 |
| `ScopeKeys` 相关的 `ValidateKeyResolution` 启动校验 | §1.7 键歧义即失败 |
