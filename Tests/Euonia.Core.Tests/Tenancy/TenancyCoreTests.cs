using Nerosoft.Euonia.Execution;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Core.Tests.Tenancy;

/// <summary>
/// 验证执行上下文、租户作用域、写入守卫、解析链与键转换的核心行为。
/// </summary>
public class TenancyCoreTests
{
	private static TenantInfo NewTenant(string id, string? identifier = null, TenantStatus status = TenantStatus.Active)
	{
		return new TenantInfo { Id = id, Identifier = identifier, Name = id, Status = status };
	}

	/// <summary>
	/// 执行上下文按后进先出恢复，嵌套作用域互不覆盖。
	/// </summary>
	[Fact]
	public void AmbientExecutionContext_ShouldRestoreInLifoOrder()
	{
		AmbientExecutionContext.Reset();

		using (AmbientExecutionContext.Push(context => context.TenantId = "a"))
		{
			Assert.Equal("a", AmbientExecutionContext.Current!.TenantId);

			using (AmbientExecutionContext.Push(context => context.TenantId = "b"))
			{
				Assert.Equal("b", AmbientExecutionContext.Current!.TenantId);
			}

			Assert.Equal("a", AmbientExecutionContext.Current!.TenantId);
		}

		Assert.Null(AmbientExecutionContext.Current);
	}

	/// <summary>
	/// 乱序释放不会覆盖当前快照。
	/// </summary>
	[Fact]
	public void AmbientExecutionContext_ShouldIgnoreOutOfOrderDispose()
	{
		AmbientExecutionContext.Reset();

		var outer = AmbientExecutionContext.Push(context => context.TenantId = "a");
		var inner = AmbientExecutionContext.Push(context => context.TenantId = "b");

		outer.Dispose();

		Assert.Equal("b", AmbientExecutionContext.Current!.TenantId);
		inner.Dispose();
		Assert.Equal("a", AmbientExecutionContext.Current!.TenantId);
		outer.Dispose();
		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// 租户作用域释放后恢复进入前的租户。
	/// </summary>
	[Fact]
	public void TenantScopeFactory_ShouldRestorePreviousTenant()
	{
		AmbientExecutionContext.Reset();
		var factory = new TenantScopeFactory();

		using (factory.Begin(NewTenant("a")))
		{
			Assert.Equal("a", AmbientExecutionContext.Current!.TenantId);

			using (factory.BeginCrossTenant("support", "b"))
			{
				Assert.Equal("b", AmbientExecutionContext.Current!.TenantId);
				Assert.True(AmbientExecutionContext.Current!.IsCrossTenant);
				Assert.Equal("b", AmbientExecutionContext.Current!.TargetTenantId);
			}

			Assert.Equal("a", AmbientExecutionContext.Current!.TenantId);
			Assert.False(AmbientExecutionContext.Current!.IsCrossTenant);
		}

		Assert.Null(AmbientExecutionContext.Current);
	}

	/// <summary>
	/// 同一作用域内切换到另一个租户应被拒绝。
	/// </summary>
	[Fact]
	public void TenantScopeFactory_ShouldRejectSwitchingTenant()
	{
		AmbientExecutionContext.Reset();
		var factory = new TenantScopeFactory();

		using (factory.Begin(NewTenant("a")))
		{
			Assert.Throws<InvalidOperationException>(() => factory.Begin(NewTenant("b")));
		}

		AmbientExecutionContext.Reset();
	}

	/// <summary>
	/// 新增实体缺少租户时自动写入当前租户。
	/// </summary>
	[Fact]
	public void TenantEntryGuard_ShouldAssignScopeTenantOnAdd()
	{
		var context = new TenantEntryContext(
			TenantEntryOperation.Added,
			null,
			null,
			"a",
			null,
			TenantStatus.Active,
			false,
			false);

		Assert.Equal("a", TenantEntryGuard.Validate(context));
	}

	/// <summary>
	/// 全租户跨租户作用域只读，写入应被拒绝。
	/// </summary>
	[Fact]
	public void TenantEntryGuard_ShouldRejectWritesInAllTenantScope()
	{
		var context = new TenantEntryContext(
			TenantEntryOperation.Added,
			null,
			null,
			null,
			null,
			TenantStatus.Active,
			true,
			false);

		Assert.Throws<CrossTenantOperationException>(() => TenantEntryGuard.Validate(context));
	}

	/// <summary>
	/// 目标租户跨租户作用域允许写入目标租户。
	/// </summary>
	[Fact]
	public void TenantEntryGuard_ShouldAllowTargetTenantWrite()
	{
		var context = new TenantEntryContext(
			TenantEntryOperation.Added,
			null,
			null,
			"b",
			"b",
			TenantStatus.Active,
			true,
			false);

		Assert.Equal("b", TenantEntryGuard.Validate(context));
	}

	/// <summary>
	/// 修改实体租户应被拒绝。
	/// </summary>
	[Fact]
	public void TenantEntryGuard_ShouldRejectTenantMigration()
	{
		var context = new TenantEntryContext(
			TenantEntryOperation.Modified,
			"b",
			"a",
			"a",
			null,
			TenantStatus.Active,
			false,
			false);

		Assert.Throws<CrossTenantOperationException>(() => TenantEntryGuard.Validate(context));
	}

	/// <summary>
	/// 停用租户默认拒绝写入。
	/// </summary>
	[Fact]
	public void TenantEntryGuard_ShouldRejectInactiveTenantWrite()
	{
		var context = new TenantEntryContext(
			TenantEntryOperation.Added,
			null,
			null,
			"a",
			null,
			TenantStatus.Archived,
			false,
			false);

		Assert.Throws<TenantSuspendedException>(() => TenantEntryGuard.Validate(context));
	}

	/// <summary>
	/// 解析链按优先级选择候选，并拒绝不一致的候选。
	/// </summary>
	[Fact]
	public async Task TenantResolver_ShouldResolveAndDetectConflicts()
	{
		var store = new InMemoryTenantStore([NewTenant("a", "alpha"), NewTenant("b", "beta")]);
		var options = Microsoft.Extensions.Options.Options.Create(new MultiTenancyOptions());

		var resolver = new TenantResolver(
			[
				new StaticContributor(100, "a"),
				new StaticContributor(200, "alpha"),
			],
			store,
			new StaticOptionsMonitor(options));

		var result = await resolver.ResolveAsync(new TenantResolveContext(), TestContext.Current.CancellationToken);

		Assert.True(result.Succeeded);
		Assert.Equal("a", result.Tenant!.Id);

		var conflictResolver = new TenantResolver(
			[
				new StaticContributor(100, "a"),
				new StaticContributor(200, "beta"),
			],
			store,
			new StaticOptionsMonitor(options));

		var conflict = await conflictResolver.ResolveAsync(new TenantResolveContext(), TestContext.Current.CancellationToken);

		Assert.False(conflict.Succeeded);
		Assert.Equal(TenantFailureCodes.Conflict, conflict.FailureCode);

		var missing = await resolver.ResolveAsync(new TenantResolveContext(), TestContext.Current.CancellationToken);
		Assert.True(missing.Succeeded);
	}

	/// <summary>
	/// RequireTenant=false 时无候选值不视为失败。
	/// </summary>
	[Fact]
	public async Task TenantResolver_WithoutRequirement_ShouldAllowEmpty()
	{
		var store = new InMemoryTenantStore([]);
		var resolver = new TenantResolver([], store, new StaticOptionsMonitor(
			Microsoft.Extensions.Options.Options.Create(new MultiTenancyOptions { RequireTenant = false })));

		var result = await resolver.ResolveAsync(new TenantResolveContext(), TestContext.Current.CancellationToken);

		Assert.Null(result.FailureCode);
		Assert.Null(result.Tenant);
		Assert.False(result.Succeeded);
	}

	/// <summary>
	/// 键转换器按租户添加前缀，并在逃逸作用域内原样返回。
	/// </summary>
	[Fact]
	public void TenantKeyTransformer_ShouldPrefixAndRespectSuppression()
	{
		AmbientExecutionContext.Reset();
		var transformer = new TenantKeyTransformer();

		Assert.Equal("order:1", transformer.Transform("order:1"));

		using (new TenantScopeFactory().Begin(NewTenant("a")))
		{
			Assert.Equal("t:a:order:1", transformer.Transform("order:1"));

			using (KeyTransformSuppression.Suppress())
			{
				Assert.Equal("order:1", transformer.Transform("order:1"));
			}
		}

		AmbientExecutionContext.Reset();
	}

	private sealed class StaticContributor : ITenantResolveContributor
	{
		private readonly string? _value;

		public StaticContributor(int order, string? value)
		{
			Order = order;
			_value = value;
		}

		public int Order { get; }

		public ValueTask<string?> ResolveAsync(TenantResolveContext context, CancellationToken cancellationToken = default)
		{
			return ValueTask.FromResult(_value);
		}
	}

	private sealed class StaticOptionsMonitor : Microsoft.Extensions.Options.IOptionsMonitor<MultiTenancyOptions>
	{
		private readonly MultiTenancyOptions _value;

		public StaticOptionsMonitor(Microsoft.Extensions.Options.IOptions<MultiTenancyOptions> options)
		{
			_value = options.Value;
		}

		public MultiTenancyOptions CurrentValue => _value;

		public MultiTenancyOptions Get(string? name) => _value;

		public IDisposable? OnChange(Action<MultiTenancyOptions, string?> listener) => null;
	}
}
