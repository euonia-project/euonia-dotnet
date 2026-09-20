# Euonia.Bus Inbox / Outbox 实现报告

## 概述
本次为 `euonia-net` 的 Euonia.Bus 实现「Inbox/Outbox」可靠消息模式，对齐 Java 版设计（`euonia-java/bus-abstract/.../consistency` + `bus-core` 集成），并**通过既有 Pipeline**（`IPipeline<TRequest,TResponse>` + 行为）接线，不引入额外宿主：

- **抽象契约**（`Euonia.Bus.Abstract/Consistency`）：`IOutboxStore`、`IInboxStore`、条目/传输/处理程序状态记录；
- **选项模型**：全局开关 + 单条覆盖（发送侧按消息覆盖，接收侧仅全局）；
- **参考实现**（内存）：`InMemoryOutboxStore` / `InMemoryInboxStore`；
- **接线**：`MessageBus.PublishAsync` 走 Outbox，`DefaultHandlerContext.HandleAsync` 走 Inbox；
- **后台重试**：`MessageBus` / `DefaultHandlerContext` 各自内嵌 Timer 驱动的派发器（`OutboxDispatcher` / `InboxDispatcher`），失败条目自动重投，超限进入死信日志。

验证：`Euonia.slnx` 0 错误 0 警告；`Euonia.Bus.Tests` **56/56**、`Euonia.Bus.InMemory.Tests` **10/10** 通过。

---

## 一、抽象契约（`Source/Euonia.Bus.Abstract/Consistency/`）

### `OutboxTransport.cs`
`OutboxTransportStatus` 枚举（`Pending/Failed/Success`）+ `OutboxTransport` 可变状态记录：`Transport`、`Status`、`RetryAttempts`、`MarkAsSuccess()` / `MarkAsFailed(string)`（失败时 `RetryAttempts++`、记录 `LastError`）。

### `OutboxEntry.cs`
`MessageId`、`CreateTime`、`Content`（`IMessageEnvelope`）、`Transports`（`IReadOnlyList<OutboxTransport>`）；`Status` = 全部 Success；`MarkSuccess(string)` / `MarkFailed(string, bool)`（`markRetry=true` 时计入重试次数，供“插入成功但投递失败”计数）；`GetTransport(string)` 大小写不敏感查找。

### `IOutboxStore.cs`
```
Task<bool> Insert(IMessageEnvelope envelope, IReadOnlyList<string> transports);   // 默认：GetAndCache(envelope) + AddTransport(...)
OutboxEntry Get(string messageId);
Task<bool> MarkAsSuccess(string messageId, string transport);
Task<bool> MarkAsFailed(string messageId, string transport, string message);      // 默认：markRetry=false
IReadOnlyList<OutboxEntry> GetFailedMessages();
Task FlushAsync();                                                                 // 默认空实现（持久化实现落盘点）
static ConcurrentDictionary<string, OutboxEntry> Cache { get; }                   // 读缓存
static OutboxEntry GetAndCache(IMessageEnvelope envelope);
```
> 契约要点：`Insert` 语义 = 索引日志，**不保证传输**；幂等由调用方以 `MessageId`（`Envelope.MessageId`）先查询后插入保证。`Cache` 静态缓存为性能考虑，与 Java 版对齐；默认高并发下以 entry 为粒度无竞争（调用方工厂携带 entry）。

### `InboxHandler.cs` + `InboxEntry.cs` + `IInboxStore.cs`
与 Outbox 对称：`InboxHandlerStatus`（`Pending/Failed/Success`）、`InboxHandler`（`MarkAsSuccess()` / `MarkAsFailed(string)`）、`InboxEntry`（`MessageId`、`Channel`、`Content`、`Handlers`）、`IInboxStore`：
```
Task<bool> Insert(string channel, IMessageEnvelope envelope, IReadOnlyList<string> handlers);
InboxEntry Get(string messageId);
Task<bool> MarkAsSuccess(string messageId, string handler);
Task<bool> MarkAsFailed(string messageId, string handler, string message);
IReadOnlyList<InboxEntry> GetFailedMessages();
Task FlushAsync();
```

---

## 二、选项模型

### `Source/Euonia.Bus/Reliability/OutboxOptions.cs` / `InboxOptions.cs`
- `Enabled`（全局开关，默认 **false**）；
- `PollingInterval`（后台重试轮询间隔，默认 `TimeSpan.FromSeconds(60)`，`PollingInterval < TimeSpan.Zero` 抛错）；
- `MaxRetryAttempts`（默认 3；`<= 0` 表示不限重试）。

### `Source/Euonia.Bus/Dispatching/ExtendableOptions.cs`
新增 `bool? UseOutbox` / `bool? UseInbox`（单条覆盖；`null` = 用全局开关）。`UseInbox` 目前仅作发送侧扩展点——**接收侧由全局 `InboxOptions.Enabled` + 存储注册决定**，内置传输不消费该属性。

### `MessageBusOptions` 配置节点
`Euonia:Bus:Outbox:Enabled/PollingInterval/MaxRetryAttempts` 与 `Euonia:Bus:Inbox:...` 标准绑定。

---

## 三、消息管线接线

### Outbox（发送侧）—— `Source/Euonia.Bus/Dispatching/MessageBus.cs`
```csharp
var useOutbox = options.UseOutbox ?? (_outboxOptions.Enabled && _outboxStore != null);
if (useOutbox)
{
    if (_outboxStore == null) throw new MessagePersistentException("Outbox store is not registered.");
    var envelope = new RoutedMessage<TMessage>(_payload[, correlationId, ...]) { MessageId = options.MessageId };
    if (!await _outboxStore.Insert(envelope, transports.ToArray()))
        throw new MessagePersistentException("Message already exists.");
}
transports = _dispatcher.Determine(options.Channel, payload.GetType(), true);
await RunWithPipelineAsync(options.Channel, _requestAccessor.Context, payload, transports, useOutbox: useOutbox, cancellationToken);
```
- 先 **插入 Outbox Entry**（此时即持锁、落库语义，`MessageId` 缺失自动生成），之后任何传输失败都由后台派发器补偿；
- `RunWithPipelineAsync(..., useOutbox)` 在 `OutgoingLoggingBehavior` 之后追加
  `pipeline.Use(typeof(OutgoingOutboxBehavior<TMessage, Unit>), transport)`，逐传输独立标记成败。

### `Source/Euonia.Bus/Behaviors/OutgoingOutboxBehavior.cs`（internal）
```
HandleAsync(context, next):
  try      { await next(context);  await _store.MarkAsSuccess(id, transport); }
  catch (Exception e) { await _store.MarkAsFailed(id, transport, e.Message); throw; }
```
即：**消息既已入库，投递失败不再上抛给发布者（由后台重试），仅标记失败**。

### Inbox（接收侧）—— `Source/Euonia.Bus/Handling/DefaultHandlerContext.cs`
- `_handlerContainer` 升级为 `ConcurrentDictionary<string, List<HandlerRegistration>>`；三个 `Register` 重载统一存 `new HandlerRegistration(handlerType.FullName, factory)`，处理器身份 = 处理器类型 `FullName`；
- `HandleAsync(channel, message, context, cancellationToken)` 按 `IsUnicast/IsMulticast` 分发：
  - **单播**（`useInbox = _inboxOptions.Enabled && _inboxStore != null`）：直接执行选定处理器，成功 `MarkAsSuccess`、失败 `MarkAsFailed` 后**重抛**（命令语义，调用方需感知失败）；
  - **多播**：`_inboxStore.Insert(channel, envelope, handlerNames)` 去重——重复 `MessageId` 直接跳过（幂等）；否则对每个处理器**并行**执行，逐处理器独立 `MarkAsSuccess/MarkAsFailed`，单个失败不阻断其他处理器。
- 并发安全：`ConcurrentBag<(Entry, Handler)>` 收集条目 + `Task.WhenAll`；`while (attempt < 50) try { } catch (SynchronizationLockException) { Cancel(registered); attempt++; }` 兼容高争用下锁语义；
- `MessageId` 缺失时从 `MessageContext` 解出；无上下文则生成（无去重语义）。

---

## 四、后台重试派发器

### `Source/Euonia.Bus/Reliability/OutboxDispatcher.cs`（internal，`MessageBus` 内嵌）
- 构造注入 `IServiceAccessor`、`IOutboxStore`、`OutboxOptions`；
- `Start()`：**只要有存储即启动**定时器（默认轮询间隔），互斥 `Interlocked.CompareExchange` 防重入（正在轮询直接跳过），`Timer.Dispose()` 支持 `IDisposable`；
- `RetryAllAsync()`：`GetFailedMessages()` 中取 `CanRetry = MaxRetryAttempts <= 0 || item.RetryAttempts <= MaxRetryAttempts` 的条目，逐条 `RedeliverAsync(item.Content, env.Channel, env.MessageId)`；
- `RedeliverAsync`：`Envelope -> (Channel, MessageId)` 提取后经**反射实例化还原泛型**，以与生产路径完全相同的 `RedeliverCoreAsync<TMessage>` 走 `_accessor.GetService<IPipeline<TMessage, Unit>>()`，与普通发布共享行为链（含 Outbox 行为自动标记）；
- 超限（`!CanRetry`）不重投，转死信日志（结构化日志）。

### `Source/Euonia.Bus/Reliability/InboxDispatcher.cs`（internal，`DefaultHandlerContext` 内嵌）
- 构造 `(IServiceProvider, IInboxStore, InboxOptions, 共享 handlerContainer)`；`Start()`：`_store != null && _options.Enabled` 才启动；
- `RetryAllAsync()`：失败条目按 `InboxHandler` 名称反查容器，执行对应处理器，成功 `MarkAsSuccess` / 失败续标记；异常吞掉并记日志（重试场景不打断轮询）。

### `MessageBus` / `DefaultHandlerContext` 生命周期
两者实现 `IDisposable`：发布侧 `_outboxDispatcher`、接收侧 `_inboxDispatcher` 均由所属单例在 `Dispose` 时释放 Timer。

---

## 五、服务注册（`ServiceCollectionExtensions.AddEuoniaBus`）
**不默认注册任何存储实现**——`AddEuoniaBus` 只接线核心服务（配置器、处理器上下文、总线、分发器、管道、接收器激活），`IOutboxStore` / `IInboxStore` 需由应用显式注册：

```csharp
// 使用内置内存参考实现
services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
services.AddSingleton<IInboxStore, InMemoryInboxStore>();
// 或注册自定义持久化实现（SQL/EF/...）
services.AddSingleton<IOutboxStore, SqlOutboxStore>();
services.AddSingleton<IInboxStore, SqlInboxStore>();
```

未注册存储时：Outbox/Inbox 无论全局开关如何**均不激活**（`MessageBus` / `DefaultHandlerContext` 解析到 null → 走非收件箱逻辑）；若仅单条强制 `UseOutbox=true` 而无存储，则 `PublishAsync` 抛 `MessagePersistentException`。`MemoryOutboxStore` / `InMemoryInboxStore` 作为参考实现保留在 `Euonia.Bus/Reliability`，供开发环境直接注册使用。`Source/Euonia.Bus/Properties/AssemblyInfo.cs` 增 `InternalsVisibleTo("Euonia.Bus.Tests")` 以便测试访问 internal 派发器/行为。

---

## 六、删除的桩代码
`Source/Euonia.Bus.Abstract/Persistence/IMessageStore.cs` 与 `Source/Euonia.Bus.Abstract/Tracking/IMessageTracking.cs`（无引用、未使用），按既定决策删除。

---

## 七、测试（`Tests/Euonia.Bus.Tests/Consistency/`）

### `TestMessages.cs`
`OrderPlacedEvent : IMulticast` + `Envelope<T>(payload, channel, messageId)` 帮助器（返回带 `MessageId` 的 `RoutedMessage<T>`）。

### `OutboxStoreTests.cs` / `InboxStoreTests.cs`（各 7 例）
插入默认接口行为、`GetAndCache`、`GetTransport/GetHandler` 不敏感查找、`MarkAsSuccess/Failed` 状态机与 `RetryAttempts`、`GetFailedMessages` 过滤、重复插入返回 false。

### `OutboxBehaviorTests.cs`（2 例）
直接调用 `OutgoingOutboxBehavior` + `PipelineDelegate`：投递成功 → 标记 Success；投递抛异常 → 标记 Failed 并上抛。

### `InboxOutboxIntegrationTests.cs`（6 例 + `RecordingTransporter`）
- 全局关闭默认不入库；全局开启入库并在传输成功后标记 Success（断言 `Delivered`/`MessageId`）；
- **单条覆盖**：全局关闭 + `UseOutbox=true` 仍入库；
- 去重：多播重复 `HandleAsync`（同 MessageId）只执行一次；不同 MessageId 各执行；
- DI 装配：`ServiceAccessor`/`IServiceAccessor`、`DefaultRequestContextAccessor` + `DelegateRequestContextAccessor`、`TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>`、`AddLogging/AddOptions/Configure<MessageBusOptions>`、`AddEuoniaBus`、keyed `ITransporter("test")`。

### `InboxOutboxRetryTests.cs`（3 例）
- `OutboxDispatcher.RetryAllAsync` 对失败条目经完整管线重投并标记 Success（`RetryAttempts`、失败集清空）——**行为行为与生产一致**：行为从 DI 解析同一存储单例；
- `InboxDispatcher.RetryAllAsync` 按处理器名反查容器重执行并标记 Success；
- `OutboxDispatcher.RetryAllAsync` 尊重 `MaxRetryAttempts`（超限不重投、状态保持 Failed）。

---

## 八、验证结果

| 项目 | 结果 |
| --- | --- |
| `dotnet build Euonia.slnx` | 0 错误，0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误，0 警告 |
| `Euonia.Bus.Tests` | 56/56 通过（含 16 例 Outbox、15 例 Inbox、25 例存储/行为/集成/注册/快速失败） |
| `Euonia.Bus.InMemory.Tests` | 10/10 通过（真实 InMemory 传输端到端回归） |

**环境说明**：本机 `dotnet test <csproj>` 经 MTP 适配器偶发报“Zero tests ran / exit 5”（与代码无关），可靠验证路径为直接运行 `./Tests/<Proj>/bin/Debug/net10.0/<Proj>`。

## 九、优化与查缺补漏（第二轮）

本轮针对实现中的正确性风险与便利性缺口进行加固，全部随上述验证通过。

### 1. 修复「在途消息被重复投递/重复执行」竞态（P0 正确性缺陷）
**问题**：`InMemoryOutboxStore` / `InMemoryInboxStore` 的 `GetFailedMessages()` 原实现过滤条件为 `Status != Success`，会把 `Pending`（已入库、仍在初次投递/执行中）的记录一并返回；而两个后台调度器对 `GetFailedMessages()` 的条目**立即重投**。若轮询定时器恰好在「插入条目 → 传输完成并标记」之间触发，就会对仍在途的消息重复投递（Outbox）或重复执行破坏去重（Inbox）。

**修复**：`GetFailedMessages()` 过滤收紧为 `Status == Failed`，仅返回真正失败、等待调度的记录；同步修正 `IOutboxStore` / `IInboxStore` 接口文档（由「待发送或发送失败」改为「投递/执行失败且等待重试」）。内存实现进程内重启即清空，`Pending` 天然不可能跨重启残留，因此不会产生“卡死 Pending 永不重试”问题。
> 若引入持久化存储，需自行处理“崩溃时处于 Pending 的条目”：可在 `GetFailedMessages()` 中按 `CreatedAt` 阈值把超时 Pending 视为失败返回。

### 2. 配置缺失快速失败（避免静默退化）
**问题**：全局开关启用但存储未注册时，原逻辑静默回退到无 Outbox/Inbox 路径——启用可靠性却悄悄失效。

**修复**：
- `MessageBus.PublishAsync`：`useOutbox = options.UseOutbox ?? _outboxOptions.Enabled`；若为真但 `_outboxStore == null` → `MessagePersistentException`（含注册指引，如 `services.AddInMemoryOutbox()`）。
- `DefaultHandlerContext.HandleAsync`：`useInbox = _inboxOptions.Enabled`，启用但无存储时同样抛 `MessagePersistentException`。单播/多播路径统一到入口处校验，不再有 `_inboxStore.Insert` 空引用风险。

### 3. 调度器并行化与微优化
- **并行重投**：`OutboxDispatcher` / `InboxDispatcher` 的 `RetryAllAsync` 从串行逐个变为收集任务后 `Task.WhenAll` 并行执行（对齐 Java 版 `CompletableFuture.allOf`），失败记录较多时缩短恢复窗口；逐条目异常仍各自吞掉并记日志，不整体中断轮询。
- **反射缓存**：`RedeliverAsync` 的 `MakeGenericMethod` 由每次调用计算改为按 `Type` 缓存的 `ConcurrentDictionary<Type, MethodInfo>`。
- **空安全**：`finally` 中 `_store?.ClearCache()`（防御空存储）。
- 空失败集提前 `return`，避免无谓创建任务列表。

### 4. 便利注册扩展
默认不注册存储（见第五节）的基础上，新增一行式显式注册扩展（`TryAddSingleton`，可被自定义实现覆盖）：
```csharp
services.AddInMemoryOutbox();   // TryAddSingleton<IOutboxStore, InMemoryOutboxStore>
services.AddInMemoryInbox();    // TryAddSingleton<IInboxStore, InMemoryInboxStore>
```

### 5. 新增测试（本轮 +9）
- `GetFailedMessages_ExcludesPendingInFlight*`（Outbox/Inbox 存储）：Pending 不入待重试列表。
- `OutboxDispatcher_RetryAllAsync_DoesNotRedeliverPendingInFlightMessages`、`InboxDispatcher_RetryAllAsync_DoesNotReexecutePendingInFlightHandlers`：调度器对在途记录零动作（防竞态回归）。
- `PublishAsync_WithOutboxEnabled_WithoutStore_ThrowsMessagePersistentException`、`HandleAsync_WithInboxEnabled_WithoutStore_ThrowsMessagePersistentException`：配置缺失快速失败。
- `StoreRegistrationTests`：`AddInMemoryOutbox` / `AddInMemoryInbox` 注册成功，且 `TryAddSingleton` 不覆盖已注册自定义实现。

## 遗留 / 说明
- **内存实现为参考实现**：不自动注册、不保证重启持久化；需显式 `AddSingleton<IOutboxStore, InMemoryOutboxStore>()`（及 Inbox）启用开发环境，生产建议以 `IOutboxStore`/`IInboxStore` 实现为 SQL/EF 存储（契约已含 `FlushAsync` 持久化点）。
- **Outbox 重试上抛语义**：`OutgoingOutboxBehavior` 仅标记不重抛（失败交给后台重试），保持发布者快速失败语义与 Java 版一致。
- **单播 Inbox 失败重抛**：命令语义下调用方须感知失败；若需纯异步可靠投递建议走多播 + 订阅模式。
- 死信阶段（超过 `MaxRetryAttempts`）当前仅日志记录，未实现死信队列存储。