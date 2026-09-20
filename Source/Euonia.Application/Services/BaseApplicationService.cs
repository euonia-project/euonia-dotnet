using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 应用服务的基础类型。
/// </summary>
/// <remarks>
/// 通过 <see cref="LazyServiceProvider"/> 懒加载解析常用服务（消息总线、当前用户、请求上下文、日志），
/// 派生应用服务可直接使用 <see cref="Bus"/>、<see cref="User"/>、<see cref="HttpRequestAccessor"/>、
/// <see cref="RequestContext"/> 与 <see cref="Logger"/>。
/// </remarks>
public abstract class BaseApplicationService : IApplicationService
{
	/// <summary>
	/// 获取或设置懒加载服务提供程序。
	/// </summary>
	public virtual ILazyServiceProvider LazyServiceProvider { get; set; }

	/// <summary>
	/// 获取 <see cref="IBus"/> 实例。
	/// </summary>
	protected virtual IBus Bus => LazyServiceProvider.GetService<IBus>();

	/// <summary>
	/// 获取当前请求的用户主体。
	/// </summary>
	protected virtual UserPrincipal User => LazyServiceProvider.GetService<UserPrincipal>();

	/// <summary>
	/// 获取当前请求上下文访问器。
	/// </summary>
	protected virtual IRequestContextAccessor HttpRequestAccessor => LazyServiceProvider.GetService<IRequestContextAccessor>();

	/// <summary>
	/// 获取当前请求上下文；请求上下文访问器缺失或当前不在请求流内时返回 <see langword="null"/>。
	/// </summary>
	protected virtual RequestContext RequestContext => HttpRequestAccessor?.Context;

	/// <summary>
	/// 获取当前请求的取消令牌；不在请求流内时返回 <see cref="CancellationToken.None"/>。
	/// </summary>
	protected virtual CancellationToken RequestAborted => RequestContext?.RequestAborted ?? CancellationToken.None;

	/// <summary>
	/// 获取以当前服务类型为类别的日志记录器。
	/// </summary>
	protected virtual ILogger Logger => LazyServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType()) ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
}