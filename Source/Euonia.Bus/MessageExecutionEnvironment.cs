using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Execution;
using Nerosoft.Euonia.Messaging;
using Nerosoft.Euonia.Telemetry;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 承载一个宿主容器，并为每次消息处理创建执行作用域。
/// </summary>
/// <remarks>
/// 所有处理器调用路径（处理器上下文、收件箱重试、发件箱重投递）都通过本类型执行。
/// 执行作用域负责服务提供程序的获取/复用与请求上下文安装，宿主组件本身不捕获 scoped 服务。
/// </remarks>
internal sealed class MessageExecutionEnvironment
{
	private readonly IServiceProvider _container;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger? _logger;

	/// <summary>
	/// 使用宿主容器创建执行环境。
	/// </summary>
	/// <param name="container">宿主容器，用于创建作用域并识别“复用了根容器”。</param>
	/// <param name="logger">可选日志记录器。</param>
	internal MessageExecutionEnvironment(IServiceProvider container, ILogger? logger)
	{
		ArgumentNullException.ThrowIfNull(container);

		_container = container;
		_scopeFactory = container.GetRequiredService<IServiceScopeFactory>();
		_logger = logger;
	}

	/// <summary>
	/// 在描述符指定的执行环境中运行处理器，并返回结果。
	/// </summary>
	/// <typeparam name="TResult">处理结果类型。</typeparam>
	/// <param name="descriptor">执行环境描述符。</param>
	/// <param name="message">消息上下文；出站重投递等无入站消息的场景可为 <c>null</c>。</param>
	/// <param name="execute">使用实际服务提供程序执行的处理委托。</param>
	/// <returns>处理结果。</returns>
	internal async Task<TResult> RunAsync<TResult>(
		MessageExecutionDescriptor descriptor,
		MessageContext? message,
		Func<IServiceProvider, Task<TResult>> execute)
	{
		ArgumentNullException.ThrowIfNull(execute);

		await using var scope = new MessageExecutionScope(_container, _scopeFactory, descriptor, message, _logger);
		var entries = await scope.ResolveParticipantsAsync(message).ConfigureAwait(false);
		var result = scope.ApplyParticipants(entries);

		using var span = StartSpan(scope, message);
		ApplyTenantTags(span);

		if (!ShouldExecute(result, message))
		{
			return default!;
		}

		try
		{
			var value = await execute(scope.ServiceProvider).ConfigureAwait(false);
			span.SetOutcome(true);
			return value;
		}
		catch (Exception exception)
		{
			span.RecordException(exception);
			throw;
		}
	}

	/// <summary>
	/// 在描述符指定的执行环境中运行处理器，并返回参与者裁决。
	/// </summary>
	/// <param name="descriptor">执行环境描述符。</param>
	/// <param name="message">消息上下文；出站重投递等无入站消息的场景可为 <c>null</c>。</param>
	/// <param name="execute">使用实际服务提供程序执行的处理委托。</param>
	/// <returns>参与者裁决；处理器未执行时为 <see cref="MessageExecutionResult.Skip"/> 或 <see cref="MessageExecutionResult.DeadLetter"/>。</returns>
	internal async Task<MessageExecutionResult> RunWithResultAsync(
		MessageExecutionDescriptor descriptor,
		MessageContext? message,
		Func<IServiceProvider, Task> execute)
	{
		ArgumentNullException.ThrowIfNull(execute);

		await using var scope = new MessageExecutionScope(_container, _scopeFactory, descriptor, message, _logger);
		var entries = await scope.ResolveParticipantsAsync(message).ConfigureAwait(false);
		var result = scope.ApplyParticipants(entries);

		using var span = StartSpan(scope, message);
		ApplyTenantTags(span);

		if (result.Action != MessageExecutionScopeAction.Continue)
		{
			span.SetOutcome(false, result.DeadLettered ? "DeadLettered" : "Skipped");
			return result;
		}

		try
		{
			await execute(scope.ServiceProvider).ConfigureAwait(false);
			span.SetOutcome(true);
			return MessageExecutionResult.Completed;
		}
		catch (Exception exception)
		{
			span.RecordException(exception);
			throw;
		}
	}

	/// <summary>
	/// 创建消息消费 Span；未安装遥测时返回空句柄。
	/// </summary>
	private static TelemetryActivity StartSpan(MessageExecutionScope scope, MessageContext? message)
	{
		try
		{
			var factory = scope.ServiceProvider.GetService<ITelemetrySourceFactory>();
			if (factory == null || message == null)
			{
				return default;
			}

			var propagator = scope.ServiceProvider.GetService<ITelemetryPropagator>();
			var parent = message.Metadata is { } metadata ? propagator?.Extract(metadata) : null;

			// Outbox/Inbox 重投：原始 trace 作为 Link，避免把重投当成同一调用的子 Span。
			List<ActivityLink>? links = null;
			if (propagator != null &&
			    message.Metadata is { } linkMetadata &&
			    linkMetadata.TryGetValue(MessageMetadataKeys.LinkTraceParent, out var linkValue) &&
			    linkValue is string linkTraceParent &&
			    !string.IsNullOrWhiteSpace(linkTraceParent))
			{
				var linkContext = propagator.Extract(new Dictionary<string, object>
				{
					[TraceHeaders.TraceParent] = linkTraceParent,
				});

				if (linkContext is { } context && context != default)
				{
					links = [new ActivityLink(context)];
				}
			}

			var source = factory.Create("Nerosoft.Euonia.Bus");
			return source.StartActivity(
				TelemetryNames.BusProcess,
				ActivityKind.Consumer,
				parent,
				links,
				tags:
				[
					new KeyValuePair<string, object?>(TelemetryNames.MessagingOperationType, "process"),
					new KeyValuePair<string, object?>(TelemetryNames.MessagingMessageId, message.MessageId),
					new KeyValuePair<string, object?>(TelemetryNames.MessagingConversationId, message.ConversationId),
					new KeyValuePair<string, object?>(TelemetryNames.MessagingTransport, message.Transport),
					new KeyValuePair<string, object?>(TelemetryNames.CorrelationId, message.CorrelationId),
				]);
		}
		catch (Exception)
		{
			// 遥测失败开放。
			return default;
		}
	}

	/// <summary>
	/// 参与者安装后补充租户标签（只进 Trace）。
	/// </summary>
	private static void ApplyTenantTags(TelemetryActivity span)
	{
		var context = AmbientExecutionContext.Current;
		if (context == null)
		{
			return;
		}

		span.SetTag(TelemetryNames.TenantId, context.TenantId);
		span.SetTag(TelemetryNames.TenantActorId, context.ActorTenantId);

		if (context.IsCrossTenant)
		{
			span.SetTag(TelemetryNames.TenantCross, true);
		}
	}

	private static bool ShouldExecute(MessageExecutionResult result, MessageContext? message)
	{
		if (result.DeadLettered)
		{
			throw new MessageDeadLetteredException(message?.MessageId, "The message was dead-lettered by an execution scope participant.");
		}

		return !result.Skipped;
	}

	/// <summary>
	/// 在描述符指定的执行环境中运行处理器。
	/// </summary>
	/// <param name="descriptor">执行环境描述符。</param>
	/// <param name="message">消息上下文；出站重投递等无入站消息的场景可为 <c>null</c>。</param>
	/// <param name="execute">使用实际服务提供程序执行的处理委托。</param>
	/// <returns>表示处理操作的任务。</returns>
	internal async Task RunAsync(
		MessageExecutionDescriptor descriptor,
		MessageContext? message,
		Func<IServiceProvider, Task> execute)
	{
		ArgumentNullException.ThrowIfNull(execute);

		await using var scope = new MessageExecutionScope(_container, _scopeFactory, descriptor, message, _logger);
		var entries = await scope.ResolveParticipantsAsync(message).ConfigureAwait(false);
		var result = scope.ApplyParticipants(entries);

		using var span = StartSpan(scope, message);
		ApplyTenantTags(span);

		if (!ShouldExecute(result, message))
		{
			return;
		}

		try
		{
			await execute(scope.ServiceProvider).ConfigureAwait(false);
			span.SetOutcome(true);
		}
		catch (Exception exception)
		{
			span.RecordException(exception);
			throw;
		}
	}
}
