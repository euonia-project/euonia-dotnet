using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 支持延迟服务解析的可编辑业务对象基类。
/// </summary>
/// <typeparam name="TTarget">派生自该基类的具体可编辑对象类型。</typeparam>
public abstract class EditableObjectBase<TTarget> : EditableObject<TTarget>, IHasLazyServiceProvider
	where TTarget : EditableObjectBase<TTarget>
{
	/// <summary>
	/// 获取或设置延迟服务提供程序。
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="ILazyServiceProvider"/> 允许对象按需解析框架或应用程序服务，
	/// 而无需构造函数注入。该属性应由宿主或依赖注入基础设施提供。
	/// </para>
	/// <para>
	/// 该属性支持服务的延迟加载，从而在运行时实现依赖注入与服务解析。
	/// 请注意，该属性应由管理业务对象生命周期的框架或基础设施代码设置。
	/// </para>
	/// </remarks>
	//[Inject]
	public ILazyServiceProvider LazyServiceProvider { get; set; } = null!;
}