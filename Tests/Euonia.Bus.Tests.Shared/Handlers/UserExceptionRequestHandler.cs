using Nerosoft.Euonia.Bus.Tests.Requests;

namespace Nerosoft.Euonia.Bus.Tests.Handlers;

/// <summary>
/// 示例请求处理器，处理请求时以 <see cref="NotFoundException"/> 失败，用于验证调用异常传播。
/// </summary>
public class UserExceptionRequestHandler : IHandler<UserExceptionRequest, int>
{
	/// <summary>
	/// 处理 <see cref="UserExceptionRequest"/> 并返回以 <see cref="NotFoundException"/> 失败的任务。
	/// </summary>
	/// <param name="message">待处理的请求。</param>
	/// <param name="context">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>始终为失败的任务。</returns>
	/// <exception cref="NotFoundException">任务以该异常失败。</exception>
	public Task<int> HandleAsync(UserExceptionRequest message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		return Task.FromException<int>(new NotFoundException("User not found"));
	}
}

