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
	public AccountNotFoundException(string identity)
		: base(identity)
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