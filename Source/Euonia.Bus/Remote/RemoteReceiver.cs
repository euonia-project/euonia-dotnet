using System.Text.Json;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 远程消息接收器。
/// 反序列化 HTTP / gRPC 传输的请求负载，调用 <see cref="IHandlerContext"/> 处理消息，
/// 并将处理结果或错误序列化为 <see cref="RemoteReply{TResult}"/> 的 JSON 字符串返回。
/// </summary>
public static class RemoteReceiver
{
	/// <summary>
	/// JSON 中消息类型名称的属性键（camelCase / PascalCase）。
	/// </summary>
	private static readonly string[] TypeNameKeys = { "typeName", "TypeName" };

	/// <summary>
	/// 处理远程请求负载并返回序列化后的回复 JSON 字符串。
	/// 负载反序列化失败或消息处理失败时，统一以 <see cref="RemoteReply{TResult}.Failure"/> 形式返回。
	/// </summary>
	/// <param name="serializer">用于反序列化请求与序列化回复的消息序列化器。</param>
	/// <param name="handler">用于处理消息的处理器上下文。</param>
	/// <param name="payload">包含 <see cref="IMessageEnvelope"/> JSON 数据的请求负载。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>序列化后的 <see cref="RemoteReply{TResult}"/> JSON 字符串。</returns>
	public static async Task<string> ReceiveAsync(IMessageSerializer serializer, IHandlerContext handler, string payload, CancellationToken cancellationToken = default)
	{
		IMessageEnvelope message;
		MessageContext context;

		try
		{
			message = DeserializeMessage(serializer, payload);
			context = new MessageContext(message);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception exception)
		{
			return serializer.Serialize(RemoteReply<object>.Failure(RemoteError.Create(exception)));
		}

		var taskCompletion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (cancellationToken != CancellationToken.None)
		{
			cancellationToken.Register(() => taskCompletion.TrySetCanceled(), false);
		}

		context.Responded += OnResponded;
		context.Failed += OnFailed;
		context.Completed += OnCompleted;

		try
		{
			await HandleAsync(handler, message.Channel, message.Payload, context, cancellationToken);

			var result = await taskCompletion.Task;
			var reply = RemoteReply<object>.Success(result);
			return serializer.Serialize(reply);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception exception)
		{
			var reply = RemoteReply<object>.Failure(RemoteError.Create(exception));
			return serializer.Serialize(reply);
		}
		finally
		{
			context.Responded -= OnResponded;
			context.Failed -= OnFailed;
			context.Completed -= OnCompleted;
		}

		void OnResponded(object sender, MessageRepliedEventArgs e)
		{
			taskCompletion.TrySetResult(e.Result);
		}

		void OnFailed(object sender, Exception exception)
		{
			taskCompletion.TrySetException(exception);
		}

		void OnCompleted(object sender, MessageHandledEventArgs e)
		{
			taskCompletion.TryCompleteFromCompletedTask(Task.FromResult(default(object)));
		}
	}

	/// <summary>
	/// 调用处理器上下文处理消息，成功后触发 <see cref="MessageContext.Response(object)"/>。
	/// </summary>
	/// <param name="handler">消息处理器上下文。</param>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="message">要处理的消息对象。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	private static async Task HandleAsync(IHandlerContext handler, string channel, object message, MessageContext context, CancellationToken cancellationToken)
	{
		try
		{
			var result = await handler.HandleAsync(channel, message, context, cancellationToken);
			context.Response(result);
		}
		catch (Exception exception)
		{
			context.Failure(exception);
		}
		finally
		{
			context.Complete(null);
		}
	}

	/// <summary>
	/// 从 JSON 负载中反序列化消息信封。
	/// </summary>
	/// <param name="serializer">消息序列化器。</param>
	/// <param name="payload">包含消息信封 JSON 数据的字符串。</param>
	/// <returns>反序列化后的 <see cref="IMessageEnvelope"/> 实例。</returns>
	private static IMessageEnvelope DeserializeMessage(IMessageSerializer serializer, string payload)
	{
		var typeName = ReadTypeName(payload);
		if (string.IsNullOrWhiteSpace(typeName))
		{
			throw new MessageDeliverException("The remote message payload does not contain a valid message type name.");
		}

		var messageType = Type.GetType(typeName);
		if (messageType == null)
		{
			throw new MessageDeliverException($"Failed to resolve message type '{typeName}'.");
		}

		return serializer.DeserializeEnvelope(payload, messageType);
	}

	/// <summary>
	/// 从 JSON 字符串中读取消息类型名称。
	/// </summary>
	/// <param name="payload">包含消息信封 JSON 数据的字符串。</param>
	/// <returns>消息类型名称；未找到时返回 <c>null</c>。</returns>
	private static string ReadTypeName(string payload)
	{
		using var document = JsonDocument.Parse(payload);
		foreach (var key in TypeNameKeys)
		{
			if (document.RootElement.TryGetProperty(key, out var element))
			{
				return element.GetString();
			}
		}

		return null;
	}
}