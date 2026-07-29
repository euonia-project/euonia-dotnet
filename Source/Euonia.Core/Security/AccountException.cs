using System.Security.Authentication;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 身份验证期间与账户相关的错误所抛出的异常。
/// </summary>
public abstract class AccountException : AuthenticationException
{
	/// <summary>
	/// 使用指定的账户标识初始化 <see cref="AccountException"/> 类的新实例。
	/// </summary>
	/// <param name="identity">与错误关联的账户标识。</param>
	protected AccountException(string identity)
	{
		Identity = identity;
	}

	/// <summary>
	/// 使用指定的错误消息初始化 <see cref="AccountException"/> 类的新实例。
	/// </summary>
	/// <param name="identity">与错误关联的账户标识。</param>
	/// <param name="message">描述错误的消息。</param>
	protected AccountException(string identity, string message)
		: base(message)
	{
		Identity = identity;
	}

	/// <summary>
	/// 使用指定的错误消息和对导致此异常的内部异常的引用初始化 <see cref="AccountException"/> 类的新实例。
	/// </summary>
	/// <param name="identity">与错误关联的账户标识。</param>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的异常，如果没有内部异常则为 <c>null</c>。</param>
	protected AccountException(string identity, string message, Exception innerException)
		: base(message, innerException)
	{
		Identity = identity;
	}

	/// <summary>
	/// 获取导致异常的账户标识。
	/// </summary>
	public string Identity { get; }

	/// <summary>
	/// 获取用于存储账户错误额外详细信息或元数据的字典。
	/// 键为字符串类型，值为任意对象。
	/// </summary>
	public virtual Dictionary<string, object> Details { get; } = new();
}