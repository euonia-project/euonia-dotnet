using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.Tests.Tenancy;

/// <summary>
/// 关闭多租户时的行为：租户模型上下文必须快速失败，而不是静默返回空数据。
/// </summary>
public class TenancyDisabledTests
{
	/// <summary>
	/// `Enabled=false` 时构造租户上下文抛配置异常。
	/// </summary>
	[Fact]
	public void MultiTenantContext_WhenDisabled_ShouldFailFast()
	{
		using var connection = new SqliteConnection("DataSource=:memory:");
		connection.Open();

		var options = new DbContextOptionsBuilder<TenantAdvancedEfTests.RoutedContext>()
		              .UseSqlite(connection)
		              .Options;

		var accessor = new TenantAccessor();
		var tenancy = Options.Create(new MultiTenancyOptions { Enabled = false });

		Assert.Throws<ConfigurationException>(() => new TenantAdvancedEfTests.RoutedContext(options, accessor, tenancy));
	}

	/// <summary>
	/// 启用但未注册租户存储时启动校验抛配置异常。
	/// </summary>
	[Fact]
	public async Task StartupValidator_WithoutStore_ShouldThrow()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		services.AddTenancy(options => options.Enabled = true);
		await using var provider = services.BuildServiceProvider();

		var validator = new TenantStartupValidator(
			provider,
			Options.Create(new MultiTenancyOptions { Enabled = true }));

		await Assert.ThrowsAsync<ConfigurationException>(() => validator.StartAsync(TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// 关闭时不校验存储。
	/// </summary>
	[Fact]
	public async Task StartupValidator_WhenDisabled_ShouldPass()
	{
		var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
		await using var provider = services.BuildServiceProvider();

		var validator = new TenantStartupValidator(
			provider,
			Options.Create(new MultiTenancyOptions { Enabled = false }));

		await validator.StartAsync(TestContext.Current.CancellationToken);
	}
}
