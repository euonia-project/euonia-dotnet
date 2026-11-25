using System.Reflection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// B1 回归（读取器层）：<see cref="PermissionRequirements.For(Type, MethodInfo)"/> 的方法级要求
/// 必须取「静态方法声明 ∪ 运行时类型上实际实现声明」的并集——派生 override 与 <c>new</c> 遮蔽上的
/// <see cref="PermissionAttribute"/> 不在静态方法的基类链上，只读静态方法会静默放行。
/// </summary>
public class PermissionRequirementsRuntimeResolutionTests
{
	private static readonly MethodInfo BaseRun = typeof(ResolutionBase).GetMethod(nameof(ResolutionBase.Run))!;

	private static readonly MethodInfo BasePlain = typeof(ResolutionBase).GetMethod(nameof(ResolutionBase.Plain))!;

	/// <summary>
	/// 按运行时类型收集时，基类声明与派生 override 声明取并集（修复前只有基类声明）。
	/// </summary>
	[Fact]
	public void For_RuntimeType_Should_Union_Static_And_Override_Declarations()
	{
		var requirements = PermissionRequirements.For(typeof(ResolutionDerived), BaseRun);

		Assert.Equal(["runtime:base", "runtime:derived"], requirements.Select(requirement => requirement.Permission));
	}

	/// <summary>
	/// 多层 override 链上的声明全部参与并集（实际实现的 inherit 链覆盖中间层）。
	/// </summary>
	[Fact]
	public void For_RuntimeType_Should_Union_Whole_Override_Chain()
	{
		var requirements = PermissionRequirements.For(typeof(ResolutionGrandDerived), BaseRun);

		Assert.Equal(3, requirements.Count);
		Assert.Contains(requirements, requirement => requirement.Permission == "runtime:base");
		Assert.Contains(requirements, requirement => requirement.Permission == "runtime:derived");
		Assert.Contains(requirements, requirement => requirement.Permission == "runtime:grand");
	}

	/// <summary>
	/// <c>new</c> 遮蔽按名称 + 参数签名解析：遮蔽方法上的声明并入要求（修复前只有基类声明）。
	/// </summary>
	[Fact]
	public void For_ShadowedMethod_Should_Union_Declarations()
	{
		var requirements = PermissionRequirements.For(typeof(ResolutionShadowDerived), BasePlain);

		Assert.Equal(2, requirements.Count);
		Assert.Contains(requirements, requirement => requirement.Permission == "runtime:plain");
		Assert.Contains(requirements, requirement => requirement.Permission == "runtime:shadow");
	}

	/// <summary>
	/// 运行时类型没有 override 时保留静态方法（含基类链）的声明，类型级声明在前。
	/// </summary>
	[Fact]
	public void For_WithoutOverride_Should_Keep_Base_Declarations()
	{
		var requirements = PermissionRequirements.For(typeof(ResolutionTypeLevelDerived), BaseRun);

		Assert.Equal(["runtime:type", "runtime:base"], requirements.Select(requirement => requirement.Permission));
	}

	/// <summary>
	/// 解析只针对被调用的那一个方法：兄弟方法上的声明不得混入。
	/// </summary>
	[Fact]
	public void For_Resolution_Should_Not_Leak_Sibling_Declarations()
	{
		var requirements = PermissionRequirements.For(typeof(ResolutionDerived), BaseRun);

		Assert.DoesNotContain(requirements, requirement => requirement.Permission == "runtime:sibling");
	}

	/// <summary>
	/// 并集结果按「运行时类型 + 方法」缓存：不同反射实例作键也命中同一份只读快照。
	/// </summary>
	[Fact]
	public void For_Implementation_Should_Be_Cached()
	{
		var first = PermissionRequirements.For(typeof(ResolutionDerived), BaseRun);
		var second = PermissionRequirements.For(typeof(ResolutionDerived), typeof(ResolutionBase).GetMethod(nameof(ResolutionBase.Run))!);

		Assert.Same(first, second);
	}

	/// <summary>
	/// 传入类型就是方法声明类型时保持静态语义：不解析、不并入其它声明。
	/// </summary>
	[Fact]
	public void For_DeclaringType_Should_Keep_Static_Semantics()
	{
		var requirements = PermissionRequirements.For(typeof(ResolutionBase), BaseRun);

		Assert.Equal(["runtime:base"], requirements.Select(requirement => requirement.Permission));
	}
}

/// <summary>
/// 解析基类：虚方法与兄弟方法各带声明，另有一个非虚方法供遮蔽用例使用。
/// </summary>
public class ResolutionBase
{
	/// <summary>
	/// 虚入口：声明 <c>runtime:base</c>。
	/// </summary>
	[Permission("runtime:base")]
	public virtual void Run()
	{
	}

	/// <summary>
	/// 非虚入口：声明 <c>runtime:plain</c>，供 <c>new</c> 遮蔽用例使用。
	/// </summary>
	[Permission("runtime:plain")]
	public void Plain()
	{
	}

	/// <summary>
	/// 兄弟方法：声明 <c>runtime:sibling</c>，不得混入 Run 的判定。
	/// </summary>
	[Permission("runtime:sibling")]
	public virtual void Sibling()
	{
	}
}

/// <summary>
/// 派生类：在 Run 的 override 上追加声明。
/// </summary>
public class ResolutionDerived : ResolutionBase
{
	/// <summary>
	/// Run 的 override：声明 <c>runtime:derived</c>。
	/// </summary>
	[Permission("runtime:derived")]
	public override void Run()
	{
	}
}

/// <summary>
/// 二层派生类：继续在 Run 的 override 上追加声明，验证整条 override 链参与并集。
/// </summary>
public class ResolutionGrandDerived : ResolutionDerived
{
	/// <summary>
	/// Run 的 override：声明 <c>runtime:grand</c>。
	/// </summary>
	[Permission("runtime:grand")]
	public override void Run()
	{
	}
}

/// <summary>
/// 遮蔽派生类：用 <c>new</c> 遮蔽非虚方法 Plain 并追加声明。
/// </summary>
public class ResolutionShadowDerived : ResolutionBase
{
	/// <summary>
	/// 以 <c>new</c> 遮蔽的 Plain：声明 <c>runtime:shadow</c>。
	/// </summary>
	[Permission("runtime:shadow")]
	public new void Plain()
	{
	}
}

/// <summary>
/// 仅追加类型级声明的派生类：不 override 任何方法。
/// </summary>
[Permission("runtime:type")]
public class ResolutionTypeLevelDerived : ResolutionBase
{
}
