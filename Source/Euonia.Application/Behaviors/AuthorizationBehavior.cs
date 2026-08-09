using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 负责将用户主体信息添加到消息元数据中的管道行为。
/// </summary>
/// <typeparam name="TMessage">由管道处理的消息类型。必须是实现了 <see cref="IMessageEnvelope"/> 接口的类。</typeparam>
/// <typeparam name="TResponse">管道返回的响应类型。</typeparam>
public class AuthorizationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IMessageEnvelope
{
	private readonly UserPrincipal _user;
	private readonly IRequestContextAccessor _contextAccessor;

	/// <summary>
	/// 初始化 <see cref="AuthorizationBehavior{TMessage, TResponse}"/> 类的新实例。
	/// </summary>
	/// <param name="provider">服务提供程序，用于解析 <see cref="UserPrincipal"/> 和 <see cref="IRequestContextAccessor"/>。</param>
	public AuthorizationBehavior(IServiceProvider provider)
	{
		_user = provider.GetService<UserPrincipal>();
		_contextAccessor = provider.GetService<IRequestContextAccessor>();
	}

	/// <summary>
	/// 处理管道中的消息，将请求头中的 Bearer Token 和已认证用户信息添加到消息元数据中，然后调用下一个管道委托。
	/// </summary>
	/// <param name="context">正在处理的消息信封。</param>
	/// <param name="next">下一个要调用的管道委托。</param>
	/// <returns>包含管道响应结果的任务。</returns>
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		if (_contextAccessor?.Context?.RequestHeaders.TryGetValue("Authorization", out var value) == true)
		{
			if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("Bearer") && !value.Equals("Bearer null", StringComparison.OrdinalIgnoreCase))
			{
				context.Metadata.Set("Authorization", value);
			}
		}

		if (_user is { IsAuthenticated: true })
		{
			context.Metadata.Set("$nerosoft:user.name", _user.Username);
			context.Metadata.Set("$nerosoft:user.id", _user.UserId);
			context.Metadata.Set("$nerosoft:user.code", _user.Code);
			context.Metadata.Set("$nerosoft:user.tenant", _user.Tenant);
		}


		return await next(context);
	}
}