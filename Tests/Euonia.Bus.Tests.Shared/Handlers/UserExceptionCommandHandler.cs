using Nerosoft.Euonia.Bus.Tests.Commands;

namespace Nerosoft.Euonia.Bus.Tests.Handlers;

/// <summary>
/// 处理 <see cref="UserExceptionCommand"/> 并抛出异常，用于验证
/// <see cref="IHandler{TMessage}"/>（Unit 响应）路径下异常能够传播到调用方。
/// </summary>
public class UserExceptionCommandHandler : IHandler<UserExceptionCommand>
{
	public Task HandleAsync(UserExceptionCommand message, IMessageContext messageContext, CancellationToken cancellationToken = default)
	{
		return Task.FromException(new NotFoundException("User not found"));
	}
}

