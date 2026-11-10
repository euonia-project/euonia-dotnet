using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 验证 <see cref="BusinessObjectFactory"/> 的实例创建点可被派生工厂接管。
/// </summary>
public class FactoryExtensibilityTests
{
	/// <summary>
	/// 验证自定义工厂覆盖实例创建点后，由该工厂实际创建实例并记录创建次数。
	/// </summary>
	[Fact]
	public async Task CustomFactory_ShouldOverrideInstanceCreation()
	{
		var services = new ServiceCollection();
		services.AddScoped<IObjectFactory, CountingFactory>();
		services.AddBusinessObject();

		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();
		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);

		try
		{
			var factory = scope.ServiceProvider.GetRequiredService<IObjectFactory>();
			var target = await factory.CreateAsync<FactoryProbe>(TestContext.Current.CancellationToken);

			var custom = Assert.IsType<CountingFactory>(factory);
			Assert.Equal(1, custom.Instances);
			Assert.NotNull(target);
		}
		finally
		{
			BusinessContextAccessor.Clear();
		}
	}
}

/// <summary>
/// 统计实例创建次数的自定义工厂。
/// </summary>
public sealed class CountingFactory(IServiceProvider provider) : BusinessObjectFactory(provider)
{
	/// <summary>
	/// 该工厂累计创建的实例数量。
	/// </summary>
	public int Instances { get; private set; }

	/// <summary>
	/// 覆盖实例创建点：先累加创建计数，再委托基类创建实例。
	/// </summary>
	/// <typeparam name="TTarget">待创建的业务对象类型。</typeparam>
	/// <returns>创建出的目标类型实例。</returns>
	protected override TTarget GetObjectInstance<TTarget>()
	{
		Instances++;
		return base.GetObjectInstance<TTarget>();
	}
}

/// <summary>
/// 用于工厂创建测试的最小业务对象。
/// </summary>
public sealed class FactoryProbe : BusinessObject<FactoryProbe>
{
	/// <summary>
	/// 指示创建工厂方法是否已执行。
	/// </summary>
	public bool Created { get; private set; }

	[FactoryCreate]
	private Task CreateAsync(CancellationToken cancellationToken = default)
	{
		Created = true;
		return Task.CompletedTask;
	}
}
