using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions"/> 中
/// 内存版发件箱/收件箱存储注册扩展的测试。
/// </summary>
public class StoreRegistrationTests
{
	[Fact]
	public void AddInMemoryOutbox_RegistersReferenceStore()
	{
		var services = new ServiceCollection();
		services.AddInMemoryOutbox();

		using var provider = services.BuildServiceProvider();
		var store = provider.GetRequiredService<IOutboxStore>();
		Assert.IsType<InMemoryOutboxStore>(store);
	}

	[Fact]
	public void AddInMemoryInbox_RegistersReferenceStore()
	{
		var services = new ServiceCollection();
		services.AddInMemoryInbox();

		using var provider = services.BuildServiceProvider();
		var store = provider.GetRequiredService<IInboxStore>();
		Assert.IsType<InMemoryInboxStore>(store);
	}

	[Fact]
	public void AddInMemoryStores_AreTryAddSingleton_DoesNotOverwriteCustomStore()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IOutboxStore, CustomOutboxStore>();
		services.AddInMemoryOutbox();

		using var provider = services.BuildServiceProvider();
		Assert.IsType<CustomOutboxStore>(provider.GetRequiredService<IOutboxStore>());
	}

	private sealed class CustomOutboxStore : InMemoryOutboxStore;
}