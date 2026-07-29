using System.Security.Authentication;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 身份验证期间与凭据相关的错误的基类异常。
/// 携带导致错误的凭据对象以及用于存储额外元数据的可选详细信息字典。
/// </summary>
public abstract class CredentialException : AuthenticationException
{
	/// <summary>
	/// 使用指定的凭据初始化 <see cref="CredentialException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">与错误关联的凭据对象。</param>
	protected CredentialException(object credential)
	{
		Credential = credential;
	}

	/// <summary>
	/// 使用指定的错误消息初始化 <see cref="CredentialException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">与错误关联的凭据对象。</param>
	/// <param name="message">描述错误的消息。</param>
	protected CredentialException(object credential, string message)
		: base(message)
	{
		Credential = credential;
	}

	/// <summary>
	/// 使用指定的错误消息和对导致此异常的内部异常的引用初始化 <see cref="CredentialException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">与错误关联的凭据对象。</param>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的异常，如果没有内部异常则为 <c>null</c>。</param>
	protected CredentialException(object credential, string message, Exception innerException)
		: base(message, innerException)
	{
		Credential = credential;
	}

	/// <summary>
	/// 获取导致异常的凭据对象。
	/// </summary>
	public object Credential { get; }

	/// <summary>
	/// 获取用于存储凭据错误额外详细信息或元数据的字典。
	/// 键为字符串类型，值为任意对象。
	/// </summary>
	public virtual Dictionary<string, object> Details { get; } = new();
}