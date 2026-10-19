using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Mapping.Tests;

/// <summary>
/// 针对 <see cref="MapsterModule"/> 服务注册的回归测试。
/// </summary>
public class MapsterModuleTests
{
	[Fact]
	public void ConfigureServices_KeyedFactory_ShouldResolveWithScopeValidation()
	{
		// 回归测试：键控工厂曾按单例注册却依赖作用域的 IMapper，
		// ValidateScopes 开启时启动即失败，关闭时则会捕获根作用域的映射器。
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		new MapsterModule().ConfigureServices(new ServiceConfigurationContext(services));

		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		using var scope = provider.CreateScope();

		var factory = scope.ServiceProvider.GetRequiredKeyedService<ITypeAdapterFactory>("mapster");

		Assert.NotNull(factory);
	}
}
