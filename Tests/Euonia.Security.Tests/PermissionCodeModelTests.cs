using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security.Properties;
using Nerosoft.Euonia.Security.Tests.BrokenFixtures;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 「权限码即声明」的端到端行为：任意码、零枚举、类型级与方法级隔离、Trim/去重、
/// 兼容来源与自定义来源的叠加、启动期违规声明校验，以及码与数据权限键的共存。
/// </summary>
public class PermissionCodeModelTests
{
	private static readonly Assembly TestAssembly = typeof(PermissionCodeModelTests).Assembly;

	private static readonly Assembly BrokenFixturesAssembly = typeof(EmptyCodeDeclaredAsset).Assembly;

	private static readonly MethodInfo ApproveMethod = typeof(CodeModelAsset).GetMethod(nameof(CodeModelAsset.Approve))!;

	private static readonly MethodInfo UpdateMethod = typeof(CodeModelAsset).GetMethod(nameof(CodeModelAsset.Update))!;

	private static readonly MethodInfo ArchiveMethod = typeof(CodeModelAsset).GetMethod(nameof(CodeModelAsset.Archive))!;

	private static readonly MethodInfo TrimmedMethod = typeof(CodeModelAsset).GetMethod(nameof(CodeModelAsset.Trimmed))!;

	#region 任意码 / 零枚举

	/// <summary>
	/// 任意字符串（含 <c>-</c>、<c>.</c>）都是合法权限码：只靠 <see cref="PermissionAttribute"/>
	/// 声明即可被发现，不需要任何规则，且授予后判定命中。
	/// </summary>
	[Fact]
	public void Any_Code_Should_Be_Discovered_From_Attribute_Without_Any_Rule()
	{
		var provider = Build(p => p.Scan(TestAssembly), "order:approve-2.0");

		var source = provider.GetRequiredService<IPermissionRequirementSource>();
		var requirements = source.RequirementsFor(typeof(CodeModelAsset), ApproveMethod, "anything");

		Assert.Contains(requirements, requirement => requirement.Permission == "order:approve-2.0");
		Assert.True(provider.GetRequiredService<IPermissionChecker>().IsGranted("order:approve-2.0"));
	}

	/// <summary>
	/// <c>AddPermission(p =&gt; p.Scan(asm))</c> 本身就是一份完整配置：注册期不抛异常，
	/// 且方法级码已经在要求来源里可见（零枚举的直接证明）。
	/// </summary>
	[Fact]
	public void Scan_Alone_Should_Be_A_Complete_Configuration()
	{
		var services = new ServiceCollection();
		services.AddSingleton(User("dev"));
		services.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver());

		Assert.Null(Record.Exception(() => services.AddPermission(p => p.Scan(TestAssembly))));

		var provider = services.BuildServiceProvider();

		var requirements = provider.GetRequiredService<IPermissionRequirementSource>()
		                           .RequirementsFor(typeof(CodeModelAsset), UpdateMethod, BusinessOperation.Update);

		Assert.Contains(requirements, requirement => requirement.Permission == "order:update");
	}

	/// <summary>
	/// 类型级声明对该类型的所有授权标识都生效：<c>method == null</c> 时对 3 个不同的操作都返回该码。
	/// </summary>
	[Fact]
	public void Type_Level_Code_Should_Apply_To_Every_Operation()
	{
		var provider = Build(p => p.Scan(TestAssembly));

		var source = provider.GetRequiredService<IPermissionRequirementSource>();

		foreach (var operation in new[] { BusinessOperation.Read, BusinessOperation.Create, "approve" })
		{
			var requirements = source.RequirementsFor(typeof(CodeModelAsset), null, operation);

			Assert.Contains(requirements, requirement => requirement.Permission == "asset:type-level");
		}
	}

	#endregion

	#region 方法级隔离 / Trim / 去重

	/// <summary>
	/// 方法级声明只在被查询的那一个方法上出现：update 不需要 archive 的码，archive 也不会带上 update 的码。
	/// </summary>
	[Fact]
	public void Method_Level_Code_Should_Not_Leak_To_Sibling_Methods()
	{
		var provider = Build(p => p.Scan(TestAssembly));

		var source = provider.GetRequiredService<IPermissionRequirementSource>();

		var forUpdate = source.RequirementsFor(typeof(CodeModelAsset), UpdateMethod, BusinessOperation.Update).Select(requirement => requirement.Permission).ToArray();
		var forArchive = source.RequirementsFor(typeof(CodeModelAsset), ArchiveMethod, "archive").Select(requirement => requirement.Permission).ToArray();

		Assert.Contains("order:update", forUpdate);
		Assert.DoesNotContain("order:archive", forUpdate);

		Assert.Contains("order:archive", forArchive);
		Assert.DoesNotContain("order:update", forArchive);
	}

	/// <summary>
	/// 权限码在读取时被裁剪（<c>"  order:trimmed  "</c> → <c>order:trimmed</c>）；
	/// 同一成员上重复声明同一个码则去重成一条（读取器层面，注册期会另行报错）。
	/// </summary>
	[Fact]
	public void Codes_Should_Be_Trimmed_And_De_Duplicated()
	{
		var provider = Build(p => p.Scan(TestAssembly));

		var source = provider.GetRequiredService<IPermissionRequirementSource>();

		var trimmed = source.RequirementsFor(typeof(CodeModelAsset), TrimmedMethod, "update")
		                    .Select(requirement => requirement.Permission)
		                    .ToArray();

		Assert.Contains("order:trimmed", trimmed);
		Assert.DoesNotContain("  order:trimmed  ", trimmed);

		var duplicateMethod = typeof(DuplicateCodeDeclaredAsset).GetMethod(nameof(DuplicateCodeDeclaredAsset.Approve))!;
		var deduplicated = PermissionRequirements.For(typeof(DuplicateCodeDeclaredAsset), duplicateMethod);

		Assert.Equal(["dup:code"], deduplicated.Select(requirement => requirement.Permission));
	}

	#endregion

	#region 来源叠加

	/// <summary>
	/// 按 <c>(类型, 操作)</c> 查询的兼容来源（<see cref="IPermissionCodeSource"/>）与内建声明取并集：
	/// 两者给出的码同时可见，重复项只留一条。
	/// </summary>
	[Fact]
	public void Legacy_Source_Should_Merge_With_Builtin_Declarations()
	{
		var services = new ServiceCollection();
		services.AddSingleton(User("dev"));
		services.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver());
		services.AddPermission(p => { p.Scan(TestAssembly); p.Source(new LegacyExtraCodeSource()); });

		var provider = services.BuildServiceProvider();

		var codes = provider.GetRequiredService<IPermissionRequirementSource>()
		                    .RequirementsFor(typeof(CodeModelAsset), UpdateMethod, BusinessOperation.Update)
		                    .Select(requirement => requirement.Permission)
		                    .ToArray();

		Assert.Contains("asset:type-level", codes);
		Assert.Contains("order:update", codes);
		Assert.Contains("legacy:extra", codes);
	}

	/// <summary>
	/// 自定义要求来源是<b>增量</b>的：与内建声明取并集，不覆盖也不被覆盖。
	/// </summary>
	[Fact]
	public void Custom_Requirement_Source_Should_Be_Additive()
	{
		var services = new ServiceCollection();
		services.AddSingleton(User("dev"));
		services.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver());
		services.AddPermission(p => { p.Scan(TestAssembly); p.Requirements(new ExtraRequirementSource()); });

		var codes = services.BuildServiceProvider()
		                    .GetRequiredService<IPermissionRequirementSource>()
		                    .RequirementsFor(typeof(CodeModelAsset), UpdateMethod, BusinessOperation.Update)
		                    .Select(requirement => requirement.Permission)
		                    .ToArray();

		Assert.Contains("asset:type-level", codes);
		Assert.Contains("order:update", codes);
		Assert.Contains("custom:extra", codes);
	}

	#endregion

	#region 启动期校验

	/// <summary>
	/// 空权限码应在注册期抛 <see cref="InvalidOperationException"/>，并点名出错的成员。
	/// </summary>
	[Fact]
	public void Empty_Code_Should_Fail_At_Registration()
	{
		var exception = Assert.Throws<InvalidOperationException>(
			() => new ServiceCollection().AddPermission(p => p.Scan(BrokenFixturesAssembly)));

		var member = $"{typeof(EmptyCodeDeclaredAsset).FullName}.{nameof(EmptyCodeDeclaredAsset.Approve)}";

		// 断言具体诊断（资源模板 + 出错成员），而不是只看类型名
		Assert.Contains(string.Format(Resources.IDS_PERMISSION_CODE_EMPTY, member), exception.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// 保留前缀 <c>@</c> 应在注册期被拒绝。
	/// </summary>
	[Fact]
	public void Reserved_Prefix_Should_Fail_At_Registration()
	{
		var exception = Assert.Throws<InvalidOperationException>(
			() => new ServiceCollection().AddPermission(p => p.Scan(BrokenFixturesAssembly)));

		// 断言具体诊断：码与框架保留前缀都由资源模板给出
		Assert.Contains(
			string.Format(Resources.IDS_SCOPE_CODE_RESERVED_PREFIX, "@reserved:code", ScopeKeys.Prefix),
			exception.Message,
			StringComparison.Ordinal);
	}

	/// <summary>
	/// 同一成员上重复声明同一个权限码应在注册期被拒绝。
	/// </summary>
	[Fact]
	public void Duplicate_Code_On_Same_Member_Should_Fail_At_Registration()
	{
		var exception = Assert.Throws<InvalidOperationException>(
			() => new ServiceCollection().AddPermission(p => p.Scan(BrokenFixturesAssembly)));

		var member = $"{typeof(DuplicateCodeDeclaredAsset).FullName}.{nameof(DuplicateCodeDeclaredAsset.Approve)}";

		// 断言具体诊断（资源模板 + 成员 + 重复的码），而不是只看类型名
		Assert.Contains(string.Format(Resources.IDS_PERMISSION_CODE_DUPLICATE, member, "dup:code"), exception.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// 一次报全：三条违规诊断逐行拼接在同一条异常消息里，而不是修一条再跑一次。
	/// </summary>
	[Fact]
	public void Violations_Should_Be_Reported_Together()
	{
		var exception = Assert.Throws<InvalidOperationException>(
			() => new ServiceCollection().AddPermission(p => p.Scan(BrokenFixturesAssembly)));

		var lines = exception.Message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

		Assert.Equal(3, lines.Length);
		Assert.Contains(lines, line => line.Contains(nameof(EmptyCodeDeclaredAsset), StringComparison.Ordinal));
		Assert.Contains(lines, line => line.Contains("@reserved:code", StringComparison.Ordinal));
		Assert.Contains(lines, line => line.Contains("dup:code", StringComparison.Ordinal));
	}

	/// <summary>
	/// <see cref="PermissionOptions.NoOperationCodes"/> 是空操作：既不阻止方法级码生效，
	/// 又仍算一次显式配置（「只调用它、什么都没写」不再被判为空配置）。
	/// </summary>
	[Fact]
	public void NoOperationCodes_Should_Be_A_Noop_But_Still_Count_As_Configuration()
	{
		var onlyAssertion = new ServiceCollection();

		Assert.Null(Record.Exception(() => onlyAssertion.AddPermission(p => p.NoOperationCodes())));

		var provider = Build(p => { p.Scan(TestAssembly); p.NoOperationCodes(); });

		var requirements = provider.GetRequiredService<IPermissionRequirementSource>()
		                           .RequirementsFor(typeof(CodeModelAsset), UpdateMethod, BusinessOperation.Update);

		Assert.Contains(requirements, requirement => requirement.Permission == "order:update");
	}

	#endregion

	#region 与数据权限键共存

	/// <summary>
	/// 权限码与授权标识共用字符串命名空间但互不冲突：
	/// <c>ForOperation("update", …, "order:update")</c> 与 <c>For("order:archive")</c> 可并存，
	/// 且码的发现仍按声明走，不依赖这套寻址声明。
	/// </summary>
	[Fact]
	public void Code_And_Operation_Declarations_Should_Coexist_Without_Runtime_Conflict()
	{
		var provider = Build(p => p.Scan(TestAssembly));

		var registry = provider.GetRequiredService<ScopeModelRegistry>();

		Assert.True(registry.TryGet(typeof(CoexistingScopeAsset), out var registration));

		Assert.True(registration.TryResolve(BusinessOperation.Update, out _, out var operationKey));
		Assert.Equal("order:update", operationKey);

		Assert.True(registration.TryResolve("order:archive", out _, out var codeKey));
		Assert.Equal("order:archive", codeKey);

		var requirements = provider.GetRequiredService<IPermissionRequirementSource>()
		                           .RequirementsFor(typeof(CoexistingScopeAsset), typeof(CoexistingScopeAsset).GetMethod(nameof(CoexistingScopeAsset.Archive))!, "archive");

		Assert.Contains(requirements, requirement => requirement.Permission == "order:archive");
	}

	#endregion

	#region 辅助

	private static ServiceProvider Build(Action<PermissionOptions> configure, params string[] codes)
	{
		var services = new ServiceCollection();
		services.AddSingleton(User("dev"));
		services.AddSingleton<IScopeSubjectResolver>(new FixedSubjectResolver(codes));

		services.AddPermission(configure);

		return services.BuildServiceProvider().Warm();
	}

	private static UserPrincipal User(string userId)
	{
		var identity = new ClaimsIdentity(
			[new Claim(ClaimTypes.Name, userId)],
			"Bearer",
			ClaimTypes.Name,
			ClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	#endregion
}

/// <summary>
/// 带类型级声明与多个方法级声明的资源：用于任意码、隔离、Trim 用例。
/// </summary>
[Permission("asset:type-level")]
public sealed class CodeModelAsset
{
	/// <summary>
	/// 任意码入口（含 <c>-</c> 与 <c>.</c>）。
	/// </summary>
	[Permission("order:approve-2.0")]
	public void Approve() { }

	/// <summary>
	/// 更新入口：只声明 <c>order:update</c>。
	/// </summary>
	[Permission("order:update")]
	public void Update() { }

	/// <summary>
	/// 归档入口：只声明 <c>order:archive</c>。
	/// </summary>
	[Permission("order:archive")]
	public void Archive() { }

	/// <summary>
	/// 权限码带首尾空白的入口。
	/// </summary>
	[Permission("  order:trimmed  ")]
	public void Trimmed() { }
}

/// <summary>
/// 同时声明「按操作寻址、授予键为权限码」与「按权限码寻址」的资源：用于验证两种声明并存。
/// </summary>
public sealed class CoexistingScopeAsset
{
	/// <summary>
	/// 负责人标识，对应 <c>owner</c> 维度。
	/// </summary>
	public string OwnerId { get; set; } = null!;

	/// <summary>
	/// 归档入口：声明 <c>order:archive</c> 权限码。
	/// </summary>
	[Permission("order:archive")]
	public void Archive() { }
}

/// <summary>
/// <see cref="CoexistingScopeAsset"/> 的权限模型：一个操作键映射到权限码，另一个直接按权限码声明。
/// </summary>
public sealed class CoexistingScopeAssetModel : ScopeModel<CoexistingScopeAsset>
{
	/// <inheritdoc />
	public override void Define(ScopeModelBuilder<CoexistingScopeAsset> builder)
	{
		builder.Map(ScopeDimensions.Owner, asset => asset.OwnerId);
	}

	/// <inheritdoc />
	public override ScopePolicy<CoexistingScopeAsset> Policy => ScopePolicy<CoexistingScopeAsset>.Grant(ScopeDimensions.Owner);

	/// <inheritdoc />
	public override void Declare(ScopePolicySet<CoexistingScopeAsset> policies)
	{
		// 授权标识 "update" 的授予键写成权限码 "order:update"；另一条直接按码声明 —— 两者共存不冲突
		policies.ForOperation(BusinessOperation.Update, ScopePolicy<CoexistingScopeAsset>.Grant(ScopeDimensions.Owner), "order:update");
		policies.For("order:archive", ScopePolicy<CoexistingScopeAsset>.Grant(ScopeDimensions.Owner));
	}
}

/// <summary>
/// 只在 update 操作上补充 <c>legacy:extra</c> 的兼容来源（按 <c>(类型, 操作)</c> 查询）。
/// </summary>
internal sealed class LegacyExtraCodeSource : IPermissionCodeSource
{
	/// <inheritdoc />
	public IReadOnlyList<string> AllOperations => [BusinessOperation.Update];

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation)
	{
		return type == typeof(CodeModelAsset) && operation == BusinessOperation.Update ? ["legacy:extra"] : [];
	}
}

/// <summary>
/// 对任意类型/方法都补充 <c>custom:extra</c> 的新契约来源。
/// </summary>
internal sealed class ExtraRequirementSource : IPermissionRequirementSource
{
	/// <inheritdoc />
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, MethodInfo? method, string operation)
	{
		return type == typeof(CodeModelAsset) ? [new PermissionAttribute("custom:extra")] : [];
	}
}
