namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示远程调用的回复结果载体。
/// 用于在 HTTP / gRPC 传输过程中统一承载成功结果与失败错误信息。
/// </summary>
/// <typeparam name="TResult">回复结果的类型。</typeparam>
public class RemoteReply<TResult>
{
	/// <summary>
	/// 获取或设置一个值，指示远程调用是否成功。
	/// </summary>
	public bool IsSuccess { get; set; }

	/// <summary>
	/// 获取或设置成功时的回复结果。
	/// </summary>
	public TResult Result { get; set; }

	/// <summary>
	/// 获取或设置失败时的错误信息。
	/// </summary>
	public RemoteError Error { get; set; }

	/// <summary>
	/// 初始化 <see cref="RemoteReply{TResult}"/> 类的新实例。
	/// </summary>
	public RemoteReply()
	{
	}

	/// <summary>
	/// 初始化 <see cref="RemoteReply{TResult}"/> 类的新实例。
	/// </summary>
	/// <param name="result">回复结果。</param>
	/// <param name="isSuccess">是否成功。</param>
	/// <param name="error">错误信息。</param>
	public RemoteReply(TResult result, bool isSuccess, RemoteError error)
	{
		Result = result;
		IsSuccess = isSuccess;
		Error = error;
	}

	/// <summary>
	/// 创建一个表示成功的回复结果。
	/// </summary>
	/// <param name="result">回复结果。</param>
	/// <returns>表示成功的回复结果。</returns>
	public static RemoteReply<TResult> Success(TResult result)
	{
		return new RemoteReply<TResult>(result, true, null);
	}

	/// <summary>
	/// 创建一个表示失败的回复结果。
	/// </summary>
	/// <param name="error">错误信息。</param>
	/// <returns>表示失败的回复结果。</returns>
	public static RemoteReply<TResult> Failure(RemoteError error)
	{
		return new RemoteReply<TResult>(default, false, error);
	}
}