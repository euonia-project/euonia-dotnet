using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// Base class for application services.
/// </summary>
public abstract class BaseApplicationService : IApplicationService
{
	/// <summary>
	/// Gets or sets the lazy service provider.
	/// </summary>
	public virtual ILazyServiceProvider LazyServiceProvider { get; set; }

	/// <summary>
	/// Gets the <see cref="IBus"/> instance.
	/// </summary>
	protected virtual IBus Bus => LazyServiceProvider.GetService<IBus>();

	/// <summary>
	/// Gets the current request user principal.
	/// </summary>
	protected virtual UserPrincipal User => LazyServiceProvider.GetService<UserPrincipal>();

	/// <summary>
	/// Gets the current request context accessor.
	/// </summary>
	protected virtual IRequestContextAccessor HttpRequestAccessor => LazyServiceProvider.GetService<IRequestContextAccessor>();
}