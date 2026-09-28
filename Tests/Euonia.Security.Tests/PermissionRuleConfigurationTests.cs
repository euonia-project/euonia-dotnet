using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Security.Tests.Ambiguity.One;
using Nerosoft.Euonia.Security.Tests.Fixtures;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 操作入口规则的两种「配置化」载体：Action 回调与配置节。
/// <para>
/// 它们与既有的 <see cref="IPermissionCodeSource"/> 是同一套语义（同一份注册期校验、同一套合并规则），
/// 因此本类既验证新载体本身，也验证「三种载体可并存并取并集」。
/// </para>
/// </summary>
public class PermissionRuleConfigurationTests
{
	private static readonly Assembly TestAssembly = typeof(PermissionRuleConfigurationTests).Assembly;

	private static readonly Assembly FixturesAssembly = typeof(GuardedAsset).Assembly;

	#region 回调载体

	[Fact]
	public void Callback_Should_Declare_Rules_Inline()
	{
		var provider = Build(s => s.AddPermission(o => o.OnMethodName(BusinessOperation.Execute, "Run"), FixturesAssembly));

		Assert.Equal("guarded:run", ResolveExecuteKey(provider, typeof(GuardedAsset)));
	}

	[Fact]
	public void Callback_Without_Rules_Should_Fail_At_Registration()
	{
		// 空回调几乎总是漏写：直接报错，并指明两种「说清楚意图」的写法
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(o => { }));

		Assert.Contains("AddPermissionModels", exception.Message);
		Assert.Contains("EmptyCodeSource", exception.Message);
	}

	[Fact]
	public void Callbacks_From_Different_Modules_Should_Merge()
	{
		var services = new ServiceCollection();

		services.AddPermission(o => o.OnAttribute(BusinessOperation.Execute, typeof(AssetApproveAttribute)), TestAssembly);
		services.AddPermission(o => o.OnAttribute(BusinessOperation.Execute, typeof(AssetSecondApproveAttribute)), TestAssembly);

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Equal([BusinessOperation.Execute], source.AllOperations);
		Assert.Equal(["asset:approve"], source.CodesFor(typeof(ApproveOnlyAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void Callback_And_Custom_Source_Should_Merge()
	{
		// 两种载体并存：回调贴在自己的模块里，自定义来源交给框架无关的既有实现
		var services = new ServiceCollection();

		services.AddPermission(new ConventionCodeSource(), FixturesAssembly);
		services.AddPermission(o => o.OnAttribute(BusinessOperation.Read, typeof(AssetApproveAttribute)), TestAssembly);

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Contains("guarded:run", source.CodesFor(typeof(GuardedAsset), BusinessOperation.Execute));
		Assert.Contains("asset:read", source.CodesFor(typeof(ApprovableAsset), BusinessOperation.Read));
	}

	[Fact]
	public void Callback_Should_Not_Duplicate_Registrations()
	{
		var services = new ServiceCollection();

		services.AddPermission(o => o.OnMethodName(BusinessOperation.Execute, "Run"), FixturesAssembly);
		services.AddPermission(o => o.OnMethodName(BusinessOperation.Execute, "Run"), FixturesAssembly);

		Assert.Equal(1, services.Count(x => x.ServiceType == typeof(ScopeModelRegistry)));
		Assert.Equal(1, services.Count(x => x.ServiceType == typeof(IPermissionCodeSource)));
	}

	#endregion

	#region 配置节载体

	[Fact]
	public void Configuration_Should_Declare_Rules_By_Method_Name()
	{
		var provider = Build(s => s.AddPermission(Rules(new[]
		{
			("Operations:execute:Names:0", "Run")
		}), FixturesAssembly));

		Assert.Equal("guarded:run", ResolveExecuteKey(provider, typeof(GuardedAsset)));
	}

	[Fact]
	public void Configuration_Should_Declare_Rules_By_Attribute_Type()
	{
		var provider = Build(s => s.AddPermission(Rules(new[]
		{
			("Operations:execute:Attributes:0", typeof(AssetApproveAttribute).FullName)
		}), TestAssembly));

		var source = provider.GetRequiredService<IPermissionCodeSource>();

		// 类型级码（asset:read）始终参与，方法级码由规则命中而来
		Assert.Contains("asset:approve", source.CodesFor(typeof(ApprovableAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void Configuration_Should_Union_Attributes_And_Names()
	{
		// 二者并存 = 「命中其一即为入口」。用两条**不同的码**来证明：只给一半时另一半确实不在，
		// 给全时两条都在（UnionProbeAsset 的 Purge 只打特性、Probe 只按命名）。
		var attributesOnly = Codes(Rules([("Operations:execute:Attributes:0", typeof(UnionProbeAttribute).FullName)]));
		var namesOnly = Codes(Rules([("Operations:execute:Names:0", nameof(UnionProbeAsset.Probe))]));
		var both = Codes(Rules(new[]
		{
			("Operations:execute:Attributes:0", typeof(UnionProbeAttribute).FullName),
			("Operations:execute:Names:0", nameof(UnionProbeAsset.Probe))
		}));

		Assert.Equal(["probe:by-attr"], attributesOnly);
		Assert.Equal(["probe:by-name"], namesOnly);
		Assert.Equal(["probe:by-attr", "probe:by-name"], both);
	}

	[Fact]
	public void Configuration_Should_Match_Derived_Attribute_Instances()
	{
		// 规则里写基类型，派生特性实例同样命中（特性匹配的通常语义）——这条由配置载体暴露给使用者，故钉住
		var provider = Build(s => s.AddPermission(Rules(new[]
		{
			("Operations:execute:Attributes:0", typeof(EntryMarkAttribute).FullName)
		}), TestAssembly));

		var source = provider.GetRequiredService<IPermissionCodeSource>();

		Assert.Equal(["probe:by-derived"], source.CodesFor(typeof(UnionProbeAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void Configuration_And_Callback_Should_Merge()
	{
		var services = new ServiceCollection();

		services.AddPermission(Rules([("Operations:execute:Names:0", "Run")]), FixturesAssembly);
		services.AddPermission(o => o.OnAttribute(BusinessOperation.Read, typeof(AssetApproveAttribute)), TestAssembly);

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Contains("guarded:run", source.CodesFor(typeof(GuardedAsset), BusinessOperation.Execute));
		Assert.Contains("asset:read", source.CodesFor(typeof(ApprovableAsset), BusinessOperation.Read));
	}

	#endregion

	#region 配置的错误形态（全部在注册期暴露）

	[Fact]
	public void Configuration_Should_Reject_Scalar_In_Place_Of_Array()
	{
		// 标量写法读不到任何子项：若静默跳过，规则会比作者以为的更窄——必须在注册期报错
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(Rules(new[]
		{
			("Operations:execute:Attributes", typeof(UnionProbeAttribute).FullName),
			("Operations:execute:Names:0", nameof(UnionProbeAsset.Probe))
		})));

		Assert.Contains("Attributes", exception.Message);
		Assert.Contains("数组", exception.Message);
	}

	[Fact]
	public void Configuration_Should_Reject_Blank_Array_Element()
	{
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(Rules(new[]
		{
			("Operations:execute:Attributes:0", " "),
			("Operations:execute:Names:0", nameof(UnionProbeAsset.Probe))
		})));

		Assert.Contains("非空字符串", exception.Message);
		Assert.Contains("Attributes:0", exception.Message);
	}

	[Fact]
	public void Configuration_Should_Reject_Unknown_Operation_Key()
	{
		// 键名拼错（Name 少了 s）是最容易发生、也最难察觉的一类：整条规则会静默消失
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(Rules(new[]
		{
			("Operations:execute:Names:0", "Run"),
			("Operations:execute:Name:0", "Run")
		})));

		Assert.Contains("未知的节点", exception.Message);
		Assert.Contains("Names", exception.Message);
	}

	[Fact]
	public void Configuration_Should_Reject_Case_Only_Duplicate_Operations()
	{
		// JSON 这类配置源大小写敏感，`read` 与 `Read` 会变成两个操作：预期的那个静默失去规则。
		// 内存配置源本身大小写不敏感（放不下这两个键），故这里用大小写敏感的最小实现。
		var section = Section(
			"Permission",
			Section(
				"Permission:Operations",
				Section("Permission:Operations:read", Section("Permission:Operations:read:Names", Leaf("Permission:Operations:read:Names:0", "Fetch"))),
				Section("Permission:Operations:Read", Section("Permission:Operations:Read:Names", Leaf("Permission:Operations:Read:Names:0", "Get")))));

		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(section));

		Assert.Contains("大小写", exception.Message);
	}

	[Fact]
	public void Configuration_Should_Reject_Generic_Attribute_Type()
	{
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(Rules(new[]
		{
			("Operations:execute:Attributes:0", typeof(GenericMarkAttribute<>).FullName)
		}), TestAssembly));

		Assert.Contains("泛型", exception.Message);
	}

	[Fact]
	public void Configuration_Should_Resolve_AssemblyQualified_Type()
	{
		// 程序集限定名是显式逃生舱：即使该程序集没有传入扫描范围也能解析
		var provider = Build(s => s.AddPermission(Rules(new[]
		{
			("Operations:execute:Attributes:0", typeof(AssetApproveAttribute).AssemblyQualifiedName)
		})));

		var source = provider.GetRequiredService<IPermissionCodeSource>();

		Assert.Contains("asset:approve", source.CodesFor(typeof(ApprovableAsset), BusinessOperation.Execute));
	}

	[Fact]
	public void Configuration_Should_Report_Malformed_Qualified_Type_As_Registration_Error()
	{
		// 限定名写坏时 Type.GetType 抛的是框架异常：必须转成与其他错误同一口径的注册期报错
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(Rules(new[]
		{
			("Operations:execute:Attributes:0", "No.Such.TypeName, No.Such.Assembly")
		})));

		Assert.Contains("No.Such.TypeName", exception.Message);
		Assert.Contains("无法解析", exception.Message);
	}

	[Fact]
	public void Configuration_Should_Resolve_Types_From_An_Earlier_Call()
	{
		// 类型名在「已累积 + 本次」的扫描范围里解析：先加程序集、后注册配置节也应可用
		var services = new ServiceCollection();

		services.AddPermissionModels(TestAssembly);
		services.AddPermission(Rules([("Operations:execute:Attributes:0", typeof(UnionProbeAttribute).FullName)]));

		var source = services.BuildServiceProvider().GetRequiredService<IPermissionCodeSource>();

		Assert.Equal(["probe:by-attr"], Codes(source, typeof(UnionProbeAsset)));
	}

	[Fact]
	public void Failed_Rules_Should_Leave_The_Container_Untouched()
	{
		// 规则不合法时不应留下半套注册：既是实现约束（先构造规则再动容器），也是排障体验
		var services = new ServiceCollection();

		Assert.Throws<InvalidOperationException>(() => services.AddPermission(o => { }));
		Assert.Throws<InvalidOperationException>(() => services.AddPermission(Rules([])));

		// 累积状态（internal 的 PermissionModelSetup）同样不该留下：它一旦被创建，就说明「先构造规则」被破坏了
		Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ScopeModelRegistry)
		                                           || descriptor.ServiceType == typeof(IPermissionCodeSource)
		                                           || descriptor.ServiceType.Name == "PermissionModelSetup");

		// 同一容器随后仍可正常注册
		services.AddPermission(o => o.OnMethodName(BusinessOperation.Execute, "Run"), FixturesAssembly);

		Assert.Equal("guarded:run", ResolveExecuteKey(services.BuildServiceProvider(), typeof(GuardedAsset)));
	}

	[Fact]
	public void Configuration_Should_Reject_Root_Node()
	{
		// 传根配置而不是配置节：报错必须说清该怎么传
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(Rules([])));

		Assert.Contains("Operations", exception.Message);
		Assert.Contains("GetSection", exception.Message);
	}

	[Fact]
	public void Configuration_Should_Reject_Operation_Without_Rules()
	{
		// 操作节点是标量（或没有子节点）：既读不到特性也读不到方法名——等于没有规则
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(Rules(new[]
		{
			("Operations:approve", "Approve")
		})));

		Assert.Contains("Operations:approve", exception.Message);
		Assert.Contains("Names", exception.Message);
	}

	[Fact]
	public void Configuration_Should_Reject_Unresolvable_Attribute_Type()
	{
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(Rules(new[]
		{
			("Operations:execute:Attributes:0", "No.Such.Namespace.MissingAttribute")
		}), TestAssembly));

		Assert.Contains("No.Such.Namespace.MissingAttribute", exception.Message);
	}

	[Fact]
	public void Configuration_Should_Reject_Ambiguous_Attribute_Type()
	{
		// 短名在两个命名空间里都有：必须报歧义，并要求写完整类型名——规则写错不能到运行期才显现
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(Rules(new[]
		{
			("Operations:execute:Attributes:0", nameof(SharedEntryAttribute))
		}), TestAssembly));

		Assert.Contains(nameof(SharedEntryAttribute), exception.Message);
		Assert.Contains("完整类型名", exception.Message);
	}

	[Fact]
	public void Configuration_Should_Reject_Non_Attribute_Type()
	{
		var exception = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddPermission(Rules(new[]
		{
			("Operations:execute:Attributes:0", typeof(GuardedAsset).FullName)
		}), FixturesAssembly));

		Assert.Contains("不是特性", exception.Message);
	}

	#endregion

	#region 辅助

	/// <summary>由键值对构造一个配置节（等价于 appsettings 里的一组规则）。</summary>
	private static IConfigurationSection Rules(IEnumerable<(string Key, string Value)> entries)
	{
		return new ConfigurationBuilder()
		       .AddInMemoryCollection(entries.Select(entry => new KeyValuePair<string, string>("Permission:" + entry.Key, entry.Value)))
		       .Build()
		       .GetSection("Permission");
	}

	/// <summary>用一组规则注册后，取某资源在 execute 上的权限码。</summary>
	private static string[] Codes(IConfigurationSection rules, Type resourceType = null)
	{
		var provider = Build(s => s.AddPermission(rules, TestAssembly));

		return Codes(provider.GetRequiredService<IPermissionCodeSource>(), resourceType ?? typeof(UnionProbeAsset));
	}

	private static string[] Codes(IPermissionCodeSource source, Type resourceType)
	{
		return [.. source.CodesFor(resourceType, BusinessOperation.Execute).OrderBy(code => code, StringComparer.Ordinal)];
	}

	/// <summary>构造一个大小写敏感的配置节（<c>Key</c> 取路径最后一段）。</summary>
	private static IConfigurationSection Section(string path, params IConfigurationSection[] children)
	{
		return new CaseSensitiveSection(path, null, children);
	}

	/// <summary>构造一个叶子节点：没有子节点，值是标量。</summary>
	private static IConfigurationSection Leaf(string path, string value)
	{
		return new CaseSensitiveSection(path, value, []);
	}

	/// <summary>
	/// 最小的<b>大小写敏感</b>配置节实现。
	/// </summary>
	/// <remarks>
	/// 内存配置源把键都装进大小写不敏感的字典，表达不了「<c>read</c> 与 <c>Read</c> 并存」这种真实配置源
	/// （JSON、环境变量的大小写语义各自不同）里可能出现的写法，故这里手写一个。
	/// </remarks>
	private sealed class CaseSensitiveSection(string path, string value, IReadOnlyList<IConfigurationSection> children) : IConfigurationSection
	{
		public string Key { get; } = path[(path.LastIndexOf(':') + 1)..];

		public string Path => path;

		public string Value { get; set; } = value;

		public string this[string key]
		{
			get => GetSection(key).Value;
			set => Value = value;
		}

		public IConfigurationSection GetSection(string key)
		{
			return children.FirstOrDefault(child => child.Key == key) ?? new CaseSensitiveSection($"{path}:{key}", null, []);
		}

		public IEnumerable<IConfigurationSection> GetChildren()
		{
			return children;
		}

		public IChangeToken GetReloadToken()
		{
			// 永不触发的变更令牌：配置只在注册期读一次，本测试也不涉及重载
			return new CancellationChangeToken(CancellationToken.None);
		}
	}

	private static string ResolveExecuteKey(IServiceProvider provider, Type resourceType)
	{
		var registry = provider.GetRequiredService<ScopeModelRegistry>();
		var codeSource = provider.GetRequiredService<IPermissionCodeSource>();

		Assert.True(registry.TryGet(resourceType, out var registration));

		return ScopeKeyResolver.Resolve(registration, resourceType, BusinessOperation.Execute, codeSource);
	}

	private static ServiceProvider Build(params Action<IServiceCollection>[] configure)
	{
		var services = new ServiceCollection();
		services.AddSingleton(User());

		foreach (var action in configure)
		{
			action(services);
		}

		return services.BuildServiceProvider();
	}

	private static UserPrincipal User(string userId = "dev")
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
