using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 负责将当前请求的认证信息与用户上下文挂载到消息元数据中，供跨服务传递。
/// </summary>
/// <typeparam name="TMessage">由管道处理的消息类型。必须是实现了 <see cref="IMessageEnvelope"/> 接口的类。</typeparam>
/// <typeparam name="TResponse">管道返回的响应类型。</typeparam>
/// <remarks>
/// 将请求头中的 Bearer Token（<c>Authorization</c>）与已认证用户的身份信息
/// （名称、ID、Code、租户）写入消息元数据，下游服务解析元数据即可恢复用户上下文。
/// </remarks>
public class UserContextBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IMessageEnvelope
{
	private readonly IServiceScopeFactory _scopeFactory;

	/// <summary>
	/// 初始化 <see cref="UserContextBehavior{TMessage, TResponse}"/> 类的新实例。
	/// </summary>
	/// <param name="scopeFactory">用于在每次处理时创建作用域以解析 scoped 的 <see cref="UserPrincipal"/> 和 <see cref="IRequestContextAccessor"/>。</param>
	/// <remarks>
	/// <see cref="UserPrincipal"/> 与 <see cref="IRequestContextAccessor"/> 均注册为 scoped 服务，
	/// 若在构造函数中从注入的 <see cref="IServiceProvider"/> 直接解析（行为从根容器解析时），
	/// 将得到空值，用户信息会静默丢失；因此改为在 <see cref="HandleAsync"/> 内按作用域解析。
	/// </remarks>
	public UserContextBehavior(IServiceScopeFactory scopeFactory)
	{
		_scopeFactory = scopeFactory;
	}

	/// <summary>
	/// 处理管道中的消息，将请求头中的 Bearer Token 和已认证用户信息添加到消息元数据中，然后调用下一个管道委托。
	/// </summary>
	/// <param name="context">正在处理的消息信封。</param>
	/// <param name="next">下一个要调用的管道委托。</param>
	/// <returns>包含管道响应结果的任务。</returns>
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		using var scope = _scopeFactory.CreateScope();
		var provider = scope.ServiceProvider;
		var contextAccessor = provider.GetService<IRequestContextAccessor>();
		var user = provider.GetService<UserPrincipal>();

		var token = contextAccessor?.Context?.Authorization ?? string.Empty;

		if (!string.IsNullOrEmpty(token))
		{
			if (token.StartsWith("Bearer") && !token.Equals("Bearer null", StringComparison.OrdinalIgnoreCase))
			{
				context.Metadata.Set("Authorization", token);
			}
		}

		if (user is { IsAuthenticated: true })
		{
			context.Metadata.Set("$nerosoft:user.name", user.Username);
			context.Metadata.Set("$nerosoft:user.id", user.UserId);
			context.Metadata.Set("$nerosoft:user.code", user.Code);
			context.Metadata.Set("$nerosoft:user.tenant", user.Tenant);
		}

		{
			// prevent code analysis
		}
		return await next(context);
	}
}