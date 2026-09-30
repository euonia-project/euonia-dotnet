namespace Nerosoft.Euonia.Security;

/// <summary>
/// 当凭据已过期且不能继续用于身份验证时抛出的异常。
/// 携带已过期凭据对象以便诊断。
/// </summary>
public class CredentialExpiredException : CredentialException
{
	/// <summary>
	/// 使用指定的凭据初始化 <see cref="CredentialExpiredException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">已过期的凭据对象。</param>
        /// <remarks>默认消息不携带凭据内容（避免泄漏敏感信息；异常→状态码管道读取 <see cref="Exception.Message"/>，无参消息会停留在 BCL 默认值）。</remarks>
        public CredentialExpiredException(object credential)
                : base(credential, "Credential expired.")
        {
        }

	/// <summary>
	/// 使用指定的凭据和错误消息初始化 <see cref="CredentialExpiredException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">已过期的凭据对象。</param>
	/// <param name="message">描述错误的消息。</param>
	public CredentialExpiredException(object credential, string message)
		: base(credential, message)
	{
	}

	/// <summary>
	/// 使用指定的凭据、错误消息和内部异常初始化 <see cref="CredentialExpiredException"/> 类的新实例。
	/// </summary>
	/// <param name="credential">已过期的凭据对象。</param>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的异常，如果没有内部异常则为 <c>null</c>。</param>
	public CredentialExpiredException(object credential, string message, Exception innerException)
		: base(credential, message, innerException)
	{
	}
}