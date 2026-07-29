namespace Nerosoft.Euonia.Security;

/// <summary>
/// 当账户已过期且不能继续用于身份验证或访问时抛出的异常。
/// 携带已过期账户的标识信息以便诊断。
/// </summary>
public class AccountExpiredException : AccountException
{
	/// <summary>
	/// 使用指定的账户标识初始化 <see cref="AccountExpiredException"/> 类的新实例。
	/// </summary>
	/// <param name="identity">已过期账户的标识（例如用户名或账户 ID）。</param>
	public AccountExpiredException(string identity)
		: base(identity)
	{
	}

	/// <summary>
	/// 使用指定的错误消息和账户标识初始化 <see cref="AccountExpiredException"/> 类的新实例。
	/// </summary>
	/// <param name="identity">已过期账户的标识（例如用户名或账户 ID）。</param>
	/// <param name="message">描述错误的消息。</param>
	public AccountExpiredException(string identity, string message)
		: base(identity, message)
	{
	}

	/// <summary>
	/// 使用指定的错误消息、账户标识和对导致此异常的内部异常的引用初始化 <see cref="AccountExpiredException"/> 类的新实例。
	/// </summary>
	/// <param name="identity">已过期账户的标识（例如用户名或账户 ID）。</param>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的异常，如果没有内部异常则为 <c>null</c>。</param>
	public AccountExpiredException(string identity, string message, Exception innerException)
		: base(identity, message, innerException)
	{
	}
}