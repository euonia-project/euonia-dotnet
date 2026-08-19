using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Application.Tests;

[Collection("AppTests")]
public class ApplicationServiceRegistrationTests
{
	[Fact]
	public void InterfaceResolution_ShouldReturnCastleProxy()
	{
		using var scope = TestContainer.CreateProvider().CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<ICounterService>();

		Assert.StartsWith("Castle.Proxies", svc.GetType().FullName);
	}

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

	[Fact]
	public void FrameworkInterfaces_ShouldNotBeRegistered()
	{
		using var scope = TestContainer.CreateProvider().CreateScope();

		// IHasLazyServiceProvider / IApplicationService 属于框架接口，
		// 不应被注册为应用服务的代理服务类型（修复 5 前会被第一个应用服务"劫持"）。
		Assert.Null(scope.ServiceProvider.GetService<IHasLazyServiceProvider>());
		Assert.Null(scope.ServiceProvider.GetService<IApplicationService>());
	}

	[Fact]
	public void ConcreteResolution_ShouldReturnClassProxy()
	{
		using var scope = TestContainer.CreateProvider().CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<CounterService>();

		// 类代理保持 Is-A 关系（可强转为实现类型），同时带 Castle 代理类型名。
		Assert.StartsWith("Castle.Proxies", svc.GetType().FullName);
		Assert.True(typeof(CounterService).IsAssignableFrom(svc.GetType()));
	}

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
