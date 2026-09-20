# Euonia.Bus CallAsync 优化与扩展报告

## 概述
本次针对 `euonia-net` Euonia.Bus 的请求-响应调用（`CallAsync`）系列接口做了**优化**与**扩展**，
并补充测试：

- **修复**：三个基于委托的重载（`Func<IServiceProvider, Task<TResult>>` 及
  `IBus` 默认方法 `Func<Task<TResult>>`、`Func<TService, Task<TResult>>`）此前**吞掉**调用方传入的
  `CancellationToken`，调用一旦开始无法取消。现通过 `Task.WaitAsync(cancellationToken)` 真正支持取消。
- **新增**：`CallOptions.Timeout`（毫秒，继承自 `ExtendableOptions`，`CallBuilder.WithTimeout(...)` 可设置）
  在 `CallAsync` 上正式生效——限时内未完成则抛出 `TimeoutException`；由调用方主动取消仍是
  `OperationCanceledException`，语义可区分。
- **重构**：两个消息重载（`CallAsync<TRequest, TResponse>(...)` 与
  `CallAsync<TResult>(IRequest<TResult>, ...)`）的重复实现合并为共享内部方法 `CallAsyncCore`，
  行为零变化（信封字段、传输确定、管道、超时逻辑统一）。
- **默认方法与 `CallAsync<TService, TResult>`** 默认接口方法改为委托到
  `Func<IServiceProvider, Task<TResult>>` 重载，从而自动获得取消支持。

验证：`Euonia.slnx` 0 错误 0 警告；`Euonia.Test.slnx` 0 错误（仅存既有的、
与本次无关的 `xUnit1031` 警告）；`Euonia.Bus.Tests` **67/67**（新增 11 个用例）、
`Euonia.Bus.InMemory.Tests` **10/10** 通过。

---

## 一、取消支持（行为修复）

### `Source/Euonia.Bus/Dispatching/MessageBus.cs`
```csharp
public async Task<TResult> CallAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler, CancellationToken cancellationToken = default)
{
	ArgumentNullException.ThrowIfNull(handler);
	return await handler(_accessor.ServiceProvider).WaitAsync(cancellationToken).ConfigureAwait(false);
}
```
- 取消后即使委托自身不感知令牌，调用方也会立即收到 `OperationCanceledException`，
  不再无限期悬挂。
- 新增 `ArgumentNullException.ThrowIfNull(handler)`（此前缺失，空委托会产生含糊的空引用异常）。

### `Source/Euonia.Bus/Core/IBus.cs`
```csharp
Task<TResult> CallAsync<TResult>(Func<Task<TResult>> handler, CancellationToken cancellationToken = default)
{
	ArgumentNullException.ThrowIfNull(handler);
	return CallAsync(_ => handler(), cancellationToken);   // 原来直接 handler()，忽略取消令牌
}
```
- `CallAsync<TService, TResult>(...)` 默认方法本就委托给 `Func<IServiceProvider, ...>` 重载，
  自动受益，无需改动。

> 说明：消息重载（经传输器）的取消令牌原本就透传给 `transport.CallAsync(...)`，未作改动。

---

## 二、超时支持（新扩展）

### 消费点：`MessageBus.CallAsyncCore`
```csharp
private Task<TResponse> CallAsyncCore<TRequest, TResponse>(...)
{
	// ... 构造 RoutedMessage、MetadataSetter、_dispatcher.Determine、transports!.First() ...
	var result = RunWithPipelineAsync(pack, behavior, (transport, p) => transport.CallAsync<TRequest, TResponse>(p, cancellationToken), transportName);
	return ApplyTimeoutAsync(result, options.Timeout, cancellationToken);
}
```

### `ApplyTimeoutAsync`（辅助）
```csharp
private async Task<TResponse> ApplyTimeoutAsync<TResponse>(Task<TResponse> task, long timeout, CancellationToken cancellationToken)
{
	if (timeout <= 0)
	{
		return await task.ConfigureAwait(false);
	}

	using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
	cts.CancelAfter(TimeSpan.FromMilliseconds(timeout));
	try
	{
		return await task.WaitAsync(cts.Token).ConfigureAwait(false);
	}
	catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
	{
		throw new TimeoutException($"The call did not complete within the configured timeout ({timeout} ms).");
	}
}
```

语义：
- `Timeout <= 0`：不启用超时，零额外开销（不创建链接 CTS）。
- 超时触发 → `TimeoutException`（含配置的超时毫秒数）。
- 调用方自己取消 → 原样保留 `OperationCanceledException`（`when` 过滤器区分）。

配置入口（现有 API，无需新增）：
- `CallOptions { Channel, Timeout = 100 }`
- `bus.Call<TReq, TRes>(msg).WithTimeout(100).ExecuteAsync(ct)`

作用域：仅 `CallAsync` 消费 `Timeout`；发送（`SendAsync`）与发布（`PublishAsync`）
仍不消费该值（`ExtendableOptions.Timeout` 注释已注明）。

---

## 三、去重重构（内部行为不变）

### 合并前（两处几乎相同的 ~30 行）
`CallAsync<TRequest, TResponse>` 与 `CallAsync<TResult>(IRequest<TResult>, ...)`
各自重复：构造 `RoutedMessage` 信封 → `MetadataSetter` → `Determine` → `First()` → `RunWithPipelineAsync`。

### 合并后
```csharp
public Task<TResponse> CallAsync<TRequest, TResponse>(TRequest message, ...)
{
	// options 兜底、GetChannel、Convention.IsRequest 校验（各自保留）
	return CallAsyncCore(message, channel, messageType, options, behavior, cancellationToken);
}

public Task<TResult> CallAsync<TResult>(IRequest<TResult> request, ...)
{
	// options 兜底、GetChannel、Convention.IsRequest 校验（各自保留）
	return CallAsyncCore<IRequest<TResult>, TResult>(request, channel, messageType, options, behavior, cancellationToken);
}
```
- 校验（`IsRequest`）与渠道解析留在各自公共重载内，错误语义与消息类型原样保留；
- `CallAsyncCore` 承担信封构造、传输确定、管道调用与 `ApplyTimeoutAsync`；
- 信封静态类型维持原样（普通消息重载为 `RoutedMessage<TRequest>`，`IRequest<TResult>` 重载为
  `RoutedMessage<IRequest<TResult>>`），避免改动序列化/管道行为。

---

## 四、测试（`Tests/Euonia.Bus.Tests/Consistency/CallAsyncTests.cs`，新增 11 个用例）

桩传输器 `StubTransporter` 模拟请求-响应往返，不引入内存信使；用例覆盖：

| 类别 | 用例 | 断言 |
| --- | --- | --- |
| 消息重载 | `GenericMessageOverload_ReturnsResult` | 结果 42，信封通道正确 |
| 消息重载 | `IRequestOverload_ReturnsResult_AndPropagatesOptions` | 结果 7，`MessageId/CorrelationId/RequestTraceId` 透传 |
| 默认方法 | `DefaultCallAsyncOverload_ReturnsResult` | `CallAsync(msg, ct)` 走默认重载返回 42 |
| 构建器 | `CallBuilder_ReturnsResult` | `bus.Call<...>().WithChannel(...).ExecuteAsync()` 返回 11 |
| 超时 | `Timeout_ThrowsTimeoutException` | `Timeout=100`，桩延迟 5s → `TimeoutException` |
| 构建器超时 | `CallBuilder_WithTimeout_ThrowsTimeoutException` | `WithTimeout(100)` → `TimeoutException` |
| 关闭超时 | `TimeoutDisabled_ReturnsResult` | `Timeout=0` 正常返回 |
| 消息重载取消 | `MessageOverload_UserCancellation_...` | 调用中取消 → `OperationCanceledException` |
| 委托取消 | `DelegateOverload_HonorsCancellation` | `Func<IServiceProvider,...>` 取消立即返回 |
| 默认方法取消 | `FuncOverload_HonorsCancellation` | `Func<Task<TResult>>` 取消立即返回 |
| 类型化服务取消 | `ServiceFuncOverload_HonorsCancellation` | `Func<TService, Task<TResult>>` 取消立即返回 |

---

## 五、验证结果

| 项目 | 结果 |
| --- | --- |
| `dotnet build Euonia.slnx` | 0 错误 0 警告 |
| `dotnet build Euonia.Test.slnx` | 0 错误（1 个既有 `xUnit1031`，与本次无关） |
| `Euonia.Bus.Tests` | 67/67 通过（新增 11） |
| `Euonia.Bus.InMemory.Tests` | 10/10 通过（共享 ServiceBusTests 请求-响应路径回归） |