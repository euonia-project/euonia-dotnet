namespace Nerosoft.Euonia.Security;

/// <summary>
/// 当找不到具有指定标识的账户时抛出的异常。
/// 携带账户标识信息以便诊断。
/// </summary>
public class AccountNotFoundException : AccountException
{
	/// <summary>
	/// 使用指定的账户标识初始化 <see cref="AccountNotFoundException"/> 类的新实例。
	/// </summary>
	/// <param name="identity">未找到的账户的标识（例如用户名或账户 ID）。</param>
        /// <remarks>
        /// 默认消息用 <paramref name="identity"/> 格式化：异常→状态码管道
        /// （<c>ExceptionExtensions.GetErrorMessage</c> / <c>ApiExceptionMiddleware</c>）按<b>基类</b>匹配
        /// 并读取 <see cref="Exception.Message"/>，无参消息会停留在 BCL 默认值
        /// （"Exception of type '…' was thrown."）——既泄漏类型名又没有信息。
        /// </remarks>
        public AccountNotFoundException(string identity)
                : base(identity, $"Account not found: '{identity}'.")
        {
        }

        /// <summary>
        /// 使用指定的账户标识和自定义错误消息初始化 <see cref="AccountNotFoundException"/> 类的新实例。
        /// </summary>
        /// <param name="identity">未找到的账户的标识（例如用户名或账户 ID）。</param>
        /// <param name="message">描述错误的消息。</param>
	public AccountNotFoundException(string identity, string message)
		: base(identity, message)
	{
	}

	/// <summary>
	/// 使用指定的账户标识、自定义错误消息和内部异常初始化 <see cref="AccountNotFoundException"/> 类的新实例。
	/// </summary>
	/// <param name="identity">未找到的账户的标识（例如用户名或账户 ID）。</param>
	/// <param name="message">描述错误的消息。</param>
	/// <param name="innerException">导致当前异常的异常（如果有）。</param>
	public AccountNotFoundException(string identity, string message, Exception innerException)
		: base(identity, message, innerException)
	{
	}
}