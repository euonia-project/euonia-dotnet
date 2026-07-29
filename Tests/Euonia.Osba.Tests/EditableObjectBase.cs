using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// Base class for editable business objects that supports lazy service resolution.
/// </summary>
/// <typeparam name="TTarget">The concrete editable object type that derives from this base class.</typeparam>
public abstract class EditableObjectBase<TTarget> : EditableObject<TTarget>, IHasLazyServiceProvider
	where TTarget : EditableObjectBase<TTarget>
{
	/// <summary>
	/// Gets or sets the lazy service provider.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <see cref="ILazyServiceProvider"/> allows the object to resolve framework or application
	/// services on demand instead of requiring constructor injection. This property is expected to be
	/// provided by the host or dependency injection infrastructure.
	/// </para>
	/// <para>
	/// This property allows for lazy loading of services, enabling dependency injection and service resolution at runtime.
	/// Please note that this property should be set by the framework or infrastructure code that manages the lifecycle of business objects.
	/// </para>
	/// </remarks>
	//[Inject]
	public ILazyServiceProvider LazyServiceProvider { get; set; }
}