using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 验证应用服务注册结果：接口与实现类均以 Castle 代理解析，并在同一作用域内共享目标实例。
/// </summary>
[Collection("AppTests")]
public class ApplicationServiceRegistrationTests
{
	/// <summary>
	/// 从容器解析接口应用服务时，应得到 Castle 动态代理而非原始实现类型。
	/// </summary>
	[Fact]
	public void InterfaceResolution_ShouldReturnCastleProxy()
	{
		using var scope = TestContainer.CreateProvider().CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ICounterService>();

		Assert.StartsWith("Castle.Proxies", svc.GetType().FullName);
	}

	/// <summary>
	/// 同一作用域内同一服务的两个接口视图应共享目标实例：经一个视图写入的状态可由另一个视图读到，且目标仅创建一次。
	/// </summary>
	[Fact]
	public void InterfaceViews_ShouldShareTargetInstance()
	{
		OrderAuditService.Created = 0;

		using var scope = TestContainer.CreateProvider().CreateScope();
		var order = scope.ServiceProvider.GetRequiredService<IOrderService>();
		var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();

		order.SetFlag("shared-value");
		var flag = audit.GetFlag();

		Assert.Equal("shared-value", flag);
		Assert.Equal(1, OrderAuditService.Created);
	}

	/// <summary>
	/// 框架接口 <see cref="IHasLazyServiceProvider"/> 与 <see cref="IApplicationService"/> 不应被注册为应用服务代理，解析结果应为 <c>null</c>。
	/// </summary>
	[Fact]
	public void FrameworkInterfaces_ShouldNotBeRegistered()
	{
		using var scope = TestContainer.CreateProvider().CreateScope();

		// IHasLazyServiceProvider / IApplicationService 属于框架接口，
		// 不应被注册为应用服务的代理服务类型（修复 5 前会被第一个应用服务"劫持"）。
		Assert.Null(scope.ServiceProvider.GetService<IHasLazyServiceProvider>());
		Assert.Null(scope.ServiceProvider.GetService<IApplicationService>());
	}

	/// <summary>
	/// 直接解析实现类时，应得到保持 Is-A 关系的类代理，其类型名带有 Castle 前缀且可赋值给实现类型。
	/// </summary>
	[Fact]
	public void ConcreteResolution_ShouldReturnClassProxy()
	{
		using var scope = TestContainer.CreateProvider().CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<CounterService>();

		// 类代理保持 Is-A 关系（可强转为实现类型），同时带 Castle 代理类型名。
		Assert.StartsWith("Castle.Proxies", svc.GetType().FullName);
		Assert.True(typeof(CounterService).IsAssignableFrom(svc.GetType()));
	}

	/// <summary>
	/// 直接解析实现类时拦截器仍应生效，20 个并发调用下最大并发数应保持为 1。
	/// </summary>
	[Fact]
	public async Task ConcreteResolution_ShouldApplyInterceptors()
	{
		// 直接解析实现类也必须获得拦截（修复 6）：锁应生效。
		CounterService.MaxConcurrent = 0;

		using var scope = TestContainer.CreateProvider().CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<CounterService>();

		await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => svc.GetNextAsync()));

		Assert.Equal(1, CounterService.MaxConcurrent);
	}

	/// <summary>
	/// 无默认构造函数的实现类无法生成类代理时应回退为裸实例，而对应接口仍解析为代理。
	/// </summary>
	[Fact]
	public void NoDefaultConstructor_ShouldFallBackToRawInstance()
	{
		using var scope = TestContainer.CreateProvider().CreateScope();

		// 无默认构造函数无法生成类代理，回退为裸实例；
		// 接口路径（接口代理）不受影响。
		var svc = scope.ServiceProvider.GetRequiredService<NoDefaultCtorService>();
		Assert.IsType<NoDefaultCtorService>(svc);

		var proxy = scope.ServiceProvider.GetRequiredService<INoDefaultCtorService>();
		Assert.StartsWith("Castle.Proxies", proxy.GetType().FullName);
	}

	/// <summary>
	/// 分别以接口和实现类解析同一服务时应共享目标实例，经接口写入的状态应能从实现类读取。
	/// </summary>
	[Fact]
	public void InterfaceAndConcrete_ShouldShareTargetInstance()
	{
		using var scope = TestContainer.CreateProvider().CreateScope();
		var iface = scope.ServiceProvider.GetRequiredService<IOrderService>();
		var concrete = scope.ServiceProvider.GetRequiredService<OrderAuditService>();

		iface.SetFlag("via-interface");
		// virtual 属性经类代理转发到共享的目标实例。
		var flag = concrete.Flag;

		Assert.Equal("via-interface", flag);
	}
}
