using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对接收者注册器释放方式的回归测试。
/// </summary>
/// <remarks>
/// 注册器以单例注册到 DI 容器。接口同时继承 <see cref="IDisposable"/> 和 <see cref="IAsyncDisposable"/>，
/// 实现方只需提供同步 <c>Dispose</c>，异步释放由接口默认实现转调；因此容器无论以
/// <c>Dispose()</c> 还是 <c>DisposeAsync()</c> 释放都不会再抛出
/// “type only implements IAsyncDisposable” 异常。
/// </remarks>
public class RecipientRegistrarDisposeTests
{
	/// <summary>
	/// 验证容器同步释放持有内存注册器的服务提供程序时不抛出异常，且注册器同时实现同步与异步释放接口。
	/// </summary>
	[Fact]
	public void Dispose_ProviderWithInMemoryRegistrar_DoesNotThrow()
	{
		var provider = BuildProvider();
		var registrar = provider.GetRequiredService<IRecipientRegistrar>();

		Assert.IsAssignableFrom<IDisposable>(registrar);
		Assert.IsAssignableFrom<IAsyncDisposable>(registrar);

		var exception = Record.Exception(provider.Dispose);

		Assert.Null(exception);
	}

	/// <summary>
	/// 验证异步释放注册器时走接口默认实现，转调同步 <c>Dispose</c> 且不抛出异常。
	/// </summary>
	[Fact]
	public async Task DisposeAsync_Registrar_UsesDefaultImplementation()
	{
		var provider = BuildProvider();
		var registrar = provider.GetRequiredService<IRecipientRegistrar>();

		var exception = await Record.ExceptionAsync(() => ((IAsyncDisposable)registrar).DisposeAsync().AsTask());

		Assert.Null(exception);

		await provider.DisposeAsync();
	}

	private static ServiceProvider BuildProvider()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "InMemory");
		services.AddEuoniaBus();
		services.AddInMemoryBus("InMemory");

		return services.BuildServiceProvider();
	}
}
