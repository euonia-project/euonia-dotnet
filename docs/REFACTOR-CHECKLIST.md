# Euonia.Osba + Euonia.Security 重构清单

> 本文件是重构工作的**唯一事实来源**。所有批次执行前后都在此勾选，不依赖对话记忆。
>
> - 生成时间：2026-09-29
> - 生成方式：两份独立只读 sweep（`Source/Euonia.Osba/`、`Source/Euonia.Security/` + `Source/Euonia.Core/Security/`），HEAD `37f4ea1`，工作区干净
> - 范围：允许破坏性变更（需逐项标注 `BREAKING`）

---

## 0. 执行护栏（每次开工前读一遍）

| # | 规则 |
|---|---|
| G1 | 全量测试：`dotnet test Euonia.Test.slnx`。**不要加 `--nologo`**（xunit.v3 MTP 不认该选项 → "Zero tests ran"）。单测：`dotnet test <Proj.csproj> --filter "FullyQualifiedName~X"` |
| G2 | 测试基线：**total 902 / succeeded 889 / failed 0 / skipped 13** |
| G3 | 仓库**没有 `rg`**，用 `grep -rn --include='*.cs'`，避免扫 `bin/obj` |
| G4 | **不要删除空代码块**（用于阻止 IDE 无效代码分析）。7 处：5 处带 `// 空块：用于阻止 IDE 代码分析建议（勿删除）` 注释，2 处待补注释（见 **O-17**） |
| G5 | 用户可能并发 `git commit` 覆盖未提交编辑 → **每改完一个文件立即重读校验**，不要攒批提交 |
| G6 | 回退验证法：`cp` 备份 `/tmp/*.fixed.cs` → 精确替换还原 → 测试转红 → `cp` 回来。**不要**对含既有改动的文件直接 `git checkout` |
| G7 | `Resources.Designer.cs` 由 `Source/resource.props` 的 `GenerateResx` target **构建时自动再生**，只改 `.resx`；多 TFM 并行可能争抢 `Properties/Resources.resources` 导致 `MSB3554`（重跑一次即可） |
| G8 | `O-15` 的 `ConvertIfStatementToSwitchStatement` 抑制**不可删**（两段 `if` 必须各自独立执行） |
| G9 | 每批结束后：跑全量 → 报告 → **停下等用户确认**，再进下一批 |

---

## 1. 已完成（历史，勿重做）

### Phase 0 / 1 / 2
- [x] Phase 0.1、0.2 —— 脚手架与基线
- [x] Phase 1.1–1.11 —— Core/Osba/Security 各项（含 2.2 / 2.5 回退仍绿的契约护栏）
- [x] Phase 2.1–2.6

### Phase 3 —— 旧清单 Batch 1（Osba，6/6）
- [x] O3、O6、O10、O14、O15
- [x] ~~O12（删除空代码块）~~ —— **已永久撤销**，用户指令 G4

### Phase 3 —— 旧清单 Batch 2（Security + Core，9/9）
- [x] S2、S5、S6、S7、S9、S11、S12、S13、S14

### Phase 3 —— 旧清单 Batch 3（5/5，已提交）
- [x] O5 —— `PropertyInfo.CreateInstance` 裸 `catch` → `catch (Exception ex) when (IsUnsupportedInstance(ex))`（`Reflection/PropertyInfo.cs:260-264`）
- [x] O8 —— `BusinessObjectFactory` 10 处 try/finally → `WithActivator` / `WithActivatorAsync`
- [x] O9 —— 新增 `FieldDataManager.GetExistingOrInit<TValue>(PropertyInfo<TValue>)`，替换 5 处 switch（commit `f96dd30`）
- [x] O13a —— `ObjectReflector` 3 处空异常消息补全
- [x] O13b —— 4 个 resx key 入 `Resources.resx` + `Resources.zh-CN.resx`，护栏 `Tests/Euonia.Osba.Tests/ResourceLocalizationRegressionTests.cs`（commit `01ec77e`；回退转红已验证）

### S10 —— `UserPrincipal.Username` 分支补齐
- [x] 加 `"JwtBearer"`、`_ => null` 兜底、合并 `Windows`/`Cookies`/`Cookie`（commit `37f4ea1`）
- [x] **护栏已恢复并验证**：`Tests/Euonia.Security.Tests/UserPrincipalRegressionTests.cs`（**9 条**；原 5 条曾被 `f96dd30` 覆盖丢失）。回退验证：还原 `_ => null` 且去掉 `JwtBearer` → **failed 3**

---

## 2. 新清单（26 项：P1×3 / P2×7 / P3×16，无 P0）

### 2.1 P1

#### S-1 · 操作名大小写不一致 → 权限门 fail-open
- [x] 状态：**Batch 5 已修（权限码/操作名忽略大小写）**。`OperationCodeSource.cs` 的 `_rules`、`Build()` 字典、`CodesFor.Distinct` 改 `OrdinalIgnoreCase`；`CompositeCodeSource.cs:19` 操作并集去重同改；`ObjectPermissionRequirementProvider.cs:55/:80` 同改。护栏 `Tests/Euonia.Security.Tests/OperationCaseInsensitivityTests.cs`（3 条）。回退验证 → **failed 3**
- **位置**：`Source/Euonia.Security/OperationCodeSource.cs:59`（未知操作返回 `[]`）、`:99`（`StringComparer.Ordinal`）、`Source/Euonia.Osba/Permission/ObjectAuthorization.cs:45`（零要求 → 放行）
- **证据**：配置写 `"Read"` → `_rules` 用 `Ordinal` → 运行时查询 `BusinessOperation.Read`（`"read"`）查不到 → `requirements.Count == 0` → **直接放行**，`[Permission]` 类级与方法级码全部跳过。启动校验 `ValidateKeyResolution`（`ScopeModelRegistryBuilder.cs:163`）遍历的是**配置的大小写**，因此也校验不出来。`ConfigurationRuleBinder.cs:50` 的 `OrdinalIgnoreCase` 重复守卫反而**拒绝**了 `read` + `Read` 并存（本来能用）的配置，违反 `ConfigurationRuleBinder.cs:16` 自身契约「读到的每一个字符都要么生效、要么报错」
- **测试缺口**：`Tests/Euonia.Security.Tests/PermissionRuleConfigurationTests.cs` 26 条全用小写，唯一 case 相关用例 `:245` 只测配置内部重复
- **修法（非破坏，推荐）**：`OperationCodeSource.cs:99`、`:209`（`Build()` 字典）、`:89`（`CodesFor` 的 `Distinct`）、`Source/Euonia.Osba/Permission/ObjectPermissionRequirementProvider.cs:80` 改 `StringComparer.OrdinalIgnoreCase`
- **替代**：保持 `Ordinal`，在 `ConfigurationRuleBinder.Bind` 拒绝只差大小写的已知操作码（至少 `BusinessOperation.All`）—— 回调 API `OperationCodeSource.cs:130` 仍暴露
- **补充**：`Source/Euonia.Security/README.md` §3.4 今日**完全没提大小写**
- **BREAKING**：否

#### S-2 · 冷缓存 `GetSubjects()` 同步阻塞（= 旧 S1）
- [x] 状态：**Batch 5 已修（预热方案，非直接异步化判定）**。全量 **926 / 913 / 0 / 13**；回退（删两处 `AuthorizationWarmup.WarmAsync`）→ **failed 4**
- **实现与原计划的一处偏离（关键）**：原计划想直接在 Osba 里 `await IScopeGuard.EnsureResolvedAsync`，但 `IScopeGuard` 在 **`Euonia.Security`**，而 `Euonia.Osba` **按设计不引用它**（`Euonia.Osba.csproj` 只引 Core/Modularity/Pipeline/Validation）。所以预热入口只能挂在 **`Euonia.Core` 的两个契约**上，由实现方决定预热做什么：
  1. `Source/Euonia.Core/Security/IPermissionChecker.cs` 新增 DIM `ValueTask EnsureResolvedAsync(CancellationToken)` → 默认**空操作**（非破坏，第三方实现零改动）
  2. `Source/Euonia.Core/Security/IObjectScopeAuthorizer.cs` 新增 DIM `ValueTask EnsureResolvedAsync(IServiceProvider scope, CancellationToken)` → 默认空操作；**带 `scope` 参数**，与 `Allows` 同口径（预热必须是判定时那一个作用域，不能退化成环境上下文）
  3. `SubjectPermissionChecker` / `ObjectScopeAuthorizer` 各自覆写 → `await _guard.EnsureResolvedAsync(ct)`，并**吞 `InvalidOperationException`**（与既有 `IsGrantedAsync`/`Holds` 同口径：解析器缺席交回同步判定 fail-closed，预热不改写行为）
  4. `Source/Euonia.Osba/Permission/AuthorizationWarmup.cs`（新，internal）：两个 `WarmAsync` 重载，只是转发契约
  5. `ObjectAuthorization`：`EnsureAuthorized` 拆成 `TryPrepare`（前置条件，out 出 checker）+ `Enforce`；新增 `EnsureAuthorizedAsync` = `TryPrepare → Warm → Enforce`
  6. `ScopeAuthorization`：同样拆 `TryPrepare`/`Enforce` + `EnsureAsync`，新增 `EnsureAuthorizedBeforeAsync` / `EnsureAuthorizedAfterAsync`
  7. `BusinessObjectFactory` 的 **8 个 `*Async` 入口**（`:105,121,126,138,143,155,160,179,182,190,202,205,227,232,251,256` 共 16 处调用）全部改 `await ...Async(...)`；无 ct 的入口传 `default`，有 ct 的（`SaveAsync`/`ExecuteAsync(target)`）传 `cancellationToken`
  8. `IScopeGuard` 补 DIM `GetSubjectsAsync`（`await EnsureResolvedAsync → GetSubjects`）—— 异步消费方的公共出口，**非破坏**
- **同步入口刻意不预热**（`Create`/`Fetch` 保持调 `EnsureAuthorized`/`EnsureAuthorizedAfter`）：同步契约里阻塞是既定语义，预热只会多一次异步调度
- **为什么是「预热」而不是「把判定改异步」**：判定是同步契约且**可被派生类重写**（`CanPerformOperation`/`CanReadObject`/…）；改成 async 会改写整条继承链的签名（破坏）。预热后同步判定命中暖路径，`AsyncContext.Run` 不再被触发
- **护栏**：`Tests/Euonia.Osba.Tests/AuthorizationWarmupTests.cs`（**6 条**：异步入口 warm==1、被拒也 warm==1、同步入口 warm==0、`obj.SaveAsync` 端到端 warm==1、Scope 异步 warm==1 **且作用域同一引用**、Scope 同步 warm==0）+ `Tests/Euonia.Security.Tests/GeneralityTests.cs` 的 `GetSubjectsAsync_Should_Return_The_Cached_Snapshot_Without_Blocking`（1 条，断言 `Assert.Same` 同一份快照）
- **遗留**：`ScopeGuard.cs` 的 `AsyncContext.Run` 仍在（**没删**，G2/G8 精神：不动 load-bearing 的同步出口）；4 个生产 `GetSubjects()` 调用点仍会走它——但它们都在**同步**方法里，语义不变。彻底消除需要把那 4 处也迁到 `GetSubjectsAsync`（可并入 S-6 的处理）
- **BREAKING**：否（全部为新增 DIM + internal 变体）

#### O-1 · `Regex` 无 matchTimeout，在 setter 同步路径上执行（ReDoS）
- [x] 状态：**不做（用户决定：不考虑超时）**。`new Regex(Expression)` 不加 matchTimeout，`:59` 的 `IsMatch` 保持在 setter 同步路径上执行。
- **位置**：`Source/Euonia.Osba/Rules/CommonRule.Regular.cs:24`、`:37`、`:59`
- **证据**：`_regex = new Regex(Expression);` 无超时 → 默认 `InfiniteMatchTimeout`。`ExecuteAsync`（`:51-70`）**没有 `await`**，`_regex.IsMatch` 同步跑在调用线程；调用链 `ObservableObject.SetProperty` → `BusinessObject.PropertyHasChanged`（`Core/BusinessObject.cs:427-430`）→ `CheckPropertyRules`（`:322`）→ `Rules.CheckRules`
- **公开 API**：`README.md:234`、`README.en.md:235`、`SAMPLE.md:189,387`、`Samples/.../Repository.cs:137`
- **修法（非破坏）**：`new Regex(Expression, RegexOptions.None, TimeSpan.FromSeconds(5))`（超时提为 `const` 或构造重载）；构造期已会因语法错误抛 `ArgumentException`，加超时与既有行为一致
- **BREAKING**：否

---

### 2.2 P2

#### S-3 · 5 处文档仍称 `ClaimPermissionChecker` 有 `[Obsolete]`
- [x] 状态：**Batch 6 已修（纯文档）**。5 处全部改为「保留、**无** `[Obsolete]`、非默认实现」：`Source/Euonia.Security/DESIGN.md:154`、`:433`（被否决方案表保留历史语义，只把现在时的「已标记 `[Obsolete]`」改为「当初计划、后来未采纳」）、`Source/Euonia.Osba/PERMISSION.md:224`、`:762`、`:971`。另按 §4 附带待办补 `Source/Euonia.Security/README.md` 新增 **§4.4「非默认回退：`ClaimPermissionChecker`」**（含 opt-in 代码片段 + 顺序无关 + 只需 `UserPrincipal` + 为何不推荐）。验证：`grep ClaimPermission --include=*.md Source/` 中 6 处 obsolete 提及全部为否定式（「没有 `[Obsolete]`」）
- **位置**：`DESIGN.md:154`、`DESIGN.md:433`、`PERMISSION.md:224`、`PERMISSION.md:761`、`PERMISSION.md:970`（共 **5 处**，非此前记录的 3 处）
- **事实**：`35b9c6d`（2026-09-29 08:51）删掉了唯一那行 `[Obsolete]`；`grep '\[Obsolete'` 在 `Source/Euonia.Security/*.cs` 返回**零**；类**零**注册点；默认实现是 `ServiceCollectionExtensions.cs:231` `TryAddScoped<IPermissionChecker, SubjectPermissionChecker>()`；`Rebuild`（`:258-297`）从不碰 `IPermissionChecker` → 宿主在 `AddPermission` **之后** `AddScoped<IPermissionChecker, ClaimPermissionChecker>()` 即可生效（MS DI 取最后一个描述符），只需 `UserPrincipal`
- **代码 XML 文档 `ClaimPermissionChecker.cs:11-13` 是准确的**，无需改
- **修法**：5 处改为「保留、非默认实现、无 `[Obsolete]` 警告，文档标注为不推荐」；另在 `Source/Euonia.Security/README.md` §4 补一句确切 opt-in（含顺序无关 + 需 `UserPrincipal`）
- **保留不动**：`DESIGN.md:433` 若位于「被否决的方案」表中，其历史记录语义可保留，但须改掉「已标记 `[Obsolete]`」的现在时表述
- **BREAKING**：否

#### S-4 · `AddPermission` 省略 `assemblies` → 行级权限静默失效 + 校验器短路
- [x] 状态：**Batch 6 已修（镜像 `EmptyCodeSource` 规则）**。护栏 4 条，回退删掉新检查 → **failed 1**。全量 **931 / 918 / 0 / 13**
- **实现**（**校验期**而非注册期——注册期会打断 `AddPermission(source)` 与 `AddPermissionModels(asm)` 的合法先后顺序，且现有测试/文档布局依赖该顺序）：
  1. `PermissionModelSetup.NoModelsAsserted`（新增可写属性）记「已显式断言无模型」
  2. `ServiceCollectionExtensions.AssertNoPermissionModels()`（**新增 public 扩展**）置位
  3. `ServiceProviderExtensions.ValidatePermissionSetup()` 在 `setup == null` 早退**之后**、`RequiresSubjectResolver != true` 短路**之前**插入检查：`!NoModelsAsserted && Assemblies.Count == 0` → 抛 `InvalidOperationException`（消息含修法与 `AssertNoPermissionModels` 名）
  4. 位置很关键：零程序集时 `RequiresSubjectResolver` **恒为 false**，检查若放在短路之后就永远执行不到
- **护栏**：`Tests/Euonia.Security.Tests/AddPermissionTests.cs` 新增 4 条（零程序集报错、显式断言放行、断言在 `AddPermission` 之前也生效、`AddPermissionModels` 后无需断言）
- **不破坏**：`CleanAssembly` 扫描（扫过但无声明）不算零程序集；`PermissionSetup` 公开构造器未动
- **文档**：`Source/Euonia.Security/README.md` §5.6 补零程序集诊断 + 代码片段；`ServiceProviderExtensions` XML `<exception>` 同步
- **位置**：`Source/Euonia.Security/ServiceCollectionExtensions.cs:293`（`PermissionSetup(setup.HasDeclarations(registry))`）、`Source/Euonia.Security/ServiceProviderExtensions.cs:35`（校验器短路）、`Source/Euonia.Security/Scope/ScopeAuthorization.cs:73`（行级门短路）
- **证据**：三个 `AddPermission` 重载（`:48`、`:107`、`:166`）都接受 `params Assembly[] assemblies`。省略 → `AddFrom([])` → `HasDeclarations == false` → `RequiresSubjectResolver == false` → `ValidatePermissionSetup` 直接 return，从未检查 `IScopeSubjectResolver` / `UserPrincipal`；运行时行级门 `IsConstrained` 为 false → **行级数据权限根本不生效**，而 `ObjectAuthorization` 仍跑并 fail-closed（`SubjectPermissionChecker.Holds` `:113-116`）
- **违反**：`ServiceProviderExtensions.cs:16-18` 自己的承诺
- **既有先例**：代码源已有 `EmptyCodeSource` 显式断言（`ServiceCollectionExtensions.cs:150-153`，消息 `:152`；规则见 `ConfigurationRuleBinder.cs:93`、`OperationCodeSource.cs:202-203`）
- **修法**：零程序集时**要求显式 opt-out**（`PermissionSetup` 加「已确认无权限模型」标志），否则 `AddPermission` 注册阶段抛出带节点路径的错误
- **BREAKING**：否（只把静默变成报错；此前"能启动但静默失效"的场景本就是缺陷）

#### S-5 · `ValidateKeyResolution` 用 `Ordinal` vs `ScopePolicySet` 用 `OrdinalIgnoreCase` → 误报启动错误
- [x] 状态：**Batch 5 已修**。`ScopeModelRegistryBuilder.cs:165` 改 `OrdinalIgnoreCase`；护栏 = 把 `GuardedAssetModel.Declare` 改成与 `[Permission("guarded:run")]` 只差大小写的 `"Guarded:Run"`（fixture 内已加注释说明是刻意的）。回退验证 → **Security 套件大面积失败**（该 fixture 被几十个用例引用，覆盖极强）
- **位置**：`Source/Euonia.Security/Scope/ScopeModelRegistryBuilder.cs:165`（`new HashSet<string>(StringComparer.Ordinal)`）
- **证据**：`[Permission("repo:push")]` + `Declare("Repo:Push")` 时，键解析**确实**匹配（`ScopeKeyResolver.cs:54-56` → `ScopeModelRegistration.cs:44` → `ScopeModel.cs:67` → `ScopePolicySet.cs:20` 的 `OrdinalIgnoreCase`），`resolved` 收到 `"repo:push"`；随后 `DeclaredCodes` 返回 `"Repo:Push"`，`resolved.Contains` 在 `Ordinal` 下为 false → 启动死亡，消息「没有任何操作会解析到该码」，而紧邻列出的却是 `repo:push`。**fail-closed**（非安全洞），但诊断完全误导
- **修法（非破坏，推荐）**：`:165` 改 `StringComparer.OrdinalIgnoreCase`（一个词）；或在模型边界规范化 `DeclaredCodes`
- **BREAKING**：否

#### O-2 · `BypassRuleChecks` 对象级 bool 且跨 `await` 持有
- [x] 状态：**Phase 4 已实施（AsyncLocal，BREAKING）**。`BusinessObject.IsBypassingRuleChecks` 改为挂在本实例 `AsyncLocal<bool>` 容器上的流作用域开关（`get` 读当前流值、`set` 写当前流值；未置位时读侧零分配）；`BypassRuleChecksObject` 构造时**捕获进入前旧值**、`DeRef` 释放时还原捕获值（嵌套 `using` 退出不再关闭外层绕过）。行为变化：① 绕过不再外溢到并发流（并发 setter 的变更追踪/通知恢复正常，原「静默不持久化」缺口关闭）；② 派生流仍继承标志（与原对象级语义兼容）；③ 派生类可覆写属性整体禁用绕过。护栏 `Tests/Euonia.Osba.Tests/ReadOnlyObjectRegressionTests.cs` 新增 `BypassRuleChecksFlowScopeTests` 3 条（并发流不外溢 / 嵌套还原捕获值 / 派生流继承 + using 退出还原；探针继承 `ObservableObject` —— 只读探针写不进 `ChangedProperties` 会让握手 TCS 永不置位导致用例挂起，已注记）。既有 `Concurrent_Bypass_Blocks_Should_Restore_The_Target_Completely` 等全部保持绿。
- **位置**：`Source/Euonia.Osba/Rules/RulesExtensions.cs:26,51,76,98`（`using (target.BypassRuleChecks) { return await handler(target); }`）、`Source/Euonia.Osba/Core/BusinessObject.cs:499`（`protected virtual bool IsBypassingRuleChecks { get; set; }`，`:518-522` 置位 / `:611` 释放）
- **危害（标志为 true 期间）**：写权限检查跳过（`ObservableObject.cs:336,392,443,471,497`）、`OnPropertyChanging` 跳过（`:364,420,502`）、**`PropertyHasChanged` 跳过（`:370,426,509`）**、读权限检查跳过（`:182,249,264`）
  1. **静默不持久化**：`PropertyHasChanged` 是唯一写入 `_changedProperties` 的地方（`BusinessObject.cs:417-423`），XML 文档 `:341` 声明「持久化只取 ChangedProperties」→ handler 写入的值不入库、不通知；若这是唯一变更则 `SaveAsync(isChanged:false)` 整行跳过
  2. **跨线程外溢**：标志在对象上跨 `await` 存活 → 异步 handler 执行期间同实例的**并发** setter 也一并跳过权限/变更追踪/通知
- **修法（BREAKING）**：`IsBypassingRuleChecks` 改 `AsyncLocal<bool>` / 线程（逻辑流）作用域；同步与异步重载均保持 `using` 包裹语义，但不再外溢到并发调用方
- **影响面**：依赖「对象级绕过」语义的既有规则行为会变 —— 执行前先 `grep -rn "BypassRuleChecks" Source/ Tests/ Samples/` 盘点
- **BREAKING**：**是**

#### O-3 · `DistinctBy(OrdinalIgnoreCase)` vs `ClearRules` 序数 → 规则永不清理（= 旧 O11）
- [x] 状态：**Batch 4 已修**。删掉 `Rules.cs:442` 的比较器参数并加注释；护栏 `Tests/Euonia.Osba.Tests/BrokenRuleCaseSensitivityTests.cs`（1 条）。回退验证：还原 `StringComparer.OrdinalIgnoreCase` → **failed 1，Actual: 3**（跨轮累积）
- **位置**：`Source/Euonia.Osba/Rules/Rules.cs:442` vs `Rules/BrokenRuleCollection.cs:63`
- **证据**：`:442` `DistinctBy(property => property.Name, StringComparer.OrdinalIgnoreCase)`；`:63` `if (rule.Property != propertyName)`（`string !=` → **序数**）。`:438` `ClearRules(null)` 最终只清 `Property == null` 的条目，故 `:444` 的定向清理是属性级条目唯一的删除路径。注册是**大小写敏感**的（`Reflection/PropertyComparer.cs:16` 用 `StringComparer.InvariantCulture`）→ `Name` 与 `name` 可同时注册 → `DistinctBy` 只留第一个 → 第二个属性的陈旧条目跨轮累积（正是 `:431-432` 注释警告的缺陷）→ `ErrorCount > 0` → `IsValid` 永远 false
- **修复引入点**：`31496a0`
- **测试缺口**：`grep ClearRules|DistinctBy Tests/` → **零命中**
- **修法（非破坏，推荐）**：删掉 `:442` 的第二个参数（默认序数，与 `:63` 一致），或整段删 `DistinctBy`
- **不可做**：把 `ClearRules(string)` 改成忽略大小写 → **破坏**公共可观测行为
- **BREAKING**：否

#### O-4 · 规则引擎 6 处阻塞等待
- [x] 状态：**Phase 4 已按方案 B 实施（非破坏）**。同步入口保留（`CheckObjectRules` / `CheckRules(IPropertyInfo)` 文档标注「同步契约的固有阻塞」并指向异步对等物）；`BusinessObject` 新增 `internal CheckPropertyRulesAsync(IPropertyInfo, CancellationToken)`——`Rules.CheckRulesAsync(IPropertyInfo)` 的公共出口，含 I/O 的属性级校验在异步流程里 `await` 而不阻塞线程池；保存/命令执行/`ValidateAsync` 本就走异步内核（`CheckObjectRulesAsync`），生产代码的真实阻塞只剩「setter 热路径上的同步纯校验」这一文档化契约。方案 A（fail-fast）维持否决。
- **位置**（**穷举，无遗漏**）：`Rules/Rules.cs:340`、`:356`、`:485`、`:569`；`Factory/BusinessObjectFactory.cs:54`、`:80`
- **证据**：项目内 `.Result` = 0、`.Wait()` = 0、`Thread.Sleep` = 0；30 个 `lock (` 站点与 11 个 `async` 声明**交集为空**，`lock` 内无 `await`，无 `async void`
- **为何仍是问题**：`:336-341` 的死锁守卫只在 `SynchronizationContext.Current != null` 时换线程池；ASP.NET Core / 控制台下为 null → `RunObjectRules` **内联**跑在调用线程，`Task.WaitAll` 阻塞线程池线程直至所有异步属性规则完成 → 并发下线程池饥饿
- **已知性**：`:556-559` 已文档化「属性 setter 上会阻塞调用线程——这是同步 API 的固有代价」
- **本次核查的两处校正（回滚不影响其有效性）**：① 生产真实阻塞仅 **2 处** —— 属性 setter 热路径 `Core/BusinessObject.cs:429` → `Rules.CheckRules`、同步工厂 `AsyncContext.Run`（`BusinessObjectFactory.cs:54`/`:80`）；`CheckObjectRules` / `RunObjectRules` 生产代码**零调用**。② 异步入口**早已存在并已接线**：`CheckObjectRulesAsync`（`:374`，public）、`CheckRulesAsync(IPropertyInfo, CancellationToken)`（`:506`，`internal`），用于保存 / 命令执行 —— 原推荐的「暴露 `CheckRulesAsync`」不需要再做
- **候选修法 A（曾实现、已回滚）**：fail-fast —— `!task.IsCompleted` 当场抛 `InvalidOperationException`。实现要点：`private static void EnsureSynchronouslyCompleted(List<Task>, string entry, string subject)` 在 `RunObjectRules` / `RunPropertyRules` 调用；护栏 `SynchronousRuleFailFastTests`（4 条）+ 改写既有死锁测试（`Assert.Null(failure)` → `Assert.IsType<InvalidOperationException>`）。回退验证：删两处调用 → failed 1（对象级路径表现为挂起）。**BREAKING**
- **候选修法 B（原推荐，非破坏）**：暴露真正异步的 `CheckRulesAsync` 路径（异步内核 `RunAsync` 已存在），`BusinessObject.CheckPropertyRules` 在调用方已是异步时选用
- **BREAKING**：修法 A 是；修法 B 否
- **待办**：等用户重新决定走 A 还是 B，或维持现状

#### O-5 · 锁序反转 `_publishLock` ↔ `lock(type)`（ABBA）
- [x] 状态：**Batch 6 已修**。删掉 `FieldDataManager.ForceStaticFieldInit` 里的 `lock (type)` 并加注释（CLR 类型初始化锁已保证静态初始化只跑一次；那把锁是纯冗余且构成 ABBA 反转边）。护栏 `Tests/Euonia.Osba.Tests/FieldDataLockOrderingTests.cs`（1 条：测试线程持住 `type` monitor 后另起线程调 `ForceStaticFieldInit`，旧实现会卡到超时）。回退还原 `lock(type)` → **failed 1（3s 超时）**
- **位置**：`Reflection/PropertyInfoManager.cs:68`（`lock (_publishLock)` → `:83 ForceStaticFieldInit`）↔ `Reflection/FieldDataManager.cs:275`（`lock (type)`）
- **反向边**：`FieldDataManager.cs:48 ForceStaticFieldInit` → `:275 lock(type)` → 静态初始化 → `BusinessObject.RegisterProperty`（`Core/BusinessObject.cs:631`）→ `PropertyInfoManager.cs:150 GetPropertyListCache` → `:58 CreateAndPublish` → `:68 lock(_publishLock)`；以及 `FieldDataManager.cs:65 GetLockedSnapshot` → `CreateAndPublish`
- **可达性（诚实评估）**：因 `ForceStaticFieldInit` 遍历整条基类链（`:277-284`）且 CLR 类型初始化锁序列化静态构造，普通「两个线程首次触碰类型及其基类」竞态大多被串行化、不死锁。残留窗口 = 进入 `GetPropertyListCache(T)` 而另一线程正持有 `lock(T)`；入口是两个 **public** 方法 `PropertyInfoManager.GetRegisteredProperties(Type)`（`:108`）和 `GetRegisteredProperty(Type, string)`（`:123`）—— 本仓库内**零调用**，只咬外部消费者。仍是可证明的 ABBA 反转，且位于已发布 public API
- **修法（非破坏）**：删掉 `ForceStaticFieldInit` 中冗余的 `lock(type)`（`FieldDataManager.cs:275`）—— CLR 类型初始化锁本已保证静态初始化只跑一次；或把 `ForceStaticFieldInit` 移出 `_publishLock` 临界区
- **BREAKING**：否

#### O-8 · `[Inject]` 解析失败静默赋 `null`
- [x] 状态：**Batch 7 已修**——保持 `null` 语义（可选协作对象），未注册时 `Debug.WriteLine` 诊断（属性名/类型/修法）；强转改 `is IKeyedServiceProvider` 判断，不支持键控的容器抛带 `serviceKey` 的 `InvalidOperationException`。
- **位置**：`Source/Euonia.Osba/Factory/BusinessObjectFactory.cs:364-371`
- **证据**：`GetService(type)` 未注册返回 `null` → `SetValue(@object, null)`，零诊断 → 必需协作对象变 `null`，稍后在远离病因处爆 NRE。次级问题：`:369` 裸转 `(IKeyedServiceProvider)_provider`（`GetKeyedServices` 扩展同样要求）→ 自定义 `IServiceProvider` 包装会 `InvalidCastException`
- **修法（非破坏）**：区分「未注册（合法 null）」与「已注册但解析失败」，后者日志/抛明确异常；强转改 `is IKeyedServiceProvider keyed ? ... : throw new InvalidOperationException(...)`
- **不可做**：改 `GetRequiredService` → **破坏**故意可选的 `[Inject]`
- **BREAKING**：否

---

### 2.3 P3

| ID | 状态 | 位置 | 问题 | 修法 | BREAKING |
|---|---|---|---|---|---|
| **S-6** | [x] | `Source/Euonia.Core/Security/IPermissionChecker.cs:44`、`Source/Euonia.Security/Scope/IScopeGuard.cs:107` | 死 API：`IsGrantedAny`（全仓 1 次命中=声明本身）、`RefreshAsync`（2 次=声明+实现，零调用） | **Batch 7 已修**：`IsGrantedAny` 加 `[Obsolete]`（文档内联注明原因与替代写法）；`RefreshAsync` 经 S-2 的 `EnsureResolvedAsync` 消费链已不再是死代码，保留。**BREAKING**：否（属性/方法标记，不删 API） |
| **O-6** | [x] | `Source/Euonia.Osba/Factory/ObjectReflector.cs:273`（声明）、`:45`（唯一调用点）、`:298`（throw） | `bool? multiple` 是**循环状态伪装成入参**，调用点从不传 → 恒 `null`；仅在 `:312`/`:329` 写入后 `continue`。**`:298` 的 `multiple == true` throw 是 load-bearing fail-closed，必须保留**（否则嵌套集合晚爆于 `PropertyInfo.SetValue` 的晦涩 `ArgumentException`）。循环**可证终止**（严格下降到数组元素/`IEnumerable<>` 类型实参，有限无环） | 删参数，方法首句 `bool multiple = false;`，`:298` 逐字保留。`private static` + 单调用点 → 非破坏 | 否 |
| **O-7** | [x] | `Source/Euonia.Osba/Factory/ObjectReflector.cs:18-23`、`:316-331`、`:335` | `_collectionTypesName` 列了 `IList<>`/`ICollection<>`/`IEnumerable<>`，但泛型分支只比对 `IEnumerable<>` 一个名字 → `[Inject] public IList<Foo>` 抛 `NotSupportedException` 且消息不提这两种类型；`T[]` 可用、`IEnumerable<T>` 可用 | 泛型分支接受三个全名并取 `GenericTypeArguments[0]`；或从常量里删掉 `IList<>`/`ICollection<>` 让它不再过度承诺 | 否 |
| **O-9** | [x] | **Batch 7 已修**：`_hookedItems` 改 `Dictionary<TItem,int>` 引用计数（0→1 订阅、1→0 退订），复用既有 `ReferenceEqualityComparer`；顺带修复 `AddEventHooks` 的 BusyChanged 订阅体缺失（曾致 `InsertItem_ShouldHookChildBusyChanges` 转红）。原条目： `Source/Euonia.Osba/Core/ObservableList.cs:49`、`:139`、`:226`、`:256` | `_hookedItems` 用引用相等 `HashSet` 去重（`:256` 重复插入跳过订阅，正确）；但 `RemoveItem` → `:226 _hookedItems.Remove(item)` 返回 true 即退订（`:233`/`:238`），而同实例仍在另一索引 → `list.Add(x); list.Add(x); list.RemoveAt(1);` 后 index 0 的 `x` **静默丢失钩子** | 引用相等的 `Dictionary<TItem,int>` 计数，0→1 订阅、1→0 退订。`Tests/.../ObservableListTests.cs` 未覆盖重复实例 | 否 |
| **O-10** | [x] | **Batch 7 已修**：5 处手写 `Delegate.Combine/Remove` 全部改字段式事件（编译器 `Interlocked.CompareExchange` 循环），引发点改 `Event?.Invoke`。原条目： `Source/Euonia.Osba/Core/ObservableDictionary.cs:33-34,65-66`、`Core/ObservableList.cs:72-73`、`Core/ObservableObject.cs:125-126` | 手写事件 add/remove 用 `Delegate.Combine`/`Remove` **非原子**（绕开字段式事件的编译器 `Interlocked` 循环）→ 并发 `+=` 丢订阅者 | `Interlocked.CompareExchange(ref _field, Delegate.Combine(_field, value), _field)` 循环，或回退字段式事件。注意 `ObservableList.cs:91 BusyChanged` 是字段式（已安全）；`ObservableObject.cs:143/156/160` 的 `Interlocked` 只管 `_isBusyCounter` | 否 |
| **O-11** | [x] | **Batch 7 已修**：`Rules` / `FieldManager` 懒初始化进 `lock (_changedPropertiesLock)` 临界区（短临界区、锁序无环），`Target` 重绑同临界区。原条目： `Source/Euonia.Osba/Core/BusinessObject.cs:139-154,639`、`Core/BusinessContext.cs:51` | 懒 `field ??=` 无同步：两个线程各造一份 `Rules`（**两套 `BrokenRules` / 两个 `IsValid`**）且 `field.Target == null` 分支是无锁读-改-写；`FieldManager => field ??=` 两份 `_fieldData` → `HasChangedProperties`/`ReadProperty` 线程间分歧 | `lock` 或 `Lazy<T>`/`Interlocked.CompareExchange`；`Rules` 的 `Target` 重绑必须进同一临界区 | 否 |
| **O-12** | [x] | **Batch 7 已修**：`RulesFor` 改 `Volatile.Read`（acquire 栅栏），与锁内写构成 release/acquire 对；热路径仍无锁无分配。原条目： `Source/Euonia.Osba/Rules/RuleManager.cs:56,67,82,88` | `_propertyRules` 非 `volatile`；`RulesFor:82` 无锁无 `Volatile.Read` 读索引 → (i) 并发 `Add` 后读到陈旧非空索引（新规则暂不生效）(ii) `:88` 普通写无 release 栅栏、`:82` 无 acquire → ARM64 弱内存下可能先见引用后见字典内部存储 | 字段加 `volatile`，或 `:82` 用 `Volatile.Read(ref _propertyRules)` | 否 |
| **O-13** | [x] | **Batch 7 已修**：无参构造初始化 `_properties = []`，未注册属性时按文档抛 `ArgumentOutOfRangeException`。原条目： `Source/Euonia.Osba/Reflection/FieldDataManager.cs:22,27-29,77,88,104` | 公开无参构造**不赋值** `_properties`（唯一赋值在 `:35-39` 的 `Type` 重载）→ `new FieldDataManager().GetRegisteredProperty("X")` 抛 NRE 而非文档承诺的 `ArgumentOutOfRangeException`（`Abstracts/IBusinessObject.cs:40`）。公开可达 | 无参构造 `_properties = [];`（随后正确抛出文档承诺的异常），或改 `internal`/`private` | 否 |
| **O-14** | [x] | **Batch 7 已修**：`RemoveField` 改 `_fieldData.TryRemove`（真正删除，修正名不符实；仓内零调用，保留 API）。原条目： `Source/Euonia.Osba/Reflection/FieldDataManager.cs:245-251` | `RemoveField` **零调用**且名不符实：置 null 不删条目 → `FieldExists` 仍 true、`ReadProperty` 返回 `null` 而非注册的 `DefaultValue` | 删方法（internal 零调用），或改成 `_fieldData.TryRemove(property.Name, out _)` | 否 |
| **O-15** | [x] | **Batch 7 已修**：新增 `HistoryDepth`（默认 32；负数=不设限维持旧行为），超限丢栈底；`IsChanged`/`Undo` 语义不变。原条目： `Source/Euonia.Osba/Reflection/FieldData.cs:14,55,64,70,106` | `_histories` 每次不同写入 `Push`，**无上限**；仅 `MarkAsUnchanged` 清空、`Undo` 逐个弹出；无 `Redo`、无深度查询、公共 API 只观察到前一个值 | 设上限（若只打算单级 undo 就只留上一个值），或暴露深度让调用方自限 | 否 |
| **O-16** | [x] | **Batch 7 已修**：两个构造函数经 `EnsureStringProperty` 接线期抛同一 `NotSupportedException`；运行期分支保留。原条目： `Source/Euonia.Osba/Rules/CommonRule.Regular.cs:61` | 绑到非 string 属性时在**每次检查**而非构造时抛 `NotSupportedException`（被 `Rules.cs:707-713` 转成校验错误）。**已在 `SAMPLE.md:932` 文档化** → 归为健壮性项而非缺陷 | 构造函数（`:20`、`:33`）校验 `Property.PropertyType == typeof(string)` 并在接线时抛 —— 同一异常，只是提前 | 否 |
| **O-17** | [x] | `Source/Euonia.Osba/Actuators/Actuator.cs:54-55`、`Factory/BusinessObjectFactory.cs:330-331` | 2 个**未加注释**的空块（另有 5 个已带注释：`ServiceCollectionExtensions.cs:40-42`、`Core/BusinessContext.cs:125-127`、`Core/BusinessObject.cs:740-742`、`:981-983`、`Reflection/PropertyInfo.cs:118-120`） | **补上标准注释 `// 空块：用于阻止 IDE 代码分析建议（勿删除）`。⚠️ 不要删除**（G4，用户指令覆盖 sweep 的「删除」建议） | 否 |
| **O-18** | [x] | **Batch 7 已修**：`GetLoadableTypes` 逐条 `Trace.WriteLine` LoaderExceptions；`CreateInstance` 改 `Trace`（Release 可见）。附带待办同步完成：6 个 Account/Credential 异常单参构造补默认消息。原条目： `Source/Euonia.Osba/ServiceCollectionExtensions.cs:74-77`、`Reflection/PropertyInfo.cs:262` | `catch (ReflectionTypeLoadException)` 返回部分类型但**丢弃 `ex.LoaderExceptions`**（唯一能报告单类型加载失败的地方）→ 拼错依赖表现为「扫描什么都没找到」；`PropertyInfo` 的 `Debug.WriteLine` 在 Release 下**完全静默**（降级为共享默认值零痕迹） | 记录 `LoaderExceptions`；`PropertyInfo.cs:262` 改用 `ILogger`/`Trace` | 否 |

---

## 3. 已检查且确认干净（勿重复排查）

### Euonia.Osba
- 全项目 **5 个 `catch` 子句，0 个空 catch、0 个静默吞异常**（清单：`Rules.cs:707-713` 转校验错误并嵌入异常类型与内部异常；`PropertyInfo.cs:260-264` → O-18；`ServiceCollectionExtensions.cs:74-77` → O-18；`BusinessObject.cs:289-293` `throw;`；`Rules.cs:700-705` `throw;`）
- `.Result` / `.Wait()` / `Thread.Sleep` / `Monitor.Wait` / `SemaphoreSlim.Wait` / `SpinWait` / `Mutex` / 各种 Event：**全 0**
- **`lock` 与 `async` 零交集**（30 ∩ 11 = ∅），`lock` 内无 `await`；无 `async void`
- 无 `TODO`/`FIXME`/`HACK`/`XXX`/`BUG:` 注释；无 `NotImplementedException` 桩
- 唯一 `while (true)`（`ObjectReflector.cs:275`）已证终止
- 权限检查 **fail-closed**；唯一短路是 `IsBypassingRuleChecks`（→ O-2）
- `BrokenRuleCollection` 覆写了全部 4 个变更钩子 → `ErrorCount`/`WarningCount`/`InformationCount` 不会与内容脱节
- `Rules` / `BrokenRuleCollection` 锁序一致（始终 `Rules._lockObject` → `BrokenRuleCollection._lockObject`）；用户可重写的 `RuleCheckComplete` 故意在锁外调用（`Rules.cs:648-655`）
- `Rules.RunAsync` 取消策略正确（`OperationCanceledException` 重抛，其余转错误结果）
- `ObservableObject` 忙计数用 `Interlocked.Increment/Decrement/CompareExchange`
- `ObjectReflector.FindMatchedMethod` 打分：参数个数守卫、可选参数、`params` 回退、`MissingMethodException` 路径均有守卫
- `SaveAsync` 把授权放在 `WithActivatorAsync` **外**，create/fetch/execute 放**内** —— 不对称但一致于「先授权再初始化实例」
- `RuleScope` 规则名 `OrdinalIgnoreCase`（`RuleScope.cs:91`）自洽，且**不属于** O-3 的口径冲突

### Euonia.Security + Core/Security
- `ScopeSubjectSet` / `ScopeSubject` 不可变性：`Codes`/`KeysWithGrants` 是构造期 `Array.AsReadOnly` 快照、`ValuesOf` 返回副本、`Build()` 深拷贝、`Add` 拒绝共享 `Empty` 实例 —— 历史「cast 回 `HashSet` 注入 `admin:*`」漏洞已封
- `ScopeGuard` 失效协议：`_version` 在 `Invalidate()` 持锁自增；`ResolveAsync` 在同锁下 `version == _version` 复查后才发布；`GetPolicy` 在 `:161`/`:177` 复查 → 并发 `Refresh` 不会回滚；重试有界 `MaxResolveRetries = 3` 且带明确消息
- fail-closed 默认：未认证 → `ScopeSubjectSet.Empty`；无 resolver → `InvalidOperationException` → `Holds`/`IsGrantedAsync` 返回 false；`Allows<T>(null)`/`AllowsObject(null)` 均 false 且 `ExplainCore(null)` 结论一致（接口 `IScopeGuard.cs:76` 的双入口同结论承诺成立）
- 过期注册表 fail-open 已封：`Rebuild` 用工厂 lambda 注册 `IObjectScopeAuthorizer`/`IScopeKeyResolver`，`LastBuild` 只在完整成功后写，`IScopeGuard` 工厂惰性取 `ScopeModelRegistry`
- `checker == null ⇒ true` 的查询语义**无法穿透门**（`ObjectAuthorization.cs:53-62` 先断言 `BusinessContext` 与 `IPermissionChecker`）
- `ConfigurationRuleBinder` fail-fast 覆盖：缺 `Operations` 节点、未知子键、标量代替数组、空白数组元素、无规则的操作、不可解析/歧义/非 attribute/泛型 attribute、配置内 case-only 重复 —— 均在 `AddPermission` 抛出且带节点路径
- **通配符语义两实现一致**：`ClaimPermissionChecker.Matches`（`:59`/`:67`）与 `ScopeSubjectSet.HoldsPermission`（`:125-140`）都是精确 `OrdinalIgnoreCase` + `*` 前缀 `OrdinalIgnoreCase`，通配符不参与维度查找
- 只读包装：`OperationCodeSource.AllOperations`（`:39,:50`）、`CompositeCodeSource`（`:25,:30,:46,:65`）、`EmptyCodeSource.AllOperations`（`:31`）、`BusinessOperation.All`（`:43-50`）
- `UserPrincipal` 空安全：`Claims?.Identity?...`、`?? false`、`?? Array.Empty<>()`（`:146`），含新补齐的 `Username` 各分支（`:75-91`）—— 无 NRE 路径
- `ExceptionHandlingInterceptor`：`MaxInnerExceptionDepth = 16`（`:21`）循环有界（`:72-85`），先判 `RpcException` 再解包
- `ScopeFilter.Apply` 与 `Filter`/`Allows` 同源（`Allow && !Deny`，`CompiledScopePolicy.cs:15` / `ScopePolicy.cs:11` / `ScopePolicyNode.cs:10`，合于 `ScopeFilter.cs:29,38`）；pushdown 不回退客户端求值
- `PermissionModelSetup.Signature` 作为 `(count, count)` 元组安全：两个列表只增不减且去重 → 计数相等即集合相等，`ServiceCollectionExtensions.cs:260-265` 的跳过逻辑成立

---

## 4. 已决事项（不再讨论）

| 议题 | 决定 |
|---|---|
| 空代码块 | **永不删除**，缺注释的补标准注释（G4） |
| `ClaimPermissionChecker` | **保留可使用**，`[Obsolete]` 不恢复（已由 `35b9c6d` 删除），默认 `SubjectPermissionChecker`，宿主手动 `AddScoped` 启用；只改 5 处文档 + 补 README opt-in |
| 8 个 `Account*`/`Credential*` 异常 | **保留**（sweep verdict：已发布 NuGet 的公开 API、仓内零引用是库异常分类法的预期、三条异常→状态码管道按**基类**匹配不按名字、`[Serializable]` 缺失与 16 个兄弟异常中的 12 个一致）。附带待办见下 |
| `IScopeGuard.GetSubjects()` | **加 `GetSubjectsAsync`**（默认接口成员，非破坏）+ `internal` 异步授权变体接到 8 个 async 入口 |
| `O-2` BypassRuleChecks | **改 `AsyncLocal`（BREAKING）** |
| `S-4` 空程序集 | **镜像 `EmptyCodeSource` 规则**：零程序集须显式断言 |
| `O-8` `[Inject]` | **保持 `null` + 可诊断日志** + `IKeyedServiceProvider` 强转守卫 |
| **O-1 Regex 超时** | **不做**（用户指令「不考虑超时」）。不加 `matchTimeout`，ReDoS 风险按现状接受 |
| **权限码（含操作名）忽略大小写** | 用户指令。判定、去重、行级策略表、注册期校验一律 `OrdinalIgnoreCase`；入口**方法名**仍按 C# 语义大小写敏感。已写入 `Source/Euonia.Security/README.md` §3.4 / §4.1 |
| `O-6` `multiple` 参数 | 删参数、局部 `bool multiple = false`，`:298` throw **保留** |
| **`O-4` 同步规则入口** | **回滚、暂不实施**（先按 fail-fast 实现，随后按用户指令全部还原）；重新决定前保持现状阻塞语义 |
| `O-15` 的 switch 抑制 | **不可删**（G8） |
| `PropertyInfo.cs:262` 中文消息 | **有意不本地化**（`Debug.WriteLine` 诊断，非用户可见文案）——但 O-18 要求换成 Release 可见的通道 |

### 附带待办（sweep 提出、未列为独立条目）
- [ ] `AccountException`/`CredentialException` 单参构造只转发 `identity`/`credential` 不带消息（`AccountNotFoundException.cs:13-16`、`CredentialException.cs:15-18`）→ `Message` 停留在 BCL 默认值；而三个管道都读 `exception.Message`（`ExceptionExtensions.cs:62-68`、`ApiExceptionMiddleware.cs`）→ 客户端拿到 `"Exception of type '...AccountNotFoundException' was thrown."`，既泄漏类型名又无信息。建议默认消息用 `identity`/`credential` 格式化
- [x] `Source/Euonia.Security/README.md` 从未提及 `ClaimPermissionChecker` → **Batch 6 已补 §4.4**（见 S-3）

---

## 5. 批次计划

| 批次 | 内容 | 预期 |
|---|---|---|
| **Batch 4**（收尾，改动极小） | ① 跑 S10 护栏测试 ② **O-3**（删一个比较参数）③ **O-6**（删参数+局部化）④ **O-7**（补两个类型名）⑤ **O-17**（补 2 处注释） | ✅ **914 / 901 / 0 / 13** |
| **Batch 5**（P1） | ✅ **S-1** + 提前并入 **S-5** → ~~**O-1**~~（不做，不考虑超时）→ ✅ **S-2**（Core 契约 DIM 预热 + internal 异步授权变体接 8 入口） | ✅ **926 / 913 / 0 / 13** |
| **Batch 6**（P2） | ✅ **O-5**（删冗余 `lock(type)`）→ **O-4**（fail-fast 已实现、后按指令**回滚**，保持 `- [ ]`）→ ✅ **S-4**（镜像 `EmptyCodeSource` 规则）→ ✅ **S-3** 仅文档（5 处 + 新增 README §4.4） | 回滚后基线全量绿 |
| **Batch 7**（P3） | ✅ **S-6 / O-8 / O-9 / O-10 / O-11 / O-12 / O-13 / O-14 / O-15 / O-16 / O-18** + 附带待办（异常默认消息）；`S-6` 的 `RefreshAsync` 保留（已被 `EnsureResolvedAsync` 链消费） | ✅ Core 78 / Security 138 / Osba 233 / Standalone 7 全绿 |
| **Phase 4** | ✅ **O-2**（`IsBypassingRuleChecks` → AsyncLocal 流作用域，BREAKING）→ **O-4 方案 B**（`CheckPropertyRulesAsync` 出口 + 同步入口文档标注） | ✅ Osba 236 全绿（新护栏 3 条） |
| **Phase 5** | ✅ 全量验证 16 个测试项目 + **BREAKING 汇总**（见下方） | ✅ 见执行记录 |

> 每批结束：`dotnet test Euonia.Test.slnx` → 报告 → **停下等确认**。

---

## 6. 执行记录

| 日期 | 批次 | 结果 | commit |
|---|---|---|---|
| 2026-09-29 | Batch 4（S10 / O-3 / O-6 / O-7 / O-17） | 全量 914 / 901 / 0 / 13，0 failed；S10 与 O-3 回退均转红 | 未提交 |
| 2026-09-29 | Batch 5 前半（S-1 + S-5，权限码忽略大小写） | 全量 **919 / 906 / 0 / 13**，0 failed；S-1 回退 failed 3、S-5 回退大面积红 | 未提交 |
| 2026-09-29 | Batch 5 后半 = **S-2**（异步授权预热） | 全量 **926 / 913 / 0 / 13**，0 failed；删两处 `AuthorizationWarmup.WarmAsync` → 护栏 **failed 4**；新护栏 6+1 条 | `627c2c1` |
| 2026-09-29 | Batch 6 前半（**O-5** + **S-3** + **S-4**） | 全量 **931 / 918 / 0 / 13**，0 failed；O-5 回退 failed 1（3s 超时）、S-4 回退 failed 1；新护栏 1+4 条 | `627c2c1` |
| 2026-09-29 | **O-4 fail-fast：实现 → 按用户指令回滚** | 实现期全量 **935 / 922 / 0 / 13**（新护栏 `SynchronousRuleFailFastTests` 4 条 + 改写既有死锁测试；回退删两处 `EnsureSynchronouslyCompleted` → failed 1，对象级路径表现为挂起）。随后 `Rules.cs` / `PropertyRuleTriggerTests.cs` 还原到 HEAD、删除新护栏测试，**回到 Batch 6 前半状态** | 未提交 |

| 2026-09-29 | **Batch 7（P3 全部 11 项 + 附带待办）** | Core 78 / Security 138 / Osba 233 / Standalone 7，全绿。改动面：Core（`IPermissionChecker` S-6、6 个异常默认消息、DIM 的 `ValueTask.CompletedTask` → `default` 修复 netstandard2.1 编译）、Osba（`BusinessObjectFactory` O-8、`ObservableList` O-9/O-10、`ObservableDictionary`/`ObservableObject` O-10、`BusinessObject` O-11、`RuleManager` O-12、`FieldDataManager` O-13/O-14、`FieldData` O-15、`CommonRule.Regular` O-16、`ServiceCollectionExtensions`/`PropertyInfo` O-18）。**netstandard2.1 编译修复说明**：`627c2c1` 的 DIM 用了 `ValueTask.CompletedTask`，该成员 .NET 5 才有，netstandard2.1 TFM 编译失败（此前多 TFM 构建未跑全），改为 `default` | 未提交 |

| 2026-09-30 | **Phase 4（O-2 AsyncLocal + O-4 方案 B）** | Osba **236 / 236** 全绿（新增 `BypassRuleChecksFlowScopeTests` 3 条；探针必须继承 `ObservableObject`——只读探针写入被拒会让握手 TCS 永不置位、用例挂起，首版踩坑已注记）。O-2 BREAKING：绕过标志改异步流作用域，不再外溢并发流；嵌套 using 还原捕获值。O-4 非破坏：`CheckPropertyRulesAsync` 公共出口 + 同步入口文档标注异步对等物 | 未提交 |
| 2026-09-30 | **Phase 5 全量回归（16 个测试项目）** | Core 78 / Security 138 / Osba 236 / Standalone 7 / Pipeline 14 / Domain 22 / Linq 38 / Application 174 / Bus 113 / Bus.InMemory 13 / Bus.Http 11 / Bus.HealthChecks 6 / Bus.ActiveMq 10 / Bus.Grpc 11 / Bus.RabbitMq 29（10 skipped：无 Broker）/ Caching.Memory 12 / Caching.Runtime 9 / Caching.Default 4 / Caching.Redis 3（3 skipped：无 Redis）/ Mapping.AutoMapper 3 / Mapping.Mapster 3 —— **0 failed** | 未提交 |

> **全部批次完成（Batch 4–7 + Phase 4/5）**。不做/否决项：O-1（用户指令不考虑超时）、O-4 方案 A（fail-fast，曾实现后按指令回滚并否决）。
>
> **BREAKING 变更汇总（Phase 5）**：
> 1. **O-2**：`BusinessObject.IsBypassingRuleChecks` 从对象级 `bool` 改为**异步流作用域**（每实例 `AsyncLocal<bool>` 容器）。行为差异：绕过标志不再跨流外溢（并发调用方互不可见）；`BypassRuleChecksObject.Dispose` 还原**进入时捕获的值**而非恒 `false`（嵌套 `using` 语义修正）。属性仍为 `protected virtual bool`，源兼容（无需改动的消费方仅观察到更安全的并发行为）；依赖「绕过期间同实例并发 setter 也被跳过」这一旧缺陷行为的代码受影响。
> 2. **O-15**：`FieldData<T>` 新增 `HistoryDepth`（默认 32）——默认行为变化：撤销历史超 32 条后丢弃最旧记录；需要完整历史的调用方显式设为 `-1`。
> 3. **S-6**：`IPermissionChecker.IsGrantedAny` 标记 `[Obsolete]`（仅警告，不删 API）。
> 4. **异常默认消息**：6 个 Account/Credential 异常的单参构造现在生成具体消息（此前 `Message` 为 BCL 默认值）；依赖 BCL 默认消息文本的断言需调整。

