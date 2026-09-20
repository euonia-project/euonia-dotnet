# Euonia.Bus 修复报告

## 概述
本次针对 `euonia-net` 仓库中 Euonia.Bus 及关联项目（Bus.Abstract、Bus.RabbitMq、Bus.ActiveMq、Bus.InMemory）展开修复，分为五类：
- **构建阻断修复**（1 处）；
- **语义/契约修复**（2 处）；
- **缓存键修复**（3 处，同根问题）；
- **处理器调用与消息分发修复**（2 处）；
- **InMemory / RabbitMq / ActiveMq 传输层修复**（7 处）；
- **回归测试补全**。

所有修改均通过构建与测试验证：`Euonia.Build.slnx` 与 `Euonia.Test.slnx` 各 0 错误 0 警告；Bus.Tests **22/22**、InMemory.Tests **10/10** 通过。

---

## 一、构建阻断类

### 1. `Source/Euonia.Core/Extensions/Extensions.Enum.cs`
**具体问题**：`GetDisplayName(this Enum)` 原实现调用 `@enum.GetAttribute<DisplayAttribute>()?.GetName()`，其中 `GetName()` 解析自 `Nerosoft.Euonia.Reflection.PriorityValueFinder`，而该类整体被 `#if NET8_0_OR_GREATER` 条件编译包裹。项目目标框架含 `netstandard2.1`，该 TFM 下 `PriorityValueFinder` 不存在 → CS0103 编译错误，**阻断整个解决方案构建**。另有 `DisplayAttributeManager` 等死代码引用同类问题。

**修改依据**：属性解析不经反射优先级查找也可满足需求，且消除死代码。重写为显式 if/else 优先级链，完全内聚在本文件：

```csharp
var name = @enum.GetAttribute<DisplayAttribute>()?.GetName();    // 1. DisplayAttribute
if (!string.IsNullOrWhiteSpace(name)) return name;
name = @enum.GetAttribute<DisplayNameAttribute>()?.DisplayName;   // 2. DisplayNameAttribute
if (!string.IsNullOrWhiteSpace(name)) return name;
name = @enum.GetAttribute<DescriptionAttribute>()?.Description;  // 3. DescriptionAttribute
return string.IsNullOrWhiteSpace(name) ? @enum.ToString() : name; // 4. 回退
```

优先级与原设计一致（Display > DisplayName > Description > ToString），行为不回归。**注意**：`DisplayAttribute.GetName()` 在 .NET Framework 中要求先调用 `GetDescription()` 才会缓存值——本项目 TFM 为 netstandard2.1/net8+，可直接调用；如需兼容旧框架可换 `GetDescription()`，此处按现状保留。

## 二、语义/契约类

### 2. `Source/Euonia.Bus.Abstract/MessageMetadata.cs`
**具体问题**（三个方法违反 `IDictionary<string, object>` 契约）：
- `Add(string, object)` 原为 `_dictionary.TryAdd(key, value)` —— 重复 key **静默忽略**，不抛异常，违反 `ICollection.Add` 契约（须抛 `ArgumentException`）。
- `Remove(KeyValuePair<K,V>)` 原为 `_dictionary.Remove(item.Key)` —— 只删 key，忽略 value。若传入 value 不匹配的 item 也会删除，违反 `KeyValuePair` 精确匹配语义。
- `CopyTo(KeyValuePair[], int)` 原为 `_dictionary.ToArray().CopyTo(items, index)` —— 先整体分配临时数组，浪费且与语义无关。

**修改依据**：
- `Add` → `_dictionary.Add(key, value)`：交给字典原生 `Add` 抛“重复键”异常，与所有 `IDictionary` 实现一致。
- `Remove(KeyValuePair)` → `((ICollection<KeyValuePair<string, object>>)_dictionary).Remove(item)`：委托显式接口实现完成键值双匹配，精确语义。
- `CopyTo` → 直接转型后委托 `CopyTo`：编译期不做拷贝/长度检查以外的动作，去掉中间数组分配。

索引器 getter 保持宽松（缺 key 返回 null）是刻意的：`RoutedMessage.GetTypeName()` 依赖 `Metadata[MessageTypeKey]` 在无 Payload 时读出 null，收紧会破坏该路径。

### 3. `Source/Euonia.Bus/RoutedMessage.cs`
**具体问题**：`ToString()` 原为 `$"{{MessageId}}:{{GetTypeName()}}"`，是**字面量**“`{MessageId}:{GetTypeName()}`”，从不展开实际值与类型名。

**修改依据**：`ToString` 用于日志/跟踪/描述等场景，应展示真实 ID 与消息类型。改为 `$"{MessageId}:{GetTypeName()}"`。

## 三、缓存键修复类（三处同根问题）

**共同问题**：约定（`IMessageConvention`）、策略（`ITransportStrategy`）、分发器都以 `channel` 为唯一缓存键。但 `IsUnicast/IsMulticast/IsRequest(channel, type)` 与 `Outgoing/Incoming(channel, type)` 的判定**同时依赖 channel 和消息类型**（`DefaultMessageConvention` 可按 channel 查命名前缀后缀、`DefaultTransportStrategy` 可按类型规则过滤）。结果：同一通道上第一个消息类型的结果被缓存并错误复用到其他类型 → 例如单播消息的判定被复用到请求消息，导致 `Incoming=false` 而跳过注册、选错传输器等难查 bug。

**修改依据**：缓存键升级为 `(string Channel, Type Type)` 二元组，值工厂用 `Item1/Item2`/`key.Item1` 解构，避免 `System.ValueTuple` 字段名（`Channel/Type`）在字典 lambda 中的编译歧义。

- **`Source/Euonia.Bus/Conventions/BaseMessageConvention.cs`**：`ConventionCache._cache`：`ConcurrentDictionary<string, bool>` → `ConcurrentDictionary<(string Channel, Type Type), bool>`；`Apply(channel, type, (handle, _) => ...)` 与 `_cache.GetOrAdd((channel, type), key => convention(key.Item1, key.Item2))`。三处调用（Unicast/Multicast/Request）同步改造。
- **`Source/Euonia.Bus/Strategy/BaseTransportStrategy.cs`**：`StrategyCache._cache` 同款升级；`Outgoing/Incoming` 的 lambda 签名调整为 `(key, t)`。语义注释同步更新。
- **`Source/Euonia.Bus/Dispatching/StrategicDispatcher.cs`**：`_transportCache`：`ConcurrentDictionary<string, IReadOnlyList<string>>` → 键含 `Type`；`GetOrAdd((channel, type), _ => ...)` 内用 `_.Item1/_.Item2`。

## 四、处理器调用类

### 4. `Source/Euonia.Core/System/MethodInvokerBuilder.cs`
**具体问题**：`BuildCallExpression(object target, ...)` 以 `Expression.Constant(target)` 为实例——只适合已知目标实例；`BuildHandlerInvoker` 需要「实例也是参数、编译一次、每次调用换实例」的模式。原 `DefaultHandlerContext` 依赖源码生成的私有包装 lambda，无法统一编译。

**修改依据**：新增重载 `BuildCallExpression(Expression target, MethodInfo method, params Expression[] arguments)`。对**静态方法**走 `Expression.Call(method, arguments)`（不要求实例），实例方法走 `Expression.Call(Expression.Convert(target, method.DeclaringType), method, arguments)`，统一经 `WrapToTaskObject` 包装为 `Task<object>`。这样调用器只需编译一次，静态/实例/任意返回类型（void/Task/ValueTask/Task<T>/原生值）全覆盖。

### 5. `Source/Euonia.Bus/Handling/DefaultHandlerContext.cs`
**具体问题**（多个）：
- 三条注册路径各自生成 lambda / 源码字符串匹配，处理参数类型的逻辑分散且靠字符串；
- `GetGenericTypeDefinition()` 在**非泛型**类型上调用会抛 `InvalidOperationException`（`OnChannelRegistered` 与 `Register(ChannelHandler)` 两处均缺 `IsGenericType` 防护）→ 注册非 `IHandler<,>` 类型即崩；
- 参数分类只认固定顺序/固定 `IMessageContext` 类型，具体 `MessageContext` 形参无法匹配。

**修改依据**：
- 公共路径统一为 `BuildHandlerInvoker(MethodInfo)`：一次性编译成 `Func<object, object, IMessageContext, CancellationToken, Task<object>>` 委托缓存复用；参数超过 3 个视为不支持，`LogWarning` 并跳过注册（不抛异常，保持容错）。
- `Register(string, Type, object, MethodInfo)` 与 `Register(string, ChannelHandler)` 的 else 分支复用同一 invoker，消除两套实现。
- `GetArguments` 改为表达式分类：`CancellationToken` → 接收 token；`IMessageContext` **或 `context.Type.IsAssignableFrom(parameterType)`** → 接收 context（支持具体 `MessageContext` 派生形参）；其余 → 接收消息；类型不匹配时 `Expression.Convert`。三个参数**任意顺序**均可正确映射（此前按位置写死）。

## 五、InMemory 传输器

### 6. `Source/Euonia.Bus.InMemory/InMemoryRecipientRegistrar.cs`
**具体问题**（核心多播 bug）：
- 原实现 `GetRecipient<TRecipient>()` 用 `_provider.GetService<TRecipient>()` 每次新建接收者；
- 多播接收者注册进 `WeakReferenceMessenger`（**弱引用持有**），如果注册器生命周期短/没人强引用接收者，接收者会注册后**立刻被 GC** → `PublishAsync` 永远发不到订阅者。

**修改依据**：
- 改实例级 `ConcurrentDictionary<Type, object> _recipients`：`MultipleSubscriberInstance == false`（默认）时**按类型单例复用**；
- 新增 `ConcurrentQueue<object> _aliveRecipients`：所有已创建实例由注册器**强引用持有**，从根因上杜绝弱引用信使提前回收；
- `GetService<TRecipient>()` → `GetRequiredService<TRecipient>()`：服务缺失立即暴露而非静默 null；
- `CurrentCultureIgnoreCase` → `OrdinalIgnoreCase`：通道/传输名比较应按字节序（区域不敏感且稳定）。

### 7. `Source/Euonia.Bus.InMemory/ServiceCollectionExtensions.cs`
**具体问题**：`IRecipientRegistrar, InMemoryRecipientRegistrar` 原注册为 **Transient**，与上述实例级持有相冲突——每次解析都是新注册器、新 `_recipients`，跨 `RegisterAsync` 的复用和强持有失效。

**修改依据**：改 `AddSingleton`。核对依赖链：`IConfigurator`/`IServiceProvider`/`IOptions<InMemoryBusOptions>`/`ILoggerFactory` 均为单例兼容；InMemory 接收者（`InMemoryConsumer/Subscriber/Executor`）只依赖 `IHandlerContext`/`ILoggerFactory`（亦单例），因此无捕获瞬态依赖问题（无 captive dependency）。`TryAddTransient` 的接收者与服务本身保留。

### 8. `Source/Euonia.Bus.InMemory/InMemoryTransporter.cs`
**具体问题**：
- 三处 `TaskCompletionSource` 均未指定 `TaskCreationOptions.RunContinuationsAsynchronously` → 完成回调可能同步发生在发送/回调线程上，存在死锁与线程池饿死风险；
- `cancellationToken.Register(...)` 返回的 `CancellationTokenRegistration` 从不 `Dispose()`（事件泄漏），且 `SetCanceled` 非 `Try` 形式；
- 非泛型 `SendAsync` 无 try/finally 保证注册释放。

**修改依据**：每个 TCS 增加 `RunContinuationsAsynchronously`；注册统一存入 `CancellationTokenRegistration cancellationRegistration = default;` 并在 `finally` 中 `Dispose()`；`TrySetResult/TrySetException/TrySetCanceled(Token)` 防重复完成；泛型两个方法在 finally 中同时卸载事件处理器（`Responded/Failed/Completed`），避免回调持有 context 造成泄漏。`Dispose(bool)` 重置两个全局 messenger 保留（视为显式关停钩子，风险大于收益，不改）。

### 9. `Source/Euonia.Bus.InMemory/Messenger/StrongReferenceMessenger.cs`
**具体问题**：`UnsafeSend<TMessage,TToken>` 的 token 路径中，`i == 0`（无匹配接收者）时在租用数组后抛 `InvalidOperationException`，但 `ArrayPool<object>.Shared.Rent(...)` 的数组**未归还** → 池里积压引用，长期运行内存泄漏。

**修改依据**：把 `i == 0` 的抛出移到 lock 块外、`try/finally` 之前：先 `Array.Clear(rentedArray, 0, 0)` + `ArrayPool.Return` 归还数组，再抛 `MessageDeliverException`（语义与 Unit 路径的“无接收者”异常一致；非 token 路径 `!TryGetMapping` 的 `InvalidOperationException` 仍在租数组前抛出，无泄漏）。

## 六、RabbitMq / ActiveMq（最小安全修复）

> 原则：不依赖 broker 的改动才做；channel 复用、AutoAck 统一、回复队列生命周期管理、broker 侧 `CancellationTokenRegistration` 全面推开等深改不做（无 broker 集成验证环境，避免未验证的破坏性改动）。

### 10. `Source/Euonia.Bus.ActiveMq/ActiveMqTransporter.cs`
**具体问题**：
- `SendAsync`/`CallAsync` 创建临时回复队列并监听，但 `BuildRequestAsync(session, message)` **从未传入 `replyQueue`**，`request.NMSReplyTo` 恒为 null → broker 端回复发到默认目的地，本端**永远等不到回复，请求-响应永不完成**；
- 回复回调不做相关过滤：临时队列里任何迟到/无关消息都会 `SetResult` 提前完成；
- `message.User.Identity?.Name`：`User` 为 null 时 NRE（`?` 只在 `Identity` 前）；
- TCS 使用 `SetResult/SetException`，若取消已先行触发则抛 `InvalidOperationException`；
- `await task.Task` 后卸载 Listener，取消路径（抛 `TaskCanceledException`）时 Listener 泄漏；
- TCS 缺 `RunContinuationsAsynchronously`。

**修改依据**：
- `BuildRequestAsync(session, message, replyQueue)` 打通请求-响应链路；
- `OnReceived` 增加 `if (reply.NMSCorrelationID != message.CorrelationId) return;` 过滤；
- `TrySetResult/TrySetException` + `RunContinuationsAsynchronously`；
- `try { ... return await task.Task; } finally { replyConsumer.Listener -= OnReceived; }`；
- `message.User?.Identity?.Name`。

### 11. `Source/Euonia.Bus.ActiveMq/ActiveMqRecipientRegistrar.cs` 与 `Source/Euonia.Bus.RabbitMq/RabbitMqRecipientRegistrar.cs`
**具体问题**：`string.Equals(defaultTransporter, _options.Name, StringComparison.CurrentCultureIgnoreCase)`。
**修改依据**：`CurrentCultureIgnoreCase` 依赖运行环境区域，行为不稳定（土耳其问题等），改 `OrdinalIgnoreCase`，与其余代码库约定一致。

### 12. `Source/Euonia.Bus.RabbitMq/RabbitMqTransporter.cs`
**具体问题**：与 ActiveMq 相同的 TCS 问题——`new TaskCompletionSource<TResponse>()` 无 `RunContinuationsAsynchronously`、回调 `SetResult/SetException`（取消后抛）、`consumer.ReceivedAsync -=` 在 `await task.Task` 之后（取消/异常路径泄漏）。RabbitMq 侧**已具备**回复队列接线（`BuildProperties(message, responseQueueName)`）与 CorrelationId 过滤，故只做异步安全与事件清理。
**修改依据**：TCS 加 `RunContinuationsAsynchronously`；回调改 `TrySet*`；发送逻辑与 `return await task.Task` 包入 `try/finally`，`finally` 中卸载 `ReceivedAsync`。

## 七、测试

### 13. `Tests/Euonia.Bus.Tests/FixRegressionTests.cs`（新增）
回归覆盖本轮修复：`RoutedMessage.ToString()` 插值展开（断言 `MessageId:` 前缀 + 包含 `GetTypeName()` + 不出现字面量 `{GetTypeName()}`）、`MessageMetadata.Add` 重复键抛 `ArgumentException`、`MessageMetadata.Remove(KeyValuePair)` 键值精确匹配。
> 实现要点：`RoutedMessage<object>(42,...)` 的 Payload 装箱为 `int`，按其实际 `int` 类型名断言，比硬编码 object 名更符合 `GetTypeName()` 语义。

### 14. `Tests/Euonia.Bus.Tests.Shared/`（含 `Euonia.Bus.Tests.Shared.projitems`）
- `Events/UserCreatedEvent.cs` + `Handlers/UserEventListener.cs`（`[Subscribe("user.created")]`，静态 `ConcurrentBag<UserCreatedEvent> Received`）：构造用于多播回归的独立命令/处理器，同时避免污染其他单播测试。
- `ServiceBusTests.cs`：新增 `TestPublishMulticast_EventDeliveredToSubscribers`——发布到 `user.created`，延时后断言事件进入 `UserEventListener.Received`，验证「注册器强持有接收者 + 弱引用信使」链路。受 `_preventRunTests` 门控。
- 两个新增源文件已加入 `Euonia.Bus.Tests.Shared.projitems`，随 InMemory/RabbitMq 测试项目编译。
- 过程中误将 `TestSendCommand_NoResponse_ThrowExceptionInHandler_ErrorPropagatesToCaller`（`IHandler<T>` 异常传播回归）替换删除，已恢复，回归测试集完整性保持。

## 八、验证结果

| 项目 | 结果 |
| --- | --- |
| `dotnet build Euonia.Build.slnx` | 0 错误，0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误，0 警告 |
| `Euonia.Bus.Tests` | 22/22 通过 |
| `Euonia.Bus.InMemory.Tests` | 10/10 通过（含新增多播回归与异常传播回归） |

**环境说明**：本机 `dotnet test <csproj>` 经 MTP 适配器偶发报“Zero tests ran / exit 5”（与代码无关），可靠验证路径为 `dotnet exec <testdll>`。

## 遗留 / 未做深改（按既定决策）
- **信使复位**：InMemory `Dispose` 重置进程全局 messenger 的语义保留（视为显式关停）。
- **MessageMetadata 索引器 getter** 保持宽松返回 null（不为 null 抛错），依赖 `GetTypeName()` 的 null 语义。
- **RabbitMq/ActiveMq 深改**：channel 复用、AutoAck 统一、ActiveMq 回复队列生命周期管理、broker 侧 `CancellationTokenRegistration` 全面推开等未做（无 broker 验证环境）。