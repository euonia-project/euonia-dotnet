namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示远程调用过程中发生的错误信息载体。
/// 用于在 HTTP / gRPC 传输过程中携带异常的完整信息，便于客户端还原异常。
/// </summary>
public class RemoteError
{
	/// <summary>
	/// 获取或设置异常类型的完整名称。
	/// </summary>
	public string Type { get; set; }

	/// <summary>
	/// 获取或设置异常消息。
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	/// 获取或设置异常堆栈跟踪。
	/// </summary>
	public string StackTrace { get; set; }

	/// <summary>
	/// 从指定异常创建 <see cref="RemoteError"/> 实例。
	/// </summary>
	/// <param name="exception">要封装的异常。</param>
	/// <returns>表示异常信息的 <see cref="RemoteError"/> 实例。</returns>
	public static RemoteError Create(Exception exception)
	{
		return new RemoteError
		{
			Type = exception?.GetType().FullName,
			Message = exception?.Message,
			StackTrace = exception?.StackTrace,
		};
	}

	/// <summary>
	/// 将 <see cref="RemoteError"/> 还原为异常实例。
	/// 优先还原为原始异常类型；若无法还原，则回退为 <see cref="MessageDeliverException"/>。
	/// </summary>
	/// <returns>还原后的异常实例。</returns>
	public Exception ToException()
	{
		var type = TryResolveType(Type);
		if (type == null || !typeof(Exception).IsAssignableFrom(type) || type.GetConstructor(new[] { typeof(string) }) == null)
		{
			return new MessageDeliverException(Message ?? "Remote call failed without detailed error information.");
		}

		try
		{
			var exception = (Exception)Activator.CreateInstance(type, Message);
			if (exception != null)
			{
				exception.GetType().GetProperty("Source")?.SetValue(exception, null);
				return exception;
			}
		}
		catch
		{
			// 还原失败时回退到通用异常。
		}

		return new MessageDeliverException(Message ?? "Remote call failed without detailed error information.");
	}

	private static Type TryResolveType(string typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
		{
			return null;
		}

		var type = System.Type.GetType(typeName);
		if (type != null)
		{
			return type;
		}

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			type = assembly.GetType(typeName);
			if (type != null)
			{
				return type;
			}
		}

		return null;
	}
}