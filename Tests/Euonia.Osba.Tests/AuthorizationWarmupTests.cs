using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;
using Xunit;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// S-2 护栏：<b>异步</b>授权路径必须在同步判定之前预热授权数据。
/// </summary>
/// <remarks>
/// 引擎里的授权数据是按请求异步解析并缓存的，冷缓存首次读取会走 <c>AsyncContext.Run</c>
/// （起私有 <c>SynchronizationContext</c> 并<b>阻塞调用线程</b>直到解析器完成）。
/// 授权判定本身是同步契约，所以 <c>BusinessObjectFactory</c> 的每个 <c>*Async</c> 入口若直接判定，
/// 整条链路会退化成 sync-over-async，负载下表现为线程池饥饿。
/// 本组测试锁死三件事：<b>异步入口会预热</b>、<b>同步入口刻意不预热</b>、<b>预热不改写判定结论</b>。
/// </remarks>
public class AuthorizationWarmupTests
{
	[Fact]
	public async Task EnsureAuthorizedAsync_Should_Warm_The_Checker_Before_Judging()
	{
		var checker = new RecordingPermissionChecker(granted: true);
		using var scope = CreateScope(services => services.AddSingleton<IPermissionChecker>(checker), out var provider);

		var obj = new SecuredEditableObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};

		await ObjectAuthorization.EnsureAuthorizedAsync(obj, BusinessOperation.Create, TestContext.Current.CancellationToken);

		Assert.Equal(1, checker.WarmupCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task EnsureAuthorizedAsync_Should_Warm_Even_When_The_Verdict_Is_Denied()
	{
		var checker = new RecordingPermissionChecker(granted: false);
		using var scope = CreateScope(services => services.AddSingleton<IPermissionChecker>(checker), out var provider);

		var obj = new SecuredEditableObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};

		// 预热必须发生在判定之前：被拒也要先把数据解析出来，否则拒绝就仍是阻塞式拒绝
		await Assert.ThrowsAsync<SecurityException>(() =>
			ObjectAuthorization.EnsureAuthorizedAsync(obj, BusinessOperation.Create, TestContext.Current.CancellationToken).AsTask());

		Assert.Equal(1, checker.WarmupCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void EnsureAuthorized_Sync_Should_Warm_At_The_Entry()
	{
		var checker = new RecordingPermissionChecker(granted: true);
		using var scope = CreateScope(services => services.AddSingleton<IPermissionChecker>(checker), out var provider);

		var obj = new SecuredEditableObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};

		ObjectAuthorization.EnsureAuthorized(obj, BusinessOperation.Create);

		// 引擎的同步读只读已解析的快照，不替任何人等待；于是这次等待被挪到同步入口，
		// 由宿主框架显式做一次——等待点因此可枚举，不在判定路径的深处。
		Assert.Equal(1, checker.WarmupCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task SaveAsync_Factory_Entry_Should_Warm_The_Checker()
	{
		var checker = new RecordingPermissionChecker(granted: true);
		using var scope = CreateScope(services => services.AddSingleton<IPermissionChecker>(checker), out var provider);

		var obj = new SecuredEditableObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};
		obj.MarkAsNew();

		await obj.SaveAsync(cancellationToken: TestContext.Current.CancellationToken);

		// 端到端：工厂的 *Async 入口必须走 EnsureAuthorizedAsync，而不是退回同步判定
		Assert.Equal(1, checker.WarmupCount);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task ScopeAuthorization_Async_Should_Warm_The_Authorizer_Through_The_Same_Scope()
	{
		var authorizer = new RecordingScopeAuthorizer(allowed: false);
		using var scope = CreateScope(services => services.AddSingleton<IObjectScopeAuthorizer>(authorizer), out var provider);

		var obj = new SecuredEditableObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};

		await Assert.ThrowsAsync<SecurityException>(() =>
			ScopeAuthorization.EnsureAuthorizedBeforeAsync(obj, BusinessOperation.Create, TestContext.Current.CancellationToken).AsTask());

		Assert.Equal(1, authorizer.WarmupCount);

		// 预热与判定必须是同一个作用域：换一个就可能预热到别的请求的用户
		Assert.Same(provider, authorizer.LastWarmupScope);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void ScopeAuthorization_Sync_Should_Warm_At_The_Entry()
	{
		var authorizer = new RecordingScopeAuthorizer(allowed: true);
		using var scope = CreateScope(services => services.AddSingleton<IObjectScopeAuthorizer>(authorizer), out var provider);

		var obj = new SecuredEditableObject
		{
			BusinessContext = provider.GetRequiredService<BusinessContext>()
		};

		ScopeAuthorization.EnsureAuthorizedBefore(obj, BusinessOperation.Create);

		Assert.Equal(1, authorizer.WarmupCount);

		// 预热与判定必须是同一个作用域
		Assert.Same(provider, authorizer.LastWarmupScope);

		BusinessContextAccessor.Clear();
	}

	#region Helpers

	private static IServiceScope CreateScope(Action<IServiceCollection> configure, out IServiceProvider provider)
	{
		var services = new ServiceCollection();

		// 走真实的注册路径：权限检查器、数据权限守卫与模型注册表都由 AddBusinessObject/AddPermission 装配
		services.AddBusinessObject(typeof(AuthorizationWarmupTests).Assembly);
		services.AddPermission(p => { p.Scan(typeof(AuthorizationWarmupTests).Assembly); p.Source(ObjectPermissionRequirementProvider.Instance); });

		services.AddSingleton<IScopeSubjectResolver>(new TestSubjectResolver("order:create"));
		services.AddSingleton(new UserPrincipal(new ClaimsPrincipal(new ClaimsIdentity("Bearer"))));

		// 后注册者胜出：用记录型实现顶掉引擎的真实实现
		configure?.Invoke(services);

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		provider = scope.ServiceProvider;
		return scope;
	}

	private sealed class RecordingPermissionChecker(bool granted) : IPermissionChecker
	{
		public int WarmupCount { get; private set; }

		public bool IsGranted(string permission) => granted;

		public bool IsInRole(string role) => false;

		public ValueTask EnsureResolvedAsync(CancellationToken cancellationToken = default)
		{
			WarmupCount++;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class RecordingScopeAuthorizer(bool allowed) : IObjectScopeAuthorizer
	{
		public int WarmupCount { get; private set; }

		public IServiceProvider LastWarmupScope { get; private set; }

		public bool IsConstrained(Type resourceType) => true;

		public bool Allows(object target, string operation, IServiceProvider scope) => allowed;

		public string Explain(object target, string operation, IServiceProvider scope) => "test verdict";

		public ValueTask EnsureResolvedAsync(IServiceProvider scope, CancellationToken cancellationToken = default)
		{
			WarmupCount++;
			LastWarmupScope = scope;
			return ValueTask.CompletedTask;
		}
	}

	#endregion
}
