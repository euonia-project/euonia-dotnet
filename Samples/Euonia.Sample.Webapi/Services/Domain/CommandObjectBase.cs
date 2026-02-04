using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// Base class for command objects that require access to a lazy service provider and the current user principal.
/// </summary>
/// <typeparam name="T">The concrete command object type deriving from <see cref="CommandObjectBase{T}"/>.</typeparam>
public abstract class CommandObjectBase<T> : CommandObject<T>, IHasLazyServiceProvider, IDomainService
	where T : CommandObjectBase<T>
{
	/// <summary>
	/// Gets or sets the lazy service provider used to resolve services on demand.
	/// </summary>
	/// <remarks>
	/// This property is injected by the dependency injection container. Use the provider to resolve services
	/// (for example, scoped or transient services) only when they are required, avoiding unnecessary resolution
	/// during construction.
	/// </remarks>
	[Inject]
	public virtual ILazyServiceProvider LazyServiceProvider { get; set; }

	/// <summary>
	/// Gets the current authorized user's principal.
	/// </summary>
	/// <remarks>
	/// The <see cref="UserPrincipal"/> is resolved from <see cref="LazyServiceProvider"/> when accessed.
	/// Calling this property will throw if a <see cref="UserPrincipal"/> service is not registered in the container.
	/// </remarks>
	protected virtual UserPrincipal Identity => LazyServiceProvider.GetRequiredService<UserPrincipal>();
}