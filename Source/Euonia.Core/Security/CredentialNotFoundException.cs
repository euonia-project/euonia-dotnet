namespace Nerosoft.Euonia.Security;

/// <summary>
/// 当在身份验证或查找过程中找不到凭据时抛出的异常。
/// 携带未找到的凭据对象以便诊断。
/// </summary>
public class CredentialNotFoundException : CredentialException
{
	/// <summary>
	/// 使用指定的凭据初始化 <see cref="CredentialNotFoundException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">未找到的凭据对象。</param>
	public CredentialNotFoundException(object credential)
		: base(credential)
	{
	}

	/// <summary>
	/// 使用指定的凭据和错误消息初始化 <see cref="CredentialNotFoundException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">未找到的凭据对象。</param>
	/// <param name="message">描述错误的消息。</param>
	public CredentialNotFoundException(object credential, string message)
		: base(credential, message)
	{
	}

	/// <summary>
	/// 使用指定的凭据、错误消息和对导致此异常的内部异常的引用初始化 <see cref="CredentialNotFoundException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">未找到的凭据对象。</param>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的异常，如果没有内部异常则为 <c>null</c>。</param>
	public CredentialNotFoundException(object credential, string message, Exception innerException)
		: base(credential, message, innerException)
	{
	}
}