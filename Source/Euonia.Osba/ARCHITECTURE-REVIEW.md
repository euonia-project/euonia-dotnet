# Euonia.Osba 架构与实现评审报告

> 评审对象：`Source/Euonia.Osba`（版本线 `2026.4.5`，目标框架 `net9.0;net10.0`）
> 评审方式：源码逐文件审阅 + 构建 + 240 个既有测试 + 独立基准/复现程序（Release、net10.0、Apple Silicon）。
> 本报告只记录问题、证据与修复方案，不包含已实施的代码改动。基准数字用于判断数量级，不是正式 BenchmarkDotNet 结果。

---

## 0. 结论摘要

### 0.1 一句话判断

`Euonia.Osba` 是一个**设计意图清晰、文档与测试投入很高，但“对象模型 / 持久化 / DI / 校验 / 授权”四层耦合过深、且关键热路径仍有明显性能与正确性缺陷**的富对象框架。它现在更适合被定位为「应用层的可编辑模型 + 命令模型」，而不是「纯 DDD 的领域聚合根基类」。

### 0.2 必须优先处理的 6 件事

| 级别 | 问题 | 后果 |
|---|---|---|
| P0 | `ValueTask` / `ValueTask<T>` 工厂方法**不被等待** | 持久化静默丢失，异常被吞 |
| P0 | 基类提供 `Create/Insert/Update/DeleteAsync` **空实现** | 忘记写工厂方法时保存/删除**静默成功但什么也没做** |
| P0 | `ref` 字段存储模式与规则系统**互不兼容** | 规则/DataAnnotations 读到默认值，校验结论错误 |
| P0 | EF Core 物化时属性 setter 全部标脏 | 刚加载的对象就是“已修改”，只读查询也会触发更新 |
| P0 | `*Async(criteria)` 的**行级权限是“先写后验”** | 越权数据已经落库，异常只是事后报告 |
| P1 | 每次属性写入约 **0.5–0.7 µs / 464 B**；一条属性规则约 **2.2 µs / 1.5 KB** | 与普通 POCO（约 1–2 ns / 0 B）差 2–3 个数量级 |

### 0.3 性能结论（稳态，单线程）

| 场景 | 耗时/次 | 分配/次 |
|---|---:|---:|
| `new` 一个业务对象（未触碰属性） | ≈ 42 ns | 328 B |
| 首次访问 `FieldManager` | ≈ 0.85 µs | 1.6 KB |
| 首次读一个属性（1 个属性） | ≈ 1.2 µs | 2.1 KB |
| 首次写一个属性（1 个属性） | ≈ 1.5 µs | 2.7 KB |
| 复用对象、写第 33 次以后的属性 | ≈ 0.5–0.7 µs | 464 B |
| 写一个**带 1 条属性规则**的属性 | ≈ 2.2 µs | 1.5 KB |
| `ValidateAsync()`（无规则对象） | ≈ 0.85 µs | 800 B |
| `SaveAsync()`（无规则、无真实持久化） | ≈ 4.5 µs | 2.6 KB |
| EF Core 物化 1 个 3 属性对象 | — | ≈ 9.7 KB（同结构 POCO 约 6.1 KB） |

根因不是“富对象必然慢”，而是：**每个字段一个 Rx `BehaviorSubject`、每次写入 O(32) 的历史裁剪、每个实例复制一份属性元数据、值类型装箱、反射调用、规则路径的临时集合与 Task 机制、一次保存 3 次授权判定**。这些都可以在不牺牲语义的前提下修掉。

---

## 1. 现状基线

### 1.1 规模与依赖

- C# 约 **10.3k 行**（含 303 行生成资源），源码文件约 100 个。
- `BusinessObject.cs` 1123 行、`Rules.cs` 803 行、`ObservableObject.cs` 526 行，三个 God Class。
- 依赖：`Euonia.Core`、`Euonia.Modularity`、`Euonia.Pipeline`、`Euonia.Validation`、`System.Reactive`、`System.Interactive`、`Microsoft.Extensions.DependencyInjection.Abstractions`。
- 其中 **`System.Interactive` 完全未被使用**；`System.Reactive` 只用于 `FieldData<T>` 的一个 `BehaviorSubject<T>`。
- `Euonia.Modularity` 只为两个接口（`IHasLazyServiceProvider` / `ILazyServiceProvider`）而引用，且这两个接口被放在了 `System` 命名空间下。
- `Euonia.Osba` **不引用** `Euonia.Domain`；文档中的模块图（`Osba --> Domain`）与实际依赖不一致。

### 1.2 构建与测试

```text
dotnet build Source/Euonia.Osba/Euonia.Osba.csproj -c Debug
  Build succeeded. 0 Warning(s) 0 Error(s)

直接执行测试宿主 Tests/Euonia.Osba.Tests/bin/Debug/net10.0/Euonia.Osba.Tests
  Total: 240, Errors: 0, Failed: 0, Skipped: 0, Time: 0.608s
```

> 注意：`dotnet test Tests/Euonia.Osba.Tests/...` 当前返回 `Zero tests ran (exit code 5)`，只有直接运行测试宿主才能发现/执行测试。这是仓库级 MTP/xunit.v3 接线问题，不是 Osba 代码问题，但会让 CI 出现“绿色但没跑测试”的假象，应一并修复。

### 1.3 设计意图（值得肯定的部分）

以下不是客套，是这套代码真正的资产，修复时应保留：

1. **权限线与验证线严格分离**：越权一律 `SecurityException`，校验一律 `ValidationException`，规则集合里不放权限规则。设计文档 `PERMISSION-DESIGN.md` 把取舍写得很清楚。
2. **fail-closed**：声明了 `[Permission]` / `ScopeModel<T>` 却没有接线时，工厂边界抛 `InvalidOperationException`，而不是静默放行。
3. **行级权限下推**：`IScopeGuard.Apply(IQueryable)` 生成表达式树交给 EF，避免“取全量再内存过滤”。
4. **契约放在 Core**：`IPermissionChecker` / `IObjectScopeAuthorizer` / `IObjectOperationResolver` 不依赖任何一侧实现，Osba 与 Security 之间没有适配包。
5. **单点映射**：`ScopeOperationMap` 是“对象状态 → 业务操作”的唯一实现，避免多处漂移。
6. **已有 240 个测试覆盖了很多边界**：并发 Bypass、规则取消、死锁、作用域隔离、权限大小写、EF 下推形状等。
7. **大量注释记录了“为什么”**（锁序、AsyncLocal、裁剪历史等），这在框架库里非常珍贵。

---

## 2. 设计合理性评估

### 2.1 核心设计问题

#### D1. 双存储模型：字典存储 vs 字段存储

`BusinessObject` 同时支持两种属性存储：

- **字典模式**（文档与示例主推）：`SetProperty(PropertyInfo<T>, value)` 把值写进 `FieldDataManager._fieldData`（`ConcurrentDictionary<string, IFieldData>`），真实 CLR 字段不存在。
- **字段模式**：`SetProperty(PropertyInfo<T>, ref T field, T value)` 把值写进调用方的字段，`FieldDataManager` 只记录脏标记。

两种模式互不兼容：

- 字段模式下，`ReadProperty(IPropertyInfo)` 仍然从字典读，于是**永远读到默认值**；
- `DataAnnotationRule`、`CommonRule.*`、lambda 规则都通过 `ReadProperty` 取值；
- EF Core 物化时使用属性 setter，字典模式下每个 setter 都会标脏。

实测（字段模式 + `[Required]` + 正则规则）：

```text
A='123', ReadProperty='', valid=False, broken=2
  - A: A 必填
  - A: A 只能是数字
```

值明明是 `"123"`，规则却按 `""` 判定。这是当前最危险的设计缺陷之一，因为字段模式恰恰是性能最好、EF 最友好的模式，文档却没有警告。

**结论**：必须收敛为一种存储模型。建议长期改为“字段为唯一真值 + `PropertyInfo<T>` 携带编译后的 getter/setter + `FieldData` 只存基线与脏标记”；短期至少让 `ReadProperty` 在字段模式下通过注册的 `System.Reflection.PropertyInfo` 读取真实字段，并对两种模式分别加契约测试。

#### D2. 规则注册表是进程级静态单例

`RuleManager._container` 是 `static Lazy<ConcurrentDictionary<Type, RuleManager>>`。后果：

- 规则跨 **DI 容器、测试宿主、租户、插件** 共享；
- `AddRules()` 每个类型在进程内只执行一次，晚注册的模块拿不到初始化机会；
- 运行期 `Rules.AddRule(...)` 是 public，任何业务代码都能污染全局；
- 规则实例是共享单例，绝不能持有请求状态（文档已警告，但这是设计造成的陷阱）；
- 测试之间只能靠 `RuleManager.CleanRules(type)` 手动清理，且 `CleanRules` 与 `GetOrAdd` 存在竞态。

“规则是类型级共享”本身可以接受，但**共享边界应该是容器，不是进程**。建议引入 `IRuleRegistry`（Scoped/Singleton per container），静态容器只作为无 DI 的兜底。

#### D3. 领域对象承担了持久化、DI、授权三重基础设施职责

- `EditableObject.SaveAsync` 里 `BusinessContext.GetRequiredService<IObjectFactory>()`；
- `[FactoryInsert]` 方法直接 `BusinessContext.GetRequiredService<IProjectStore>().AddAsync(this)`；
- `[Inject]` 属性注入 + `ILazyServiceProvider` 服务定位；
- `BusinessContext.CurrentServiceProvider` 对领域代码完全公开。

这不是 DDD 的“富领域模型”，而是 **Active Record + Service Locator**。它让领域对象无法脱离容器单测，也让“领域层不依赖基础设施”这条 DDD 基本约束失效。

#### D4. 两套命令、两套状态、两套事件

- `CommandObject<T>` 与 `Euonia.Domain.Command` / MediatR 的命令各成一套；
- `ObjectEditState` + `FieldData.IsChanged` + `ChangedProperties` 三个“是否变更”的来源；
- `EditableObject.Saved`（弱事件）与 `Euonia.Domain` 的 `DomainEvent` 各成一套；
- 示例里的 `EditableObjectBase<TTarget,TKey>` 直接把 `Aggregate<TKey>` 的 `_handlers` / `_events` / `RaiseEvent` 复制了一遍（95 行），因为 C# 不支持多继承，`EditableObject<T>` 又要求 `T : EditableObject<T>`。

**结论**：Osba 与 Domain 在类型系统层面不兼容。要么承认 Osba 是另一条技术路线（编辑模型），要么提供一个 `Euonia.Osba.Domain` 适配包（组合而非继承）。

#### D5. 授权位置与“纯 DDD”冲突

`[Permission]` 标在实体/聚合的工厂方法上，授权在实体保存时执行。纯 DDD 中“当前用户”属于应用层概念，领域对象不应该知道用户与权限。当前设计适合“对象即用例边界”的架构；若坚持纯 DDD，应把授权上移到用例/应用服务，实体只保留不变量。

#### D6. 权限判定重复且存在后置判定

- `ObjectAuthorization.Enforce` 先调 `businessObject.CanPerformOperation(operation)`，再对 `requirements.All(...)` 逐条判定一次；`CanPerformOperation` 内部已经按运行时类型重新收集并判定了一遍，**每条要求至少判定两次**。
- `ScopeAuthorization` 在 `SaveAsync` 上做“前置 + 后置”两次完整判定；`Delete/Insert/UpdateAsync(criteria)` 只有后置判定，而工厂方法内部通常已经落库，**拒绝发生在写入之后**。文档把它列为“已知边界”，但对安全框架来说这是不可接受的默认行为。

#### D7. 生命周期与作用域

- `BusinessContextAccessor` 的**构造函数**会写静态 `AsyncLocal<IServiceProvider>`。DI 解析一个 Scoped 服务就产生全局副作用；同一异步流里创建两个 scope 会互相覆盖；后台任务会捕获已释放的 scope。
- `BusinessContext.User` 在构造时快照，认证中间件之前解析就永久为 `null`。
- `RuleScope` 在目标对象上只有一个槽位（`_operationScope`），嵌套/并发执行器会互相覆盖，先结束的会 `Detach` 掉后开始的。

#### D8. 公共 API 面过大

`FieldDataManager`、`PropertyInfoManager`、`RuleManager`、`BrokenRuleCollection`、`PropertyInfoList`、`FieldData<T>` 均为 public 且可写；`RuleManager.Rules` 直接暴露 `List<IRuleBase>`，`Initialized` 有 public setter；`PropertyInfoList` 继承 `List<IPropertyInfo>` 并公开 `IsLocked`。这会把内部实现永久钉在公共 API 上，后续任何重构都是破坏性变更。

---

## 3. 正确性缺陷（附复现证据）

以下问题均已用独立程序复现；建议每一项都先补回归测试再修。

### C1. `ValueTask` 工厂方法不被等待（P0，静默数据丢失）

`Euonia.Core/Extensions/Extensions.Type.cs:168` 的 `IsAsync` 只识别 `Task` 和实现 `IAsyncResult` 的泛型；`ValueTask` / `ValueTask<T>` 不满足，于是 `BusinessObjectFactory.InvokeAsync` 走同步分支，把返回的 `ValueTask` 丢弃。

复现：

```text
[ValueTask factory] persisted immediately after SaveAsync=0
[ValueTask factory] persisted after 100ms=1
```

**修复**：识别 `Task`、`Task<T>`、`ValueTask`、`ValueTask<T>`（以及自定义 awaitable 的 `GetAwaiter()` 形态），或在启动期直接拒绝不支持的返回类型。异步调用统一 `await`，异常用 `ExceptionDispatchInfo.Capture(...).Throw()` 保持原栈。

### C2. 缺省工厂方法是空实现，保存/删除“静默成功”（P0）

`EditableObject<T>` 为 `CreateAsync/InsertAsync/UpdateAsync/DeleteAsync` 提供了 `Task.CompletedTask` 空实现（`Core/EditableObject.cs:155-188`）。因为约定查找会回溯基类，任何忘记写 `[FactoryInsert]` 的类型都能“成功保存”。

复现：

```text
SaveAsync on type with no [FactoryInsert]: returned=True, state=None (no exception)
SaveAsync(Delete) on type with no [FactoryDelete]: no exception, state=Deleted
CreateAsync on type with no [FactoryCreate]: returned=NoPersistence, state=New (base no-op invoked)
```

**修复**（三选一，建议 1+3）：

1. 基类改为 `throw new NotSupportedException(...)`；
2. `AddBusinessObject` 启动期校验：`IEditableObject` 必须有 `Insert/Update/Delete` 中实际实现的方法；
3. 提供 `OsbaOptions.StrictFactoryMethods`（默认开启）让旧代码可显式降级。

### C3. 字段存储模式下规则读到默认值（P0，见 D1）

`DataAnnotationRule`、`CommonRule.Required/Regular/Lambda` 都用 `target.ReadProperty(Property)`，而字段模式从不写 `FieldData`。

### C4. 非泛型 `SetProperty(IPropertyInfo, object)` 对相同值仍标脏（P1）

`ObservableObject.cs:491-509` 没有比较新旧值，直接 `SetFieldData` + `PropertyHasChanged`；而泛型重载会走 `ValuesDiffer` 提前返回。文档承诺“赋同一个值不算变更”，只对泛型路径成立。

复现：

```text
[same-value SetProperty] changed before=0, after=1, events=2
```

**修复**：非泛型入口也做值比较（注意装箱/引用类型语义），并统一走同一个内核。

### C5. `ReadOnlyObject` 可被 `LoadProperty` 改写（P1，安全语义）

`ReadOnlyObject.CanWriteProperty` 返回 `false`，但 `IBusinessObject.LoadProperty(IPropertyInfo, object)` 是 public 且不检查写权限。

复现：

```text
[read-only LoadProperty] CanWriteProperty=False, A='hacked'
```

**修复**：把加载入口收窄为 `internal`/`protected`（工厂与 EF 物化使用），或增加显式的 `IObjectMaterializer` 能力接口；`ReadOnlyObject` 至少要在 `LoadProperty` 上校验“正在物化”状态。同时 `LoadProperty` 是 public API，任何宿主代码都能改只读对象，这是权限模型上的洞。

### C6. 删除后状态不复位，重复 `SaveAsync` 会重复删除（P1）

`EditableObject.SaveAsync` 用 `wasDeleted` 阻止 `MarkAsClean()`，删除成功后 `State` 仍是 `Deleted`。

复现：

```text
[delete twice] state after 1st delete=Deleted, deleteCount=1
[delete twice] state after 2nd delete=Deleted, deleteCount=2
```

**修复**：删除成功后调用 `AcceptChanges()`（或专门的 `MarkAsDeletedCommitted()`），并让第二次保存变成幂等 no-op。删除失败则保持状态以便重试。

### C7. EF Core 物化即脏（P0，直接影响“只读查询”）

EF 通过属性 setter 物化；字典模式下 `SetProperty` 会 `PropertyHasChanged`。

复现（EF Core 10 + SQLite，3 个属性）：

```text
[new object]         changed=2, hasChanged=True, state=None
[materialized]       changed=2 [Name,Qty], hasChanged=True, state=None, isSavable=True
[simulate update]    HasChangedProperties before MarkAsChanged = True
[materialize Thing]  ≈ 9.7 KB/entity（同结构 POCO ≈ 6.1 KB/entity）
```

后果：`UpdateActuator` 会把“用户什么都没改”的对象标记为 `Changed` 并更新；`IsSavable` 对刚加载的对象为 `true`，与“干净加载”的语义矛盾；示例仓储 `context.Update(project)` 还会全列更新。

**修复**：提供 `AddOsbaEntityFrameworkCore()`：

- 注册 `IMaterializationInterceptor`：`CreatingInstance` 时给对象打“物化中”标记，`SetProperty` 在该标记下走 `LoadProperty` 语义；`InitializedInstance` 时清除并 `AcceptChanges()`；
- 提供 `ApplyChanges(DbContext, T)`：只把 `ChangedProperties` 对应列标 `IsModified`；
- `Id` 建议改为 `private set` + 工厂内 `LoadProperty`，避免“用户赋值 Id 静默无效”；
- 加 EF 映射与部分更新的集成测试。

### C8. `*Async(criteria)` 先写后验（P0，安全）

`BusinessObjectFactory.InsertAsync/UpdateAsync/DeleteAsync(criteria)` 的流程是：调用工厂方法（方法体内通常 `SaveChanges`）→ 再 `ScopeAuthorization.EnsureAuthorizedAfter`。越权时异常抛出，但数据已经写入。

**修复**：

- 对受数据权限约束的类型，**禁止**走 criteria 低层入口（启动期/运行时检测并抛 `InvalidOperationException`），只允许 `Fetch → 判定 → 修改 → Save`；
- 或要求工厂方法返回“待判定行”/提供服务端 `IAuthorizationTargetResolver`，在写入前完成判定；
- 删除路径同理：先 `Fetch` + `EnsureAuthorizedBefore(Delete)`，再 `Delete`。

### C9. `RuleScope` 覆盖与并发（P1）

`Rules.AttachOperationRules(RuleScope)` 只是赋值；`ActuatorBase.ExecuteAsync` 在 `finally` 里 `DetachOperationRules()` 置空。嵌套执行器或同一实例的并发操作会让外层规则范围丢失，或把内层规则带给外层。

**修复**：改为 `AsyncLocal<ImmutableStack<RuleScope>>` 或实例内栈 + 计数；`Detach` 只弹出自己那一层。

### C10. 业务异常被 `TargetInvocationException` 包裹（P1，易用性）

`MethodInfo.Invoke` 对同步工厂方法抛出的异常会包一层。复现：

```text
sync factory throw -> TargetInvocationException: Exception has been thrown by the target of an invocation.
```

**修复**：统一用 `ExceptionDispatchInfo` 解包；`LoadPropertyByReflection` 同理。

### C11. `BusinessContextAccessor` 构造副作用（P1）

构造函数写静态 `AsyncLocal`。复现/推理：

- 同一异步流中创建两个 scope，后创建的 provider 覆盖前一个；
- 后台任务捕获 provider，请求结束后 scope 已释放；
- 单测里解析顺序会改变环境上下文。

**修复**：构造函数只保存 `_provider`；`SetCurrent` 由 ASP.NET 中间件/显式 `using` 作用域调用；提供 `IRequestContextAccessor` 并在 `finally` 清理；不要用 AsyncLocal 持有可释放的 scope，后台任务应使用 `IServiceScopeFactory` 新建 scope。

### C12. `Saved` / `ValidationComplete` 是弱事件（P1）

`BusinessObject.Events` 是 `WeakEventManager`。用 lambda 订阅、外部又没有强引用时，委托可能被 GC，事件静默不再触发。对于服务端库，这与 `PropertyChanged`（强事件）语义不一致。

**修复**：改为普通强事件；确需弱引用时提供显式的 `WeakEvent` 包装并文档化。

### C13. `FieldData<T>` 非线程安全（P1）

`_histories` 是 `Stack<T>`，`Value` 的读-改-写没有锁；`HistoryDepth` 可运行期修改。并发写同一对象会损坏历史栈或产生错误 `IsChanged`。

**修复**：要么加锁/`Interlocked`（有成本），要么明确“业务对象实例不是线程安全的”并在调试构建加断言；更建议把历史做成可选能力（见 P2），默认只保留“上一个值”。

### C14. 其他正确性/健壮性点

- `CommonRule.Regular` 的 `new Regex(Expression)` 没有超时，用户可控模式可造成 ReDoS（库内模式虽多来自代码，但应设 `TimeSpan`）。
- `PropertyInfo<T>.CompareTo` 使用 `StringComparison.InvariantCulture` 排序属性名；应与 `BrokenRuleCollection.ClearRules(string)` 的 `Ordinal` 口径一致，避免仅大小写不同的属性错配。
- `ObjectPermissionCodeSource._cache` 用原始 `operation` 字符串做键，大小写不同会产生重复缓存项（`AttributeTypesOf` 是忽略大小写的）。
- `ObjectReflector.GetCandidateMethods` 只在“当前层没有任何候选”时才上溯基类；派生类里一个同名的私有辅助方法（如 `Update`）会让基类工厂方法整体消失，错误只在运行时以 `MissingMethodException` 出现。
- `ObjectReflector` 的工厂方法缓存键是“运行时实参类型拼出的字符串”，每次调用都要拼字符串、`GetParameters()`；不同类型实参会无限增长缓存。
- `ObservableDictionary` 继承 `Dictionary` 并用 `new` 隐藏成员：通过 `Dictionary` / `IDictionary` 引用修改不会触发事件，多态语义是破的（文档已承认，但这是设计问题，不是使用问题）。
- `MarkAsNew/Changed/Deleted` 没有状态迁移校验，任意状态可互相跳转；`MarkAsNew` 可作用于已持久化对象，造成重复插入。
- `BusinessObject.Dispose` 不释放 `FieldData<T>` 里的 `BehaviorSubject`；Rx 订阅会泄漏。
- `ObservableList` / `ObservableDictionary` 的 `_hookedItems` / 内部集合都非线程安全，却被放在“业务对象”库里。
- `EditableObject.SaveAsync` 允许工厂返回**不同实例**，此时 `this` 仍是脏的、`result` 是干净的，调用方稍后复用 `this` 会重复保存；语义不清晰。

---

## 4. 性能瓶颈

### 4.1 基准数据

方法：独立 console（Release、net10.0），预热 1k 次，取 200k 次平均；`GC.GetAllocatedBytesForCurrentThread()` 统计分配。对象为 10 属性 / 1 属性的 `EditableObject<T>`，无规则、无持久化。

| 操作 | 耗时/次 | 分配/次 |
|---|---:|---:|
| `new NoProp()` | 42 ns | 328 B |
| `new NoProp() + FieldManager` | 849 ns | 1.6 KB |
| `new TenProp() + FieldManager` | 1.09 µs | 2.1 KB |
| 首次读属性（1 属性对象） | 1.23 µs | 2.1 KB |
| 首次读属性（10 属性对象） | 0.92 µs | 2.4 KB |
| 首次写属性（1 属性） | 1.51 µs | 2.7 KB |
| 复用对象写属性（第 33 次后） | 0.51–0.66 µs | 464 B |
| 写相同值（泛型入口） | 126 ns | 0 B |
| 读已有属性 | 91 ns | 0 B |
| 写一个带 1 条属性规则的对象 | 2.18 µs | 1.5 KB |
| `ValidateAsync()`（无规则） | 848 ns | 800 B |
| `SaveAsync()`（脏对象，无真实持久化） | 4.53 µs | 2.6 KB |
| `new FieldData<string>()` | 30 ns | 176 B |
| 复用 `FieldData` 写值（超过深度 32） | 283 ns | 288 B |
| 同一 `FieldData` 连续写 64 次 | 9.3 µs / 64 次 | 10.5 KB / 64 次 |

对照：普通 POCO 属性写入通常 **1–2 ns / 0 B**。也就是说，Osba 的稳态写入比 POCO 慢 **300–600 倍**，每次写入多分配约 **0.5 KB**；带规则再乘 4。

### 4.2 根因（按影响排序）

#### P1. 每个字段一个 Rx `BehaviorSubject<T>`

`Reflection/FieldData.cs:48`。`FieldData<T>` 每个实例都 new 一个 `BehaviorSubject<T>`（176 B 起步），每次 `Value` set 走 `OnNext`。而 `ObservableValue` 在仓库内**没有任何订阅者**。

**修复**：把 `ObservableValue` 改为按需创建（`Lazy<IObservable<T>>`）或直接用 `PropertyChanged` 事件；删除 `System.Reactive` / `System.Interactive` 依赖。预期：每字段省 100–200 B，写入省约 100 ns。

#### P2. 历史栈的 O(depth) 裁剪

`Reflection/FieldData.cs:67-94`：每次写入都 `Push`；超过 `HistoryDepth`（默认 32）后，每次写入都 `ToArray()` → `Clear()` → 逐个 `Push`。稳态下**每一次属性写入都触发一次 32 元素数组分配 + 32 次入栈**。

实测 `FieldData` 复用写入 283 ns / 288 B，几乎全部来自 `TrimOldest`；64 次连续写分配 10.5 KB。

**修复**：改为环形缓冲区（`T[]` + head/count，O(1)），或把撤销历史改为显式开启（`EnableHistory`），默认只保留 1 个旧值。预期：写入从 0.5–0.7 µs 降到 **50–100 ns**，分配降到 **0 B**。

#### P3. 每个实例复制一份属性元数据

`Reflection/FieldDataManager.cs:51` 的 `CreateConsolidatedList` 在**每个对象实例**上：遍历继承链 → 每层 `PropertyInfoManager.GetLockedSnapshot`（`lock` + `new PropertyInfoList` 复制）→ `AddRange`。

`NoProp + FieldManager` 就分配 1.6 KB / 0.85 µs；10 属性对象 2.1 KB。

**修复**：把“合并后的属性列表 + 名称索引”做成 **按类型静态缓存**（`ImmutableArray<IPropertyInfo>` + `FrozenDictionary<string, IPropertyInfo>`），`FieldDataManager` 只持有值字典和类型元数据引用。预期：首次访问从 ~1.6 KB / 850 ns 降到 **< 400 B / < 150 ns**；对象创建成本接近纯对象。

#### P4. 值类型装箱与重复类型转换

`ObservableObject.SetProperty(PropertyInfo<T>, T)` → `LoadPropertyValue` → `FieldManager.SetFieldData(property, newValue)`（非泛型，`object`）→ `IFieldData.Value` setter（`object`）。`int/bool/DateTime` 每次写入至少一次装箱；字符串还会走 `TypeHelper.CoerceValue`。

**修复**：泛型路径保持泛型到 `IFieldData<T>`，删除非泛型重载或让它只走 `PropertyInfo<object>` 的兼容路径；统一 `PropertyInfo<T>.SetValue(object target, T value)` 编译委托。

#### P5. 规则热路径的分配与同步等待

`Rules.ResolvePropertyRules` 每次 setter 都：

- `RuleManager.Snapshot()`/索引查找；
- 实例规则/操作规则快照数组；
- `OrderBy(...).ToList()`；
- `new RuleContext`、`List<Task>`、`Task.WaitAll`。

一条最简规则就是 **2.2 µs / 1.5 KB**。而 `Task.WaitAll` 在 setter 上同步阻塞，本身就是设计风险。

**修复**：

- 按 `(type, property)` 缓存解析结果；无实例/操作规则时直接返回缓存数组（零分配）；
- 为 `IRuleBase` 增加同步 `Validate(context)` 快路径，真正同步的规则不创建 Task；
- 规则执行顺序固定后，用一个可复用的 `RuleContext`/结果缓冲（对象池化）；
- setter 上只做“纯同步、无 I/O”的检查，异步/重规则推迟到保存（文档已有此约定，但默认值仍偏重）。

预期：带 1 条规则的写入从 2.2 µs / 1.5 KB 降到 **0.2–0.3 µs / < 200 B**。

#### P6. 反射调用与缓存键

`BusinessObjectFactory` 每次操作：

- `ObjectReflector.GetMethodName` 拼字符串键；
- `_factoryMethods` 字典查找；
- `method.GetParameters()`（`NormalizeParameters`）；
- `MethodInfo.Invoke`（装箱参数数组）。

`SaveAsync` 稳态 4.5 µs 中，反射与规则/授权各占一部分。

**修复**：启动期为每个 `(类型, 操作, 参数类型组合)` 编译一个 `Func<object, object[], Task>`（表达式树或 `DynamicMethod`），缓存 `ParameterInfo[]` 与默认值；或提供 source generator，彻底消除运行时反射。预期：单次 `Save` 省 0.5–1 µs。

#### P7. 授权重复判定

`ObjectAuthorization.Enforce` 每条要求判定两次（`CanPerformOperation` + `requirements.All`）；`ScopeAuthorization` 在 `Save` 上判定两次（before + after）。一次保存至少 **3 次**完整授权链。

**修复**：

- `Enforce` 直接使用传入的 `requirements` 做一次判定，不再调用会重新收集要求的 `CanPerformOperation`（或让 `CanPerformOperation` 接受已收集的要求）；
- 把 `after` 判定限定在“范围列可能被业务方法修改”的操作（Create/Update），Delete/Execute 只在 before 判定；
- 在 `ScopeModel` 上标记范围列，只有范围列出现在 `ChangedProperties` 时才做 after 判定；
- 授权结果按请求缓存（引擎侧已有，Osba 侧避免重复调用）。

#### P8. `ChangedProperties` / `BrokenRuleCollection` / `IsBusy` 的分配

- 每次读 `ChangedProperties` 都复制整个列表（`[.. _changedProperties]`）；`EnsureRulesAsync` 每次保存都读一次。
- `BrokenRules` 继承 `ObservableCollection<BrokenRule>`，每次增删都触发 `CollectionChanged`/`PropertyChanged`，但服务端没有 UI 订阅者。
- `IsBusy` 遍历 `ConcurrentDictionary.Values`（`Any`）。
- `ResolveObjectRules` 每次保存都 `Snapshot().Where().Concat().OrderBy().ToList()`。

**修复**：`ChangedProperties` 返回只读视图或缓存快照；规则结果用 `List<BrokenRule>` + 计数，UI 绑定场景再包一层 Observable；`IsBusy` 用计数器；对象级规则解析结果按 `(type, scope 版本)` 缓存。

#### P9. sync-over-async

- `BusinessObjectFactory.Create/Fetch` 用 `AsyncContext.Run`（独立线程 + 同步上下文切换）；
- `Rules.CheckRules` 在 setter 上 `Task.Run(...).GetAwaiter().GetResult()`；
- `AuthorizationWarmup.Warm` 用 `AsyncContext.Run`。

这些在 ASP.NET Core（无同步上下文）里仍会占用/切换线程。**修复**：服务端只保留异步 API；同步 API 标 `[Obsolete]` 并由宿主决定是否阻塞；库内所有 `await` 加 `ConfigureAwait(false)`；同步入口直接 `GetAwaiter().GetResult()`（在无同步上下文前提下）而不是 `AsyncContext.Run` 的线程跳转。

#### P10. EF 物化成本

`FieldDataManager` 每实例创建 + 每个属性 `FieldData` + Rx Subject + 属性列表复制，导致物化 3 属性对象比 POCO 多约 3.6 KB。修复 P1/P3 后自然下降。

### 4.3 性能目标（建议写入 CI）

| 指标 | 现状 | 目标 |
|---|---:|---:|
| 属性写入（稳态，无规则） | 0.5–0.7 µs / 464 B | ≤ 100 ns / 0 B |
| 属性写入（1 条同步规则） | 2.2 µs / 1.5 KB | ≤ 300 ns / ≤ 128 B |
| 首次属性访问（10 属性对象） | 1.1 µs / 2.1 KB | ≤ 200 ns / ≤ 512 B |
| `SaveAsync`（无真实持久化） | 4.5 µs / 2.6 KB | ≤ 1 µs / ≤ 512 B |
| 对象创建（未触碰属性） | 42 ns / 328 B | ≤ 40 ns / ≤ 128 B |
| EF 物化（3 属性） | 9.7 KB/entity | ≤ 7 KB/entity |

---

## 5. 易用性（API 与开发体验）

| 编号 | 问题 | 建议 |
|---|---|---|
| U1 | `AddBusinessObject` 返回 `void`，无法链式调用 | 返回 `IServiceCollection`；提供 `AddOsba(o => ...)` |
| U2 | `RegisterProperty<TValue>` 必须显式泛型、必须用返回值、必须 `static readonly`，否则运行时抛 | Source Generator 生成；分析器检测“未使用返回值” |
| U3 | 工厂方法靠命名约定 + 可选参数 + 私有方法 + 运行时反射，拼错/漏写只在运行时暴露 | 启动期校验 + 生成代码；错误信息列出候选方法 |
| U4 | `criteria` 是 `params object[]`，无编译期类型检查，重载打分规则不透明 | 泛型 `CreateAsync<T>(...)` / 强类型请求对象；或生成强类型方法 |
| U5 | `Create`/`CreateAsync`/`InsertAsync`/`SaveAsync` 四条创建/保存路径语义不同 | 收敛为 `Create`（构造）+ `Save`（持久化）；删除 `InsertAsync(criteria)` |
| U6 | `AddRule`（全局）/`AddInstanceRule`（实例）/`WithRule`（操作）三条注册路径 | 默认只保留后两者；全局注册移到容器级注册表 |
| U7 | `Execute(ct)` 与 `ExecuteAsync(ct)` 两个 `ct` 位置 | 改名：`WithCriteria(...)` / `ExecuteAsync(ct)` |
| U8 | `IsValid` 首次检查前恒为 `true`；`IsSavable` 对刚加载对象为 `true` | 引入 `ValidationState { NotValidated, Valid, Invalid }`；不要用 `bool` 表达三态 |
| U9 | 缺少启动期诊断：忘记 `[Permission]`、缺少 ScopeModel、漏写工厂方法都晚爆 | `IOsbaStartupValidator` 聚合报告，`ValidateOnBuild` 风格 |
| U10 | `[Inject]` 属性注入是隐藏依赖，且解析失败只 `Debug.WriteLine` | 支持构造函数注入；`[Inject]` 标 `[Obsolete]`；缺失时启动期失败 |
| U11 | 诊断只有 `Debug`/`Trace`，Release 下可能零痕迹 | 接 `ILogger`；加 `ActivitySource` + `Meter`（save/rules/auth 三个维度） |
| U12 | 文档与实现漂移：README 的继承图错误（`CommandObject`/`ReadOnlyObject` 并不继承 `EditableObject`）、文档说 .NET 8+ 实际 net9/net10、模块图说 Osba→Domain 实际不引用 | 文档按代码生成/CI 校验；补英文 README 与 API 示例 |
| U13 | `[Required]` 等 DataAnnotations 与 `CommonRule.Required` 两套校验入口 | 明确唯一推荐；或在 `AddDataAnnotations` 时去重 |
| U14 | 8 种语言的资源文件，但多处异常消息是硬编码英文（`"Object not valid for save."`、`"Rule message is required"`） | 全部进 resx；CI 校验 key 完整性 |
| U15 | `Resources.resources` 二进制被提交进源码，`Resources.Designer.cs` 依赖自定义 MSBuild target 生成 | 删除二进制；用标准 `EmbeddedResource` + `PublicResXFileCodeGenerator`；CI 校验生成物 |

---

## 6. 如何在“纯 DDD”架构中使用

### 6.1 适配度评估

| DDD 要求 | Osba 现状 | 结论 |
|---|---|---|
| 领域对象不依赖基础设施 | `SaveAsync` → `IObjectFactory`；`[Inject]`；`BusinessContext.GetRequiredService` | ❌ 不满足 |
| 聚合根不可分割、通过方法修改 | 属性 setter 公开；状态可任意标记 | ⚠️ 需强约束 |
| 不变量不可绕过 | 规则可 `Suspend/Bypass` | ❌ 规则不能当不变量 |
| 领域事件 | 无；示例复制了 `Aggregate<TKey>` 的事件代码 | ❌ 需适配 |
| 仓储/工作单元 | 工厂方法内直接 `SaveChanges`；无 UoW 边界 | ❌ 不满足 |
| 持久化无关 | 值存在字典里，EF 物化即脏；无映射支持 | ❌ 需适配 |
| 用例边界授权 | `[Permission]` 在实体工厂方法上 | ⚠️ 可接受但非纯 DDD |

**结论**：直接把 `EditableObject<T>` 当聚合根基类使用，会得到一个“能跑但纯度不高”的模型；这与示例工程的做法一致。若团队要求“纯粹 DDD”，应把 Osba 降级为**应用层编辑模型**，让领域层保持 POCO。

### 6.2 方案 A（推荐，纯 DDD）：Osba 只做“编辑模型 / 命令模型”

```text
Web API
  └─ Application（用例、事务、授权、日志）
       ├─ Domain（纯 POCO：Aggregate<Guid>、值对象、领域事件、不变量）
       ├─ EditModel（Osba：EditableObject + 规则 + [Permission]）
       └─ Infrastructure（EF Core / Dapper / Repository / Outbox）
```

流程：

1. 用例从仓储加载**纯领域聚合**（`Project : Aggregate<Guid>`）；
2. 授权在用例入口完成（`[Permission]` 标在用例/处理器上，或显式 `IScopeGuard`）；
3. 把领域聚合映射为 `ProjectEditModel : EditableObject<ProjectEditModel>`（只含本次可编辑字段）；
4. `Handle` 修改 EditModel → `ValidateAsync`（Osba 规则 = 输入校验）；
5. 把 EditModel 的变更应用回领域聚合的**领域方法**（`project.Rename(...)`，不变量在这里抛 `DomainException`）；
6. `IUnitOfWork.SaveChangesAsync()`（事务边界在应用层，不在实体）；
7. 领域事件经 Outbox 在提交后发布。

优点：领域层零 Osba 依赖、可单测、可演化为事件溯源；Osba 的价值（变更追踪、表单校验、权限边界）用在最合适的地方。
代价：EditModel ↔ Domain 的映射样板；用 Mapster/源生成器消解。

### 6.3 方案 B（务实）：Osba 作为聚合根

如果团队接受“聚合根 = Osba 对象”，至少做到：

1. **属性 setter 私有化**，只暴露领域方法：`project.Rename(name)`；Osba 的 `SetProperty` 仍在基类里，供方法内部使用。
2. **规则只做输入校验，不变量用领域方法 + 异常**：`Rules` 可被绕过，不能承担“余额不能为负”这类不变量。
3. **仓储接口注入**，不要在领域方法里 `BusinessContext.GetRequiredService<...>`；构造函数注入 `IProjectStore`（Osba 的 `ActivatorUtilities.GetServiceOrCreateInstance` 支持构造函数注入，只是示例没用）。
4. **不要在实体里 `SaveChanges`**：工厂方法只做对象装配/校验；持久化交给 `IUnitOfWork` / `SaveAsync` 的工厂方法调仓储的 `Add/Update`（返回变更集），事务由外层提交。
5. **删除 `[FactoryInsert/Update/Delete]` 里的业务分支**：工厂方法应是“持久化适配器”，业务规则放领域方法。
6. **用适配包拿回 DDD 词汇**：`Euonia.Osba.Domain` 提供 `OsbaAggregate<T, TKey>`（组合 `IAggregateRoot` + 事件缓冲 + UoW），而不是在每个项目里复制 95 行。
7. **授权上移**：`[Permission]` 标在应用服务方法上；实体内部只用 `CanPerformOperation` 做 UI 提示（它无从判定时返回 `true`，不是安全闸门）。
8. **EF 物化修复后再上生产**（见 C7），并且写操作只更新 `ChangedProperties`。

### 6.4 查询侧（CQRS）

- **不要**用 Osba 对象做列表查询：每次物化 2–10 KB、还会标脏。
- 读侧用 `IQueryable` 投影到 `record`/DTO，配合 `IScopeGuard.Apply(query)` 下推行级权限。
- 单行详情需要编辑时才 `Fetch`；列表 → DTO。

### 6.5 与 `Euonia.Domain` 的冲突与适配

- `EditableObject<T>` 要求 `T : EditableObject<T>`，无法同时继承 `Aggregate<TKey>`；示例用复制粘贴解决，这是技术债。
- 建议新增包 `Euonia.Osba.Domain`：
  - `OsbaAggregate<T, TKey> : EditableObject<T>, IAggregateRoot<TKey>, IHasDomainEvents`（组合事件缓冲，不复制）；
  - `IObjectFactory` 保存后统一 flush 领域事件到 Outbox；
  - `Actuator` 默认挂 UoW 行为（`IUnitOfWork` 提交/回滚）。
- 或者反向：`Euonia.Domain` 提供 `IDomainEventBuffer` 组合对象，Osba 通过 `IUseDomainEvents` 接口接入，避免继承。

### 6.6 反模式清单（在 DDD 项目中禁用）

1. 在 `AddRules()` 里写业务不变量（可被 `BypassRuleChecks` 绕过）。
2. 在领域方法里 `BusinessContext.GetRequiredService<DbContext>()`。
3. 用 `Create/Insert(criteria)` 低层入口做写操作（先写后验）。
4. 把 Osba 对象直接返回给 API 层（泄露内部状态与权限方法）。
5. 用 Osba 对象做读模型/列表。
6. 在 `Handle` 里调用 `SaveAsync`（执行器会在终结阶段再保存一次）。
7. 依赖 `RuleManager` 的全局规则做多租户差异化校验。
8. 把 `[Permission]` 当成唯一安全边界而不在应用层复核（尤其 `CanPerformOperation` 的 `true` 语义）。

---

## 7. 其他问题

### 7.1 安全

- **`LoadProperty` 是 public 且绕过 `CanWriteProperty`**：只读对象可被改写（C5）；任何宿主代码都能改任意业务对象。建议收窄为 internal + `IObjectMaterializer`。
- **服务定位器**：`BusinessContext.GetRequiredService<T>()` / `CurrentServiceProvider` 对领域代码开放，等于把容器交给业务代码；难以审计依赖。
- **环境上下文泄漏**：`BusinessContextAccessor` 的 AsyncLocal 会流入后台任务；请求结束后 provider 已释放。建议禁止在后台任务使用环境上下文，强制 `IServiceScopeFactory`。
- **`Explain` 信息进入异常消息**：行级拒绝消息包含策略解释，可能泄露组织结构/维度值；需要分级（生产只给 correlation id，详细原因进日志）。
- **ReDoS**：`CommonRule.Regular` 无超时。
- **弱事件**：安全审计/通知订阅可能静默失效（C12）。
- **权限码来源默认单例**：`ObjectPermissionCodeSource.Instance` 的缓存按 `Type` 持有，插件卸载会泄漏；多租户下若规则不同需重新设计。

### 7.2 工程/CI

- `dotnet test` 当前 0 用例（见 1.2），CI 会误判绿色。
- 没有 Osba 的 BenchmarkDotNet 项目（只有 Bus）。
- 没有 EF Core 映射/部分更新/物化测试（这正好漏掉了 C7）。
- 没有多容器/多租户规则隔离测试（漏掉 D2）。
- `System.Interactive` 未使用；`System.Reactive` 只为一个类型服务。
- `netstandard` 条件编译块永远不会命中（`Lock` 类型要求 .NET 9+），是死代码。
- `Resources.resources` 二进制入库；`Resources.Designer.cs` 由自定义 target 生成，存在漂移风险。
- 文档：模块图/继承图与代码不一致；`SAMPLE.md` 991 行但无英文版；`docs/dotnet/osba/` 目录不存在（overview 里的链接是断的）。
- 版本 2026.4.5 采用 CalVer，但公共 API 无 `PublicAPI.Shipped.txt` 基线，破坏性变更无法自动检测。

### 7.3 可观测性

- 没有 `ActivitySource` / `Meter`：保存、规则、授权耗时无法在 APM 中切分。
- 日志用 `Debug.WriteLine`/`Trace.WriteLine`，Release 下默认无输出；`[Inject]` 缺失、程序集加载失败等只留下 `Trace`。
- 建议指标：`osba.save.duration`、`osba.rule.duration{rule}`、`osba.auth.duration{operation}`、`osba.rule.failures`、`osba.objects.materialized`；Activity 标签带对象类型与操作（不帶 PII）。

### 7.4 代码结构

- `BusinessObject` 1123 行，混合了属性存储、规则、权限、事件、反射、生命周期；建议拆成 `PropertyStore` / `RuleHost` / `AuthorizationFacade` / `ObjectLifecycle` 四个协作者。
- `ReadOnlyObject` 360 行几乎是 `ObservableObject` 的复制；`GetProperty` 有 5 个重载在 `BusinessObject`/`ObservableObject`/`ReadOnlyObject` 三处重复。
- `ReadOnlyObject<T>` 是 `public class`（非 abstract），却带 `where T : ReadOnlyObject<T>`，可直接实例化出无意义类型。
- `PropertyInfoList : List<IPropertyInfo>`、`ObservableDictionary : Dictionary<,>` 都是“继承具体集合 + 隐藏成员”，应改为组合/只读包装。
- `RuleBase.RelatedProperties` 默认 `new List<>()` 且可变，规则实例是共享单例，任何一次误写都会跨对象泄漏。

---

## 8. 修复方案与计划

### 8.1 原则

1. **先补回归测试，再改行为**：每个 C*/P* 问题对应一个失败测试，修复后保留。
2. **行为变更用开关过渡**：`OsbaOptions`（`StrictFactoryMethods`、`EnableEfMaterialization`、`EnableLegacyRefStorage`、`EnableRuleHistory`），默认值在下一个大版本收紧。
3. **性能改动必须带基准**：每个 P1 项有 benchmark 与预算，CI 回归超 20% 失败。
4. **公共 API 只做加法**：破坏性变更走 major（2027.x），先 `[Obsolete]` 两个 minor。
5. **一次只动一个关注点**：先正确性，再性能，最后 API/DDD 适配，避免“大爆炸重构”。

### 8.2 P0：正确性与安全（建议 5–8 人日）

| ID | 动作 | 验收 |
|---|---|---|
| P0-1 | 统一 awaitable 识别：`Task`/`Task<T>`/`ValueTask`/`ValueTask<T>`；不支持则启动期失败；`ExceptionDispatchInfo` 解包 | ValueTask 工厂方法测试；异常类型不被包装 |
| P0-2 | 基类工厂方法改抛 `NotSupportedException`，或启动期强制校验；提供 `StrictFactoryMethods` 开关 | 缺 `[FactoryInsert]` 的保存必须失败；旧行为有显式开关 |
| P0-3 | 统一存储模型：`ReadProperty` 在字段模式走真实字段；或废弃 ref 重载并把文档/测试切到字典模式（二选一，先决策） | 字段模式下 `[Required]`/正则/lambda 规则全部读到真实值 |
| P0-4 | EF 物化：`IMaterializationInterceptor` + “物化中”标记 + `AcceptChanges`；`ApplyChanges` 只更新变更列 | 物化后 `ChangedProperties=0`、`IsSavable=false`；部分更新 SQL 只含变更列 |
| P0-5 | 删除 `*Async(criteria)` 的“先写后验”：受约束类型禁止该入口，或改为“解析目标行 → before 判定 → 写入” | 越权写入不产生任何 DB 变更（用 SQL 计数断言） |
| P0-6 | `LoadProperty` 收窄为 internal/受控物化入口；`ReadOnlyObject` 写保护生效 | 外部代码无法通过 public API 改只读对象 |
| P0-7 | `RuleScope` 改为栈/AsyncLocal；嵌套/并发执行器测试 | 嵌套操作外层规则不丢；并发互不可见 |
| P0-8 | 删除后 `AcceptChanges`，重复保存幂等 | 第二次 `SaveAsync` 不触发 Delete |
| P0-9 | `BusinessContextAccessor` 去掉构造副作用；显式 `SetCurrent`/`Clear`；后台任务禁用环境上下文 | 两个并行 scope 不串；请求结束后解析抛明确错误 |
| P0-10 | 非泛型 `SetProperty` 做值比较；泛型路径保持泛型到 `IFieldData<T>` | 同值赋值不标脏、不发事件、零分配 |

### 8.3 P1：性能与热路径（建议 8–12 人日）

| ID | 动作 | 预期收益 |
|---|---|---|
| P1-1 | `FieldData` 去掉 `BehaviorSubject`，`ObservableValue` 按需创建；移除 Rx/Interactive 依赖 | -100~200 B/字段，-100 ns/写 |
| P1-2 | 历史改为环形缓冲或默认关闭；`TrimOldest` 删除 | 写入 0.5–0.7 µs → 50–100 ns；464 B → 0 B |
| P1-3 | 类型级属性元数据静态缓存（列表 + 名称索引 + 编译访问器） | 首次访问 1.6 KB → < 400 B；对象创建接近 POCO |
| P1-4 | 规则解析缓存 + 同步规则快路径 + 对象池化 `RuleContext` | 带规则写入 2.2 µs/1.5 KB → ≤ 300 ns/128 B |
| P1-5 | 工厂方法编译委托缓存（或 source generator） | Save 省 0.5–1 µs；消除缓存键字符串 |
| P1-6 | 授权去重：一次收集、一次判定；after 判定按范围列脏检查 | Save 省 2–3 次授权链 |
| P1-7 | `ChangedProperties` 只读视图、`BrokenRules` 用 List+计数、`IsBusy` 计数器、对象级规则缓存 | Validate/Save 分配减半 |
| P1-8 | 移除 `AsyncContext.Run`，库内 `ConfigureAwait(false)`，同步 API 标 `[Obsolete]` | 线程跳转减少；死锁面缩小 |
| P1-9 | 新增 `Benchmarks/Euonia.Osba.Benchmarks`（创建/读写/规则/保存/授权/EF 物化） | CI 性能门禁 |
| P1-10 | `SaveAsync` 只更新 `ChangedProperties` 的示例与 EF 扩展 | 避免全列更新与乐观并发冲突 |

### 8.4 P2：易用性与 API（建议 5–7 人日）

| ID | 动作 |
|---|---|
| P2-1 | `AddBusinessObject` 返回 `IServiceCollection`；新增 `AddOsba(Action<OsbaOptions>)` |
| P2-2 | `OsbaOptions`：严格工厂方法、EF 物化、规则历史、异常解包、诊断级别 |
| P2-3 | 启动期校验器：工厂方法、属性注册、`[Permission]` 来源、ScopeModel、`[Inject]` 可解析性；聚合报告 |
| P2-4 | 构造函数注入优先；`[Inject]` 标 `[Obsolete]` |
| P2-5 | `ValidationState` 三态；`IsSavable` 对干净对象为 false |
| P2-6 | 收敛创建/保存 API（`Create` + `Save`）；`InsertAsync(criteria)` 标 `[Obsolete]` |
| P2-7 | 规则注册收敛：容器级 `IRuleRegistry` + 实例级 + 操作级；全局 `RuleManager` 降为兼容层 |
| P2-8 | 公共 API 收窄：`FieldDataManager`/`RuleManager`/`PropertyInfoList`/`BrokenRuleCollection` 改 internal；引入 `PublicAPI.Shipped.txt` |
| P2-9 | 用 `ILogger`/`ActivitySource`/`Meter` 替换 `Debug`/`Trace` |
| P2-10 | 文档修正（继承图、.NET 版本、模块图、`docs/dotnet/osba` 断链）、英文 README、迁移指南 |

### 8.5 P3：DDD 适配（建议 10–15 人日）

| ID | 动作 |
|---|---|
| P3-1 | 决策：Osba 定位为“编辑模型”（方案 A）还是“聚合根”（方案 B）；写入 ADR |
| P3-2 | 新增 `Euonia.Osba.Domain`：`OsbaAggregate<T,TKey>`（组合事件缓冲/UoW），不复制 `Aggregate<TKey>` |
| P3-3 | 新增 `Euonia.Osba.EntityFrameworkCore`：映射约定、物化拦截器、`ApplyChanges`、并发令牌、值转换 |
| P3-4 | 仓储抽象 `IObjectStore<T>` + 示例；实体不再直接 `GetRequiredService` |
| P3-5 | `Actuator` 默认 UoW 行为：提交/回滚/领域事件 flush（Outbox） |
| P3-6 | 用例/应用服务层授权范式：`[Permission]` 上移、`IScopeGuard` 查询过滤、`CanPerformOperation` 仅做 UI 提示 |
| P3-7 | 提供两个端到端示例：A（纯 DDD + Osba 编辑模型）与 B（Osba 聚合 + EF） |

### 8.6 P4：工具与长期演进（建议 10+ 人日）

- Source Generator：`RegisterProperty`、`Get/SetProperty`、工厂方法分发、`[Inject]` 构造注入。
- Analyzer：`RegisterProperty` 返回值未使用、`AddRule` 出现在 `AddRules` 之外、`async void`、`[Permission]` 标在非工厂方法、同值 `SetProperty`。
- 架构测试（NetArchTest）：Domain 不得引用 Osba/EF；Osba 不得引用 Security。
- 压测/混沌：并发执行器、规则、授权缓存失效、EF 并发冲突。
- 兼容性：`PublicAPI` 基线 + SemVer/CalVer 检查。

### 8.7 建议里程碑

```text
M1（1 周）  P0-1/2/3/7/8/9/10 + 回归测试 + 构建/测试修复
M2（2 周）  P0-4/5/6 + EF 适配包雏形 + 安全回归
M3（3 周）  P1 全部 + Benchmark 基线 + CI 性能门禁
M4（2 周）  P2 全部 + 文档/诊断/启动校验
M5（3 周）  P3 适配包 + 两个 DDD 示例 + 架构测试
M6（持续）  P4 Source Generator / Analyzer
```

> 顺序理由：C2/C1/C3 会造成“静默数据丢失/错误校验”，必须先修；C7/C8 决定能否安全上生产；性能改动依赖存储模型收敛（D1），否则会在两套模型上重复优化。

### 8.8 风险与回滚

| 风险 | 缓解 |
|---|---|
| 统一存储模型是破坏性变更 | 先加 `EnableLegacyRefStorage` 开关；两个 minor 版本 `[Obsolete]`；major 移除 |
| 严格工厂方法导致旧代码启动失败 | 启动期给出完整清单与修法；开关可临时关闭 |
| EF 物化拦截器影响所有查询 | 只在 `AddOsbaEntityFrameworkCore()` 启用；提供 `SkipOsbaMaterialization` 分支 |
| 规则注册表从静态改为容器级 | 保留静态兜底；提供迁移指南与诊断日志 |
| 性能优化改变事件时序 | 用现有 240 测试 + 新增时序测试锁定；`PropertyChanged` 次数/顺序断言 |

---

## 9. 验收标准

### 9.1 行为

- 现有 240 个测试全部通过；`dotnet test` 能真正发现并运行它们。
- 每个 P0 缺陷至少 1 个回归测试（失败→通过）。
- 新增：EF 物化/部分更新、字段模式规则、嵌套执行器、并发规则、删除幂等、ValueTask 工厂、缺省工厂方法、只读对象写保护。
- 新增：多容器/多租户规则隔离、启动期校验失败信息。

### 9.2 性能

- CI 基准达到 4.3 的目标值，且连续 3 次运行波动 < 10%。
- `dotnet-counters` 下 1k RPS 保存场景，Gen0 分配/请求 ≤ 2 KB（不含业务持久化）。
- EF 物化 3 属性对象 ≤ 7 KB/entity。

### 9.3 架构

- `Euonia.Osba` 不引用 `Euonia.Security`、`Euonia.Domain`、EF Core（适配包除外）。
- 领域层示例程序集不引用 Osba/EF（方案 A）。
- `PublicAPI.Shipped.txt` 建立，破坏性变更可被 CI 捕获。

### 9.4 文档

- README 继承图、目标框架、模块图与代码一致。
- `docs/dotnet/osba/` 存在且链接可达。
- 新增 `MIGRATION-2027.md`（存储模型/严格工厂方法/规则注册表）。

---

## 10. 附录

### A. 基准与复现方法

基准程序为独立 console（不属于仓库），引用 `Euonia.Osba.csproj`，Release/net10.0：

```csharp
static void Measure(string name, int n, Action<int> body)
{
    for (var i = 0; i < 1_000; i++) body(i);                 // 预热
    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
    var before = GC.GetAllocatedBytesForCurrentThread();
    var sw = Stopwatch.StartNew();
    for (var i = 0; i < n; i++) body(i);
    sw.Stop();
    var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Console.WriteLine($"{name}: {sw.Elapsed.TotalMilliseconds * 1_000_000 / n:F1} ns/op, {allocated / (double)n:F0} B/op");
}
```

关键复现点：

- 10 属性对象首次读：`new TenProp()` + `x.A`；
- 稳态写：复用同一对象写第 33 次以后；
- 规则路径：`AddRules` 中 `Rules.AddRule(new CommonRule.Required(P, "required"))`；
- 保存路径：`MarkAsNew()` + `SaveAsync()`，工厂方法只 `Task.CompletedTask`；
- EF：`DbContext` + SQLite in-memory，`AsNoTracking().SingleAsync()`，比较 `ChangedProperties.Count` 与同结构 POCO 的分配。

### B. 关键文件索引

| 主题 | 文件 |
|---|---|
| 对象基类/属性存储 | `Core/BusinessObject.cs`、`Core/ObservableObject.cs`、`Core/ReadOnlyObject.cs` |
| 字段/历史/Rx | `Reflection/FieldData.cs`、`Reflection/FieldDataManager.cs` |
| 工厂/反射 | `Factory/BusinessObjectFactory.cs`、`Factory/ObjectReflector.cs` |
| 规则 | `Rules/Rules.cs`、`Rules/RuleManager.cs`、`Rules/RuleContext.cs`、`Rules/BrokenRuleCollection.cs` |
| 授权 | `Permission/ObjectAuthorization.cs`、`Permission/Scope/ScopeAuthorization.cs`、`Permission/ScopeOperationMap.cs` |
| 执行器 | `Actuators/ActuatorBase.cs`、`Actuators/*Actuator.cs` |
| 上下文 | `Core/BusinessContext.cs`、`Core/BusinessContextAccessor.cs` |
| 文档 | `SAMPLE.md`、`PERMISSION.md`、`PERMISSION-DESIGN.md`、`PERMISSION-SAMPLE.md` |

### C. 建议新增的测试清单

```text
[ ] ValueTask / ValueTask<T> 工厂方法被 await
[ ] 缺省工厂方法 -> NotSupportedException（或启动期失败）
[ ] 字段模式 + DataAnnotation/CommonRule 读到真实值
[ ] 非泛型 SetProperty 同值不标脏、不发事件
[ ] ReadOnlyObject 的 LoadProperty 被拒绝
[ ] 删除两次只触发一次 Delete；第二次 no-op
[ ] EF 物化后 ChangedProperties 为空、IsSavable=false
[ ] EF 部分更新只生成变更列的 UPDATE
[ ] Insert/Update/Delete(criteria) 越权时 DB 无变更
[ ] 嵌套执行器外层 RuleScope 不丢
[ ] 并发执行器 RuleScope 互不可见
[ ] 并发 SetProperty 不损坏 FieldData 历史
[ ] Save 的 before/after 授权各只判定一次（或按范围列跳过）
[ ] 业务异常不被 TargetInvocationException 包裹
[ ] 两个并行 scope 的 BusinessContext 不串
[ ] 弱/强事件订阅的生命周期语义
[ ] 多容器规则隔离
[ ] 启动期校验覆盖：工厂方法/属性/权限/注入
```

---

## 11. 实施记录（M1 + M3）

> 本节记录本轮已经落地的改动。约束：不引入 EF Core、UoW、Domain 依赖；实际项目可自行组合。

### 11.1 M1（正确性）已完成

| 项 | 改动 |
|---|---|
| P0-1 | `BusinessObjectFactory` 改为直接检查返回值形态，支持 `Task` / `Task<T>` / `ValueTask` / `ValueTask<T>`；`TargetInvocationException` 用 `ExceptionDispatchInfo` 解包 |
| P0-2 | 解析到 `EditableObject<T>` / `CommandObject<T>` 的**基类空实现**时抛 `NotSupportedException`；`Create` 保留“空对象构造”语义 |
| P0-3 | `ref` 字段写入同步到 `FieldData`，规则/DataAnnotations 通过 `ReadProperty` 读到真实值（统一两种存储视图） |
| P0-6 | `LoadProperty` 增加 `CanLoadProperty` 钩子；工厂方法执行期间自动进入装载作用域，只读对象仅在作用域内允许装载，外部写入抛 `SecurityException` |
| P0-7 | `RuleScope` 由单槽位改为栈：嵌套执行器/并发操作先结束的一层不会清掉其他层 |
| P0-8 | 删除成功后复位为 `None`，第二次 `SaveAsync` 为幂等 no-op |
| P0-9 | `BusinessContextAccessor` 构造函数不再写静态 `AsyncLocal`；必须显式 `SetCurrent` / `Clear` |
| P0-10 | 非泛型 `SetProperty(IPropertyInfo, object)` 做同值比较，与泛型入口一致 |

**未完成（受约束或需要宿主协作）**：

- **P0-4（EF 物化即脏）**：按要求不引入 EF Core；等宿主接入持久化时再实现 `IMaterializationInterceptor` / `ApplyChanges` 适配包。
- **P0-5（criteria 写入口先写后验）**：在“没有持久化抽象”的约束下，Osba 无法在写入前知道目标行。当前保留后置判定，并在受数据权限约束的类型上通过 `Trace` 发出明确告警；彻底修复需要宿主提供 `criteria → 目标行` 的解析钩子，或改用 `Fetch → 判定 → Save`（建议写入下一阶段）。

### 11.2 M3（性能）已完成

| 项 | 改动 | 效果（同机 Release/net10，单线程） |
|---|---|---|
| P1-1 | 删除 Rx `BehaviorSubject`，改为按需创建的内置 `IObservable<T>`；移除 `System.Reactive` / `System.Interactive` 依赖 | 每字段少 72 B；未订阅零开销 |
| P1-2 | 历史改为环形缓冲区，O(1) 入栈/裁剪/撤销；装载不记历史 | `FieldData` 复用写入：283 ns / 288 B → **22 ns / 0 B** |
| P1-3 | 属性元数据按类型静态缓存（属性数组 + 名称索引）；字段值改为按稳定索引的数组 | 首次属性读：1.2 µs / 2.1 KB → **0.28 µs / 0.54 KB** |
| P1-4 | 规则状态特性缓存、同步规则无状态机快路径、空任务/空列表复用、实例/操作规则无锁快路径 | 带 1 条规则的写入：2.2 µs / 1.5 KB → **0.79 µs / 0.30 KB** |
| P1-5 | 0/1 实参工厂方法改为强类型缓存键；参数形态缓存；不再每次拼字符串 | `SaveAsync`：4.5 µs / 2.6 KB → **2.25 µs / 1.14 KB** |
| P1-6 | 授权要求只判定一次；`Delete/Execute` 不再做无意义的 after 判定 | 每次保存少 1–2 次授权链 |
| P1-7 | `ChangedProperties` 快照缓存、`BrokenRules` 惰性创建、`IsBusy` 不创建管理器、`IsValid` 不创建规则集合 | `ValidateAsync`：0.85 µs / 0.8 KB → **0.36 µs / 0.26 KB** |
| P1-8 | 同步入口无同步上下文时直接等待，不再无条件走 `AsyncContext`；工厂异步调用 `ConfigureAwait(false)` | 服务端少一次线程跳转 |
| P1-9 | 新增 `Benchmarks/Euonia.Osba.Benchmarks`（对象生命周期 / 规则 / 保存） | 可作为 CI 门禁 |

**未完成**：

- **P1-10（EF 部分更新）**：同 P0-4，属于持久化适配包，本轮不引入 EF Core。

### 11.3 回归结果

```text
Euonia.Osba.Tests            258 passed（M1+M3 为 253，M4 新增 5）
Euonia.Osba.Standalone.Tests   7 passed
Euonia.slnx                  Build succeeded, 0 CS warning
```

新增回归覆盖：ValueTask 工厂、缺失 Insert/Execute、异常解包、字段模式规则、非泛型同值写入、只读对象装载作用域、规则范围嵌套、历史深度收缩、`LoadProperty` 复位、`ObservableValue`、三态校验、容器级规则隔离、选项注册、启动期 `[Inject]` 诊断。

### 11.4 性能复测（同一基准程序，稳态）

| 操作 | 改造前 | 改造后 |
|---|---:|---:|
| 首次读属性（1 属性对象） | 1.23 µs / 2.1 KB | **0.28 µs / 0.54 KB** |
| 稳态写属性 | 0.66 µs / 464 B | **0.24 µs / 24 B** |
| 写同值（泛型入口） | 0.13 µs / 0 B | **0.10 µs / 0 B** |
| 带 1 条属性规则写入 | 2.18 µs / 1.5 KB | **0.79 µs / 0.30 KB** |
| `ValidateAsync()` | 0.85 µs / 0.8 KB | **0.36 µs / 0.26 KB** |
| `SaveAsync()`（无真实持久化） | 4.53 µs / 2.6 KB | **2.25 µs / 1.14 KB** |
| `FieldData` 复用写入 | 283 ns / 288 B | **22 ns / 0 B** |

剩余 24 B/写入的分配来自属性变更通知链上的固定对象；已低于“每写 ≤ 64 B”的预算，但离“零分配”仍有一步，可作为下一轮的微优化目标。

---

## 12. 实施记录（M4）

### 12.1 已完成

| 项 | 改动 |
|---|---|
| P2-1 | `AddBusinessObject` 返回 `IServiceCollection`；新增 `AddBusinessObject(Action<OsbaOptions>, params Assembly[])` |
| P2-2 | `OsbaOptions`：`ValidateOnStartup`、`StrictFactoryMethods`、`EnableDiagnostics`、`DiagnosticLevel`；`BusinessObjectFactory` 读取容器选项 |
| P2-3 | `OsbaStartupValidator`：工厂签名、`[Inject]` 可解析性、属性元数据、缺失持久化方法；`OsbaConfigurationException` 聚合 Error |
| P2-4 | 构造函数注入由 `ActivatorUtilities` 原生支持；新增回归测试；`[Inject]` 保留兼容，文档改为“优先构造注入” |
| P2-5 | `ValidationState` 三态；`ValidateAsync`/`EnsureValidAsync`/`EnsureRulesAsync`/属性变更维护状态 |
| P2-6 | `InsertAsync(criteria)`、`ExecuteAsync(criteria)` 标记 `[Obsolete]`，引导 `Create + Save` / `Create + Execute(target)` |
| P2-7 | 新增 `IRuleRegistry`/`RuleRegistry`：规则共享边界改为容器；无 `BusinessContext` 回退进程级 `RuleManager` |
| P2-9 | `ActivitySource("Euonia.Osba")`、`Meter("Euonia.Osba")`、保存/授权/规则耗时与失败计数；`ILoggerFactory` 优先、`Trace` 回退 |
| P2-10 | README/SAMPLE/CHANGELOG 更新：继承图、规则边界、显式上下文、删除幂等、ValueTask 工厂、启动校验 |

### 12.2 暂缓

- **P2-8 公共 API 收窄**（把 `FieldDataManager`/`RuleManager`/`PropertyInfoList`/`BrokenRuleCollection` 改为 internal）：
  这些类型已经通过 `IBusinessObject.FieldManager`、`GetBrokenRules()`、`PropertyInfoManager` 暴露，
  直接收窄是二进制破坏性变更；建议下一个 major 版本配合 `PublicAPI.Shipped.txt` 一起做。
- **Source Generator / Analyzer**：属于 P4 工具链，本轮未做。
- **`[Inject]` 标记 Obsolete**：会波及 Modularity 与其他模块的编译警告；先通过启动校验与文档引导。

### 12.3 验证

```text
Euonia.Osba.Tests            258 passed（原 240 + 18 新增）
Euonia.Osba.Standalone.Tests   7 passed
Euonia.slnx                  Build succeeded, 0 CS warning
```

新增 M4 覆盖：三态校验、容器级规则隔离、`AddBusinessObject` 链式返回与选项、启动期 `[Inject]` 诊断。

---

## 13. 最终建议

1. **短期（下一个 minor）**：先修 C1/C2/C3/C6/C7/C8/C9，并把 `dotnet test` 修好；这些是“静默错误/数据丢失/越权已落库”级别的问题，优先级高于任何重构。
2. **中期（下一个 minor）**：做 D1 的存储模型决策与 P1 的性能收敛。没有这一步，Osba 不适合高吞吐服务端；做了之后，它可以进入“每个请求几十个对象”的舒适区。
3. **长期（major）**：把 Osba 明确定位为“编辑模型/命令模型”，补 `Euonia.Osba.Domain` 与 `Euonia.Osba.EntityFrameworkCore` 适配包；纯 DDD 项目按方案 A 使用，务实项目按方案 B 使用，并在文档里写清边界。
4. **不要做的事**：不要在现有双存储模型上继续叠加特性（新规则类型、新存储模式、新授权入口）；每加一层都会放大 C3/D1 的歧义。先收敛，再扩展。
