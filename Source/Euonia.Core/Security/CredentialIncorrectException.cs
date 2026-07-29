namespace Nerosoft.Euonia.Security;

/// <summary>
/// 当提供的凭据不正确时抛出的异常。
/// </summary>
public class CredentialIncorrectException : CredentialException
{
	/// <summary>
	/// 使用指定的凭据初始化 <see cref="CredentialIncorrectException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">被确定为不正确的凭据对象。</param>
	public CredentialIncorrectException(object credential)
		: base(credential)
	{
	}

	/// <summary>
	/// 使用指定的凭据和自定义错误消息初始化 <see cref="CredentialIncorrectException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">被确定为不正确的凭据对象。</param>
	/// <param name="message">描述错误的消息。</param>
	public CredentialIncorrectException(object credential, string message)
		: base(credential, message)
	{
	}

	/// <summary>
	/// 使用指定的凭据、自定义错误消息和内部异常初始化 <see cref="CredentialIncorrectException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">被确定为不正确的凭据对象。</param>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的异常（如果有）。</param>
	public CredentialIncorrectException(object credential, string message, Exception innerException)
		: base(credential, message, innerException)
	{
	}
}