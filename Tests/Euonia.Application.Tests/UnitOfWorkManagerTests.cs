using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 针对真实 <see cref="UnitOfWorkManager"/> 的测试。
/// </summary>
/// <remarks>
/// 既有的 <c>UnitOfWorkInterceptorTests</c> 对 <see cref="IUnitOfWorkManager"/> 使用了 mock，
/// 因此从未覆盖 <see cref="UnitOfWorkManager"/> 创建作用域的真实路径，这正是下列缺陷得以存留的原因。
/// </remarks>
public class UnitOfWorkManagerTests
{
	/// <summary>
	/// 回归测试：<see cref="DisposableObject.Disposed"/> 由 <c>WeakEventManager</c> 支撑，
	/// 仅弱引用处理器目标。此前用内联 lambda 订阅，其闭包除该弱引用外无任何强引用，
	/// 一经 GC 即被回收 → 工作单元的依赖注入作用域（及其中的 DbContext 等）永不释放，
	/// 且外层工作单元不会被恢复为当前工作单元。
	/// </summary>
	[Fact]
	public void Begin_WhenGarbageCollectedBeforeDispose_StillDisposesScope()
	{
		ProbeDisposable.DisposedCount = 0;

		using var provider = BuildProvider();
		var manager = provider.GetRequiredService<IUnitOfWorkManager>();

		var uow = manager.Begin(new UnitOfWorkOptions());

		// 在该工作单元的作用域内实例化探针，用于观察作用域是否被释放。
		uow.ServiceProvider.GetRequiredService<ProbeDisposable>();

		// 模拟真实场景：工作单元存活期间经历垃圾回收。
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		uow.Dispose();

		Assert.Equal(1, ProbeDisposable.DisposedCount);
	}

	/// <summary>
	/// 对照组：不做强制回收时同样应释放（用于证明上一条测试失败确由 GC 引起）。
	/// </summary>
	[Fact]
	public void Begin_WhenDisposedImmediately_DisposesScope()
	{
		ProbeDisposable.DisposedCount = 0;

		using var provider = BuildProvider();
		var manager = provider.GetRequiredService<IUnitOfWorkManager>();

		var uow = manager.Begin(new UnitOfWorkOptions());
		uow.ServiceProvider.GetRequiredService<ProbeDisposable>();
		uow.Dispose();

		Assert.Equal(1, ProbeDisposable.DisposedCount);
	}

	private static ServiceProvider BuildProvider()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddUnitOfWork();
		services.AddScoped<ProbeDisposable>();
		return services.BuildServiceProvider();
	}

	/// <summary>
	/// 统计释放次数的探针服务，注册为 Scoped 以便观察工作单元作用域是否被释放。
	/// </summary>
	private sealed class ProbeDisposable : IDisposable
	{
		public static int DisposedCount;

		public void Dispose()
		{
			DisposedCount++;
		}
	}
}
