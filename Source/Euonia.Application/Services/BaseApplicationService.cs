using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 应用服务的基础类型。
/// </summary>
/// <remarks>
/// 通过 <see cref="LazyServiceProvider"/> 懒加载解析常用服务（消息总线、当前用户、请求上下文），
/// 派生应用服务可直接使用 <see cref="Bus"/>、<see cref="User"/> 与 <see cref="HttpRequestAccessor"/>。
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
}