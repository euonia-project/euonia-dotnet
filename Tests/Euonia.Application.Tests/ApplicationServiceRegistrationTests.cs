using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Application.Tests;

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
}
