using Nerosoft.Euonia.Bus.Tests.Commands;

namespace Nerosoft.Euonia.Bus.Tests.Handlers;

/// <summary>
/// 处理 <see cref="UserExceptionCommand"/> 并抛出异常，用于验证
/// <see cref="IHandler{TMessage}"/>（Unit 响应）路径下异常能够传播到调用方。
/// </summary>
public class UserExceptionCommandHandler : IHandler<UserExceptionCommand>
{
	/// <summary>
	/// 处理 <see cref="UserExceptionCommand"/> 并返回以 <see cref="NotFoundException"/> 失败的任务。
	/// </summary>
	/// <param name="message">待处理的命令。</param>
	/// <param name="messageContext">消息上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>始终为失败的任务。</returns>
	/// <exception cref="NotFoundException">任务以该异常失败。</exception>
	public Task HandleAsync(UserExceptionCommand message, IMessageContext messageContext, CancellationToken cancellationToken = default)
	{
		return Task.FromException(new NotFoundException("User not found"));
	}
}

