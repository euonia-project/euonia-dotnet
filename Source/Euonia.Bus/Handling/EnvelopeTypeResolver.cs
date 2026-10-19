namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 解析消息信封声明的负载泛型类型。
/// </summary>
internal static class EnvelopeTypeResolver
{
	/// <summary>
	/// 获取信封实现的 <see cref="IMessageEnvelope{T}"/> 中的消息类型。
	/// </summary>
	/// <param name="envelope">消息信封。</param>
	/// <returns>
	/// 信封声明的消息类型；信封未实现泛型接口时回退到负载的运行时类型。
	/// </returns>
	/// <remarks>
	/// 不能用 <c>envelope.Payload.GetType()</c> 代替：当消息以接口/基类类型发布时，
	/// 信封是 <c>RoutedMessage&lt;Base&gt;</c>，负载运行时却是派生类型；
	/// 按运行时类型强转 <c>IMessageEnvelope&lt;Derived&gt;</c> 会失败。
	/// </remarks>
	public static Type GetMessageType(IMessageEnvelope envelope)
	{
		if (envelope == null)
		{
			return typeof(object);
		}

		foreach (var @interface in envelope.GetType().GetInterfaces())
		{
			if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IMessageEnvelope<>))
			{
				return @interface.GetGenericArguments()[0];
			}
		}

		return envelope.Payload?.GetType() ?? typeof(object);
	}
}
