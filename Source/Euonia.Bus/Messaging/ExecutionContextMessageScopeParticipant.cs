using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 把消息信封中的身份写入执行上下文；HTTP 已在中间件写入时保持父级值。
/// </summary>
public sealed class ExecutionContextMessageScopeParticipant : IMessageExecutionScopeParticipant
{
	/// <inheritdoc />
	public int Order => 0;

	/// <inheritdoc />
	public ValueTask<MessageExecutionScopeEntry> EnterAsync(IMessageContext message, IServiceProvider services, CancellationToken cancellationToken = default)
	{
		var accessor = services.GetService<IExecutionContextAccessor>();
		if (accessor?.Current != null)
		{
			// HTTP 或外层消息作用域已安装执行上下文，不重复覆盖。
			return ValueTask.FromResult(new MessageExecutionScopeEntry());
		}

		var user = message.User as ClaimsPrincipal;
		var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		var userName = user?.FindFirst(ClaimTypes.Name)?.Value;
		var correlationId = message.CorrelationId;
		var traceId = message.RequestTraceId;

		Func<IDisposable?> install = () => AmbientExecutionContext.Push(values =>
		{
			values.UserId = userId;
			values.UserName = userName;
			values.CorrelationId = correlationId;
			values.RequestTraceId = traceId;
		});

		return ValueTask.FromResult(new MessageExecutionScopeEntry(install));
	}
}
