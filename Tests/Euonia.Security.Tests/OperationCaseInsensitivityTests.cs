using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Security.Tests.Fixtures;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 操作名与权限码<b>忽略大小写</b>的口径。
/// </summary>
/// <remarks>
/// 运行期的授权门永远用 <see cref="BusinessOperation"/> 的小写常量查询（见 <c>ObjectAuthorization</c>），
/// 而声明端的配置键、回调参数可以写成任意大小写。两侧一旦按序数比较，
/// 声明端写 <c>"Execute"</c> 就会让规则表查不到 → <c>RequirementsFor</c> 返回空 →
/// <c>ObjectAuthorization</c> 认为「没有任何权限要求」而<b>静默放行</b>（fail-open）。
/// <para>
/// 口径依据：<see cref="CompositeCodeSource.CodesFor"/> 早已按 <c>OrdinalIgnoreCase</c> 去重，
/// 并在文档里写明理由——两个模块分别声明 <c>repo:delete</c> 与 <c>Repo:Delete</c> 必须收敛成同一个码，
/// 否则会让 <see cref="ScopeKeyResolver"/> 的「同一操作最多一个有策略的码」校验误报歧义。
/// </para>
/// </remarks>
public class OperationCaseInsensitivityTests
{
	private static readonly Assembly FixturesAssembly = typeof(GuardedAsset).Assembly;

	private static readonly Assembly TestAssembly = typeof(OperationCaseInsensitivityTests).Assembly;

	[Fact]
	public void Operation_Declared_With_Different_Casing_Should_Still_Serve_The_Vocabulary_Constant()
	{
		// 声明端写成大写，运行时用 BusinessOperation.Execute（"execute"）查询
		var services = new ServiceCollection();
		services.AddPermission(o => o.OnMethodName("Execute", "Run"), FixturesAssembly);

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Equal(["guarded:run"], source.CodesFor(typeof(GuardedAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void Operations_Declared_With_Different_Casing_Should_Collapse_Into_One_Vocabulary_Entry()
	{
		var services = new ServiceCollection();
		services.AddPermission(o => o.OnMethodName("Execute", "Run"), FixturesAssembly);
		services.AddPermission(o => o.OnMethodName("EXECUTE", "Run"), FixturesAssembly);

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		// 收敛成一条即可；AllOperations 保留首次声明的写法，后续一律按忽略大小写匹配
		var operation = Assert.Single(source.AllOperations);
		Assert.Equal(BusinessOperation.Execute.ToLowerInvariant(), operation.ToLowerInvariant());
	}

	[Fact]
	public void Permission_Codes_Should_Be_De_Duplicated_Across_Different_Casing()
	{
		// 同一操作上两次声明只差大小写的码，必须收敛成一个：
		// 否则下游 ScopeKeyResolver 的「同一操作最多一个有策略的码」会误报歧义。
		var services = new ServiceCollection();
		services.AddPermission(o => o.OnMethodName(BusinessOperation.Execute, "Run", "Execute"), TestAssembly);

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Equal(["dual:run"], source.CodesFor(typeof(DualCasedAsset), BusinessOperation.Execute));
	}
}

/// <summary>
/// 同一操作上以不同大小写重复声明权限码的资源（无 ScopeModel，故不参与死策略校验）。
/// </summary>
public sealed class DualCasedAsset
{
	[Permission("dual:run")]
	public void Run() { }

	[Permission("Dual:Run")]
	public void Execute() { }
}
