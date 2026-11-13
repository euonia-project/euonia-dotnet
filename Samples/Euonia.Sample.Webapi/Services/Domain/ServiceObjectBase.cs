using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// 命令对象的基类，这些命令对象需要访问延迟服务提供程序和当前用户主体。
/// </summary>
/// <typeparam name="T">派生自 <see cref="ServiceObjectBase{T}"/> 的具体命令对象类型。</typeparam>
public abstract class ServiceObjectBase<T> : ServiceObject<T>, IHasLazyServiceProvider, IDomainService
	where T : ServiceObjectBase<T>
{
	/// <summary>
	/// 获取或设置用于按需解析服务的延迟服务提供程序。
	/// </summary>
	/// <remarks>
	/// 此属性由依赖注入容器注入。请仅在需要时使用该提供程序解析服务
	///（例如作用域服务或瞬态服务），以避免在构造期间进行不必要的解析。
	/// </remarks>
	[Inject]
	public virtual ILazyServiceProvider LazyServiceProvider { get; set; } = null!;

	/// <summary>
	/// 获取当前已授权用户的主体。
	/// </summary>
	/// <remarks>
	/// 访问时，<see cref="UserPrincipal"/> 从 <see cref="LazyServiceProvider"/> 中解析。
	/// 如果容器中未注册 <see cref="UserPrincipal"/> 服务，调用此属性将抛出异常。
	/// </remarks>
	protected virtual UserPrincipal Identity => LazyServiceProvider.GetRequiredService<UserPrincipal>();
}