using Nerosoft.Euonia.Bus.Tests.Requests;

namespace Nerosoft.Euonia.Bus.Tests.Handlers;

/// <summary>
/// 示例请求处理器，返回固定的用户数量。
/// </summary>
public class UserRequestHandler : IHandler<UserCountRequest, int>
{
	/// <summary>
	/// 处理 <see cref="UserCountRequest"/> 并返回固定结果 <c>1</c>。
	/// </summary>
	/// <param name="message">待处理的请求。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>固定为 <c>1</c> 的用户数量。</returns>
	public async Task<int> HandleAsync(UserCountRequest message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return 1;
	}
}