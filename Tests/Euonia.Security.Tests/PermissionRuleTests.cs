using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Security.Tests.Fixtures;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 操作入口规则的两种载体：注册回调与自定义来源。
/// <para>
/// 二者与既有的 <see cref="IPermissionCodeSource"/> 是同一套语义（同一份注册期校验、同一套合并规则），
/// 因此本类既验证载体本身，也验证「多种载体可并存并取并集」。
/// </para>
/// </summary>
public class PermissionRuleTests
{
	private static readonly Assembly TestAssembly = typeof(PermissionRuleTests).Assembly;

	private static readonly Assembly FixturesAssembly = typeof(GuardedAsset).Assembly;

	#region 回调载体

	[Fact]
	public void Callback_Should_Declare_Rules_Inline()
	{
		var provider = Build(s => s.AddPermission(p => { p.Scan(FixturesAssembly); p.OnMethod(BusinessOperation.Execute, m => m.Name == "Run"); }));

		Assert.Equal("guarded:run", ResolveExecuteKey(provider, typeof(GuardedAsset)));
	}

	[Fact]
	public void Callback_Without_Rules_Should_Fail_At_Registration()
	{
		// 空回调几乎总是漏写：直接报错，并指明「说清楚意图」的写法
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(p => { }));

		Assert.Contains("NoModels", exception.Message);
		Assert.Contains("NoOperationCodes", exception.Message);
	}

	[Fact]
	public void Callbacks_From_Different_Modules_Should_Merge()
	{
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(TestAssembly); p.OnAttributeOrName(BusinessOperation.Execute, null, typeof(AssetApproveAttribute)); });
		services.AddPermission(p => { p.Scan(TestAssembly); p.OnAttributeOrName(BusinessOperation.Execute, null, typeof(AssetSecondApproveAttribute)); });

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Equal([BusinessOperation.Execute], source.AllOperations);
		Assert.Equal(["asset:approve"], source.CodesFor(typeof(ApproveOnlyAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void Callback_And_Custom_Source_Should_Merge()
	{
		// 两种载体并存：回调贴在自己的模块里，自定义来源交给框架无关的既有实现
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(FixturesAssembly); p.Source(new ConventionCodeSource()); });
		services.AddPermission(p => { p.Scan(TestAssembly); p.OnAttributeOrName(BusinessOperation.Read, null, typeof(AssetApproveAttribute)); });

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Contains("guarded:run", source.CodesFor(typeof(GuardedAsset), BusinessOperation.Execute));
		Assert.Contains("asset:read", source.CodesFor(typeof(ApprovableAsset), BusinessOperation.Read));
	}

	[Fact]
	public void Callback_Should_Not_Duplicate_Registrations()
	{
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(FixturesAssembly); p.OnMethod(BusinessOperation.Execute, m => m.Name == "Run"); });
		services.AddPermission(p => { p.Scan(FixturesAssembly); p.OnMethod(BusinessOperation.Execute, m => m.Name == "Run"); });

		Assert.Equal(1, services.Count(x => x.ServiceType == typeof(ScopeModelRegistry)));
		Assert.Equal(1, services.Count(x => x.ServiceType == typeof(IPermissionCodeSource)));
	}

	#endregion

	#region 合并后的「要求」面

	[Fact]
	public void Merged_Sources_Should_Expose_Requirements_From_Every_Source()
	{
		// 运行期判定要的是「要求」（含角色），不只是权限码：合并来源必须能回答它，
		// 否则强制点只能看到权限码、看不到角色。
		var services = new ServiceCollection();

		services.AddPermission(p => { p.Scan(FixturesAssembly); p.Source(new ConventionCodeSource()); });                                            // 只给码
		services.AddPermission(p => { p.Scan(TestAssembly); p.OnAttributeOrName(BusinessOperation.Read, null, typeof(AssetApproveAttribute)); });    // 能回答要求

		var source = Assert.IsAssignableFrom<IPermissionCodeSource>(
			services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>());

		// 只给码的来源（ConventionCodeSource 只实现 IPermissionCodeSource）：
		// 折算为「有码、无角色」——丢掉它会让闸门比来源本身更宽松
		Assert.Contains(
			source.RequirementsFor(typeof(GuardedAsset), BusinessOperation.Execute),
			requirement => requirement.Permission == "guarded:run" && requirement.Roles.Length == 0);

		// 能回答要求的来源：角色原样保留
		Assert.Contains(
			source.RequirementsFor(typeof(ApprovableAsset), BusinessOperation.Read),
			requirement => requirement.Permission == "asset:approve" && requirement.Roles.SequenceEqual(["auditor", "manager"]));
	}

	[Fact]
	public void Single_Source_Should_Also_Answer_Requirements()
	{
		// 只有一个来源（最常见的情形）时不必经过合并，同样要能回答要求
		var provider = Build(s => s.AddPermission(p => { p.Scan(FixturesAssembly); p.OnMethod(BusinessOperation.Execute, m => m.Name == "Run"); }));

		Assert.IsAssignableFrom<IPermissionCodeSource>(provider.GetRequiredService<IPermissionCodeSource>());
	}

	#endregion

	#region 辅助

	/// <summary>用一组配置构建容器。</summary>
	private static ServiceProvider Build(params Action<IServiceCollection>[] configure)
	{
		var services = new ServiceCollection();

		foreach (var action in configure)
		{
			action(services);
		}

		return services.BuildServiceProvider();
	}

	/// <summary>解析某资源在 execute 操作上应使用的策略键。</summary>
	private static string ResolveExecuteKey(IServiceProvider provider, Type resourceType)
	{
		var registry = provider.GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.TryGet(resourceType, out var registration));

		registration.TryResolve(BusinessOperation.Execute, out _, out var key);

		return key;
	}

	#endregion
}
