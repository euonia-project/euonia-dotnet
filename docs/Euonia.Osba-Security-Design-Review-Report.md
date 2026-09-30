# Euonia.Osba / Euonia.Security 设计问题与风险评估报告

> 审查范围：`Source/Euonia.Security`、`Source/Euonia.Osba` 全部源码及其测试项目。
> 基线：`Euonia.Security.Tests` 138 个用例、`Euonia.Osba.Tests` 233 个用例，全部通过（2026-09-29）。
> 本文只列**经源码求证**的问题；已评审为「刻意取舍」的设计单独列在 §4，不做改动。

---

## 1. 问题总览

| 编号 | 模块 | 位置 | 问题 | 严重度 | 风险类型 |
|---|---|---|---|---|---|
| H1 | Osba | `Core/EditableObject.cs` SaveAsync | 未接线 `BusinessContext` 时抛 NRE，与工厂边界的明确报错口径不一致 | 高 | 可诊断性 / fail-open 观感 |
| M1 | Osba | `Core/ObservableObject.cs` BusyChanged | 事件 add/remove 手写 `Delegate.Combine/Remove`，无同步保护 | 中 | 并发竞态（丢事件处理器） |
| M2 | Osba | `Core/BusinessObject.cs` Rules 属性 | 惰性初始化无锁，并发可产生两个 `Rules` 实例 | 中 | 并发竞态（丢违规状态） |
| M3 | Security | `Scope/ScopeGuard.cs` | 持有 `SemaphoreSlim` 但未实现 `IDisposable` | 中低 | 资源释放 |
| M4 | Security | `Scope/ScopeSubjectSetBuilder.cs` | 允许裸 `"*"` 权限码授权，等于全量放行 | 中 | 安全（fail-open 面） |
| L1 | Osba | `Factory/BusinessObjectFactory.cs` SaveAsync | 直调工厂 `SaveAsync` 不经过 `EditableObject.SaveAsync` 时验证被跳过 | 低 | 文档边界 |
| L2 | Security | `OperationCodeSource.cs` | 缓存键元组大小写敏感，与规则表 OrdinalIgnoreCase 口径不一致 | 低 | 一致性（仅冗余缓存） |

---

## 2. 问题详情与修复方案

### H1 `EditableObject<T>.SaveAsync` 在对象未接线时抛 NullReferenceException

**位置**：`Source/Euonia.Osba/Core/EditableObject.cs`（`SaveAsync(bool, object, CancellationToken)` 内 `BusinessContext.GetRequiredService<IObjectFactory>()`）。

**现象**：`new` 出对象后未设置 `BusinessContext`（未走工厂创建）即调用 `SaveAsync()` 时，先跑完规则、`MarkAsBusy()`，然后在取工厂处抛出裸 `NullReferenceException`。

**为何是问题**：
- 工厂边界的 `ObjectAuthorization` / `ScopeAuthorization` 对同一场景给出的是明确的 `InvalidOperationException`（`IDS_OBJECT_CONTEXT_MISSING`，含类型名与操作名）；而最常用的 `SaveAsync` 公开路径却给出 NRE，排障方向完全不同。
- 抛出点在 `MarkAsBusy()` **之后**——`finally` 会 `MarkAsIdle()` 归还计数，行为尚正确，但规则已经跑完一遍，浪费且时序混乱。

**修复方案**：在 `SaveAsync` 进入持久化调用前加：

```csharp
Check.Ensure(BusinessContext != null, Resources.IDS_OBJECT_CONTEXT_MISSING, GetType().Name, BusinessOperation.Update);
```

（与 `ScopeAuthorization.Ensure` 同一资源串口径；操作按当前状态经 `ScopeOperationMap.TryResolve` 取，取不到时统一写 "save"。）

**验证**：新增测试——未接线对象 `SaveAsync` 应抛带类型名的 `InvalidOperationException` 而非 NRE。

### M1 `ObservableObject<T>.BusyChanged` 事件订阅竞态

**位置**：`Source/Euonia.Osba/Core/ObservableObject.cs`，`BusyChanged` 事件的 add/remove。

**现象**：

```csharp
add => _busyChanged = (BusyChangedEventHandler)Delegate.Combine(_busyChanged, value);
remove => _busyChanged = (BusyChangedEventHandler)Delegate.Remove(_busyChanged, value);
```

读侧 `_busyChanged?.Invoke` 无任何同步。并发订阅/退订与触发并存时，`Combine` 的非原子读-改-写会**静默丢失后订阅的处理器**。同类型的 `Saved`、`ValidationComplete` 都走 `Events`（`WeakEventManager`），唯此一处手写。

**修复方案**：与既有事件对齐——改走 `Events.AddEventHandler / RemoveEventHandler / HandleEvent`，删除手写字段。副作用是订阅者变为**弱引用**（与 `Saved` 一致）；长生命周期的 VM 订阅不受影响。

**验证**：并发 `+=` / `-=` / 触发的压力测试；以及与 `Saved` 一致的弱引用语义断言（订阅者可被回收）。

### M2 `BusinessObject.Rules` 惰性初始化竞态

**位置**：`Source/Euonia.Osba/Core/BusinessObject.cs`，`Rules` 属性 getter。

**现象**：

```csharp
if (field == null) { field = new Rules(this); }
else if (field.Target == null) { field.SetTarget(this); }
```

无锁。两个线程首次并发访问（例如属性 setter 触发规则检查与工厂边界校验并发）会各自创建 `Rules` 实例，后写者胜出，先写者上累积的 `BrokenRules` / `RunningRules` 被整体丢弃——表现为偶发「违规列表为空但对象实际不合法」或 `AllRulesComplete` 重复触发。

**修复方案**：双检查 + 实例锁（锁对象复用现成的 `_changedPropertiesLock` 之外的私有 `Lock`，或 `Interlocked.Exchange` 一次性发布）。保留 `Target == null` 的重绑语义。

**验证**：并发首访压力测试，断言全程只有一个 `Rules` 实例（可通过实例引用计数或 `Rules.Target` 一致性断言）。

### M3 `ScopeGuard` 未释放 `SemaphoreSlim`

**位置**：`Source/Euonia.Security/Scope/ScopeGuard.cs`，字段 `_resolveGate`。

**现象**：`ScopeGuard` 以 Scoped 生命周期注册（`TryAddScoped<IScopeGuard>`），持有 `new SemaphoreSlim(1, 1)` 却未实现 `IDisposable`。`SemaphoreSlim` 在等待数超过许可时惰性分配内核 `WaitHandle`，不释放则依赖 GC 兜底。DI 容器对 Scoped 解析出的 `IDisposable` 会随作用域释放，实现接口即可零成本修复。

**修复方案**：`ScopeGuard : IScopeGuard, IDisposable`，`Dispose` 中 `Dispose` 掉 `_resolveGate`（容器负责调用；不设终结器）。

**验证**：现有测试回归 + 一个作用域释放冒烟测试（不抛 `ObjectDisposedException` 即可，后续访问本就发生在作用域内）。

### M4 `ScopeSubjectSetBuilder` 允许裸 `"*"` 权限码 —— 全量授权面

**位置**：`Source/Euonia.Security/Scope/ScopeSubjectSet.cs`（`HoldsPermission` 的通配匹配）与 `ScopeSubjectSetBuilder`。

**现象**：通配规则是「以 `*` 结尾的前缀通配」（`repo:*` 匹配 `repo:push`，合理且有文档）。但**裸 `"*"`**（前缀为空串）会匹配**任何**权限码：解析器实现里手滑写一个 `AddCode("*")`（或 `AddGrant("*", …)`），类型级闸门对所有 `[Permission]` 一律放行。这违反本库「fail-closed、错误要响亮」的核心原则——它是一条没有任何注册期/启动期信号的全量授权通道。

**修复方案**：在 `ScopeSubjectSetBuilder.AddCode / AddGrant` 入口拒绝裸 `"*"`（抛 `InvalidOperationException`，提示：如需超级权限请枚举具体码或引入专用超级码由策略显式表达）。保留 `repo:*` 形态的前缀通配不变。`ScopeKeys`/`ScopePolicySet` 的保留前缀校验模式可直接沿用。

**验证**：新增测试——`AddCode("*")` 抛异常且消息含修法；`AddCode("repo:*")` 仍然合法；`HoldsPermission` 对 `repo:*` 的既有语义回归不变。

### L1 直调 `IObjectFactory.SaveAsync` 跳过验证线

**位置**：`Source/Euonia.Osba/Factory/BusinessObjectFactory.cs` `SaveAsync<TTarget>`。

**现象**：规则裁决位于 `EditableObject<T>.SaveAsync`（保护方法）；工厂 `SaveAsync` 只做权限（`ObjectAuthorization`）与数据范围（`ScopeAuthorization`）。调用方绕过 `EditableObject.SaveAsync` 直调工厂时，**验证被静默跳过**（权限仍生效）。

**方案选项**（二选一，请确认）：
1. **仅文档化**：在 `IObjectFactory.SaveAsync` 接口注释与 PERMISSION.md §4 标注「验证线在 `EditableObject.SaveAsync`，直调工厂只过权限线」。零行为风险。
2. **工厂补一道裁决**：`factory.SaveAsync` 内先 `ObjectRuleGuard.EnsureRulesAsync(target, "Object not valid for save.", ct)`。语义最稳，但常规路径（EditableObject → 工厂）会跑两遍规则（幂等但异步 I/O 型规则会执行两次）。

**建议**：取方案 1（文档化）。若接受方案 2，我会同步在测试中覆盖双路径。

### L2 `OperationCodeSource` 缓存键大小写不一致

**位置**：`Source/Euonia.Security/OperationCodeSource.cs`，`_cache` 的元组键。

**现象**：规则表按 `OrdinalIgnoreCase` 匹配操作名，但 `ConcurrentDictionary<(Type, string), …>` 用默认比较器——`"Read"` 与 `"read"` 各存一份缓存。**无正确性问题**（两份结果一致），仅冗余。

**修复方案**：构造 `ConcurrentDictionary` 时传入元组比较器（`Type` 相等 + `string.OrdinalIgnoreCase`），一处小改。

---

## 3. 修复实施计划（确认后执行）

1. `Source/Euonia.Osba/Core/EditableObject.cs` —— H1 守卫
2. `Source/Euonia.Osba/Core/ObservableObject.cs` —— M1 事件走 `Events`
3. `Source/Euonia.Osba/Core/BusinessObject.cs` —— M2 `Rules` 加锁
4. `Source/Euonia.Security/Scope/ScopeGuard.cs` —— M3 `IDisposable`
5. `Source/Euonia.Security/Scope/ScopeSubjectSetBuilder.cs` —— M4 拒绝裸 `"*"`
6. `Source/Euonia.Security/OperationCodeSource.cs` —— L2 缓存比较器
7. L1 按确认结果文档化或补裁决
8. 以上每项配套回归测试（新增用例放入对应 `Tests` 项目），全部跑通 `Euonia.Security.Tests` + `Euonia.Osba.Tests`

预计不改变任何既有公共 API 的签名；M1 的弱引用语义变化会在 XML 注释中显式标注。

---

## 4. 评审过但不做改动的取舍（防止误伤既有设计）

| 设计 | 结论 |
|---|---|
| `ScopeGuard.GetSubjects()` 同步路径用 `AsyncContext.Run`（sync-over-async） | 刻意取舍，比裸 `GetAwaiter().GetResult()` 安全；异步入口 `IsGrantedAsync` 已覆盖热路径 |
| `BypassRuleChecksObject` 的进程级静态锁 | 改为每对象双锁会引入「manager 锁 → target 锁」的反向锁序，有真实死锁风险；锁只在 `using` 进出处获取，争用面小，**不改** |
| `Rules.CheckRules` 在有 SyncContext 时 `Task.Run + GetAwaiter().GetResult()` | 为规避死锁的刻意设计（注释已说明），不改 |
| `RunRules` 级联分支持 `_lockObject` 递归进锁 | Monitor 可重入，无死锁；子任务完成回调在锁外发送通知，注释已说明，不改 |
| 工厂 `Create` 不做行级数据权限判定 | 文档化的设计（只构造不落库，判定会误杀），不改 |
| 写侧后置检查不阻止「越权写入」 | 文档化的已知边界（需持久化层拦截器），不改 |
| 规则按类型进程级共享（`RuleManager`） | 刻意设计，实例级规则已有 `AddInstanceRule` 通道，不改 |
| 权限码与策略键 `OrdinalIgnoreCase`、维度值大小写敏感 | 快照/闸门口径一致的回归护栏已有测试，不改 |
| `HasPermission`/`CanAccessRow` 等查询语义（无从判定返回 true） | 拦截只在工厂边界的两线分离设计，不改 |
| 配置驱动规则不订阅变更 | 文档化（注册期读一次），不改 |
