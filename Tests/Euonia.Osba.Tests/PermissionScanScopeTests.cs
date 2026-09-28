using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 权限扫描的口径：<b>与对象工厂查找工厂方法的口径同源</b>（<see cref="ObjectReflector.GetFactoryMethods"/>）。
/// </summary>
/// <remarks>
/// 这条一致性是刻意的，也带来一处<b>有意的行为收窄</b>：工厂只在「当前类型这一层没有任何候选」时才上溯基类
/// （见 <c>ObjectReflector.GetCandidateMethods</c>），因此被派生类型遮蔽的基类方法上的权限声明不再被收集——
/// 那些方法不会被工厂调用，为它们收集要求只会产生永远无法满足的闸门。
/// 本文件把这条收窄钉住，避免日后被当成缺陷「修」回去。
/// </remarks>
public class PermissionScanScopeTests
{
	[Fact]
	public void Requirement_Should_Come_From_The_Method_The_Factory_Invokes()
	{
		var provider = ObjectPermissionRequirementProvider.Instance;

		// 基类：自己的更新方法上声明了权限
		var fromBase = provider.RequirementsFor(typeof(BaseScopedEntity), BusinessOperation.Update);

		Assert.Equal(["base:update"], fromBase.Select(requirement => requirement.Permission));

		// 派生类：遮蔽了更新方法（不是 override），工厂会调用派生类那一个 ⇒ 基类的声明不再适用
		var fromDerived = provider.RequirementsFor(typeof(DerivedScopedEntity), BusinessOperation.Update);

		Assert.Empty(fromDerived);
	}

	[Fact]
	public void Derived_Type_Should_Still_Inherit_Requirements_When_It_Declares_No_Candidate()
	{
		// 派生类没有任何候选方法时（没有遮蔽），工厂会上溯基类 ⇒ 基类方法上的声明照旧生效
		var provider = ObjectPermissionRequirementProvider.Instance;

		var requirements = provider.RequirementsFor(typeof(PlainDerivedScopedEntity), BusinessOperation.Update);

		Assert.Equal(["base:update"], requirements.Select(requirement => requirement.Permission));
	}

	[Fact]
	public void Convention_Named_Method_Should_Be_An_Entry_Without_Any_Attribute()
	{
		// 约定名同样算入口（与工厂查找一致）：方法叫 UpdateAsync 即可，不必打特性
		var provider = ObjectPermissionRequirementProvider.Instance;

		var requirements = provider.RequirementsFor(typeof(ConventionNamedEntity), BusinessOperation.Update);

		Assert.Equal(["convention:update"], requirements.Select(requirement => requirement.Permission));
	}

	[Fact]
	public void Unknown_Operation_Should_Declare_Nothing()
	{
		// 本来源只认识规则表里的操作：自定义操作由宿主的实现回答
		Assert.Empty(ObjectPermissionRequirementProvider.Instance.RequirementsFor(typeof(BaseScopedEntity), "approve"));
	}
}

#region 受控资源

/// <summary>基类：更新方法上声明了权限要求。</summary>
public class BaseScopedEntity : EditableObject<BaseScopedEntity>
{
	[Permission("base:update")]
	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>遮蔽了基类的更新方法：工厂会调用本类型这一层的方法。</summary>
public class DerivedScopedEntity : BaseScopedEntity
{
	[FactoryUpdate]
	protected new async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>没有遮蔽任何方法的派生类型：工厂上溯基类。</summary>
public class PlainDerivedScopedEntity : BaseScopedEntity
{
}

/// <summary>不打特性、只按约定名成为入口的资源。</summary>
public class ConventionNamedEntity : EditableObject<ConventionNamedEntity>
{
	[Permission("convention:update")]
	protected async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

#endregion
