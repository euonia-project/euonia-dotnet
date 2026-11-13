using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <c>Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions</c> 中
/// 内存版发件箱/收件箱存储注册扩展的测试。
/// </summary>
public class StoreRegistrationTests
{
	/// <summary>
	/// 验证 <c>AddInMemoryOutbox</c> 将 <see cref="InMemoryOutboxStore"/> 注册为发件箱存储实现。
	/// </summary>
	[Fact]
	public void AddInMemoryOutbox_RegistersReferenceStore()
	{
		var services = new ServiceCollection();
		services.AddInMemoryOutbox();

		using var provider = services.BuildServiceProvider();
		var store = provider.GetRequiredService<IOutboxStore>();
		Assert.IsType<InMemoryOutboxStore>(store);
	}

	/// <summary>
	/// 验证 <c>AddInMemoryInbox</c> 将 <see cref="InMemoryInboxStore"/> 注册为收件箱存储实现。
	/// </summary>
	[Fact]
	public void AddInMemoryInbox_RegistersReferenceStore()
	{
		var services = new ServiceCollection();
		services.AddInMemoryInbox();

		using var provider = services.BuildServiceProvider();
		var store = provider.GetRequiredService<IInboxStore>();
		Assert.IsType<InMemoryInboxStore>(store);
	}

	/// <summary>
	/// 验证内存存储以 <c>TryAdd</c> 方式注册，不会覆盖调用方已注册的自定义存储。
	/// </summary>
	[Fact]
	public void AddInMemoryStores_AreTryAddSingleton_DoesNotOverwriteCustomStore()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IOutboxStore, CustomOutboxStore>();
		services.AddInMemoryOutbox();

		using var provider = services.BuildServiceProvider();
		Assert.IsType<CustomOutboxStore>(provider.GetRequiredService<IOutboxStore>());
	}

	/// <summary>
	/// 调用方预先注册的自定义发件箱存储，用于验证内存存储的注册不会覆盖它。
	/// </summary>
	private sealed class CustomOutboxStore : InMemoryOutboxStore;
}