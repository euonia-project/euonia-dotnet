using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计能力的服务注册扩展。
/// </summary>
public static class AuditingServiceCollectionExtensions
{
	/// <summary>
	/// 注册审计能力。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configure">审计选项配置委托。</param>
	/// <returns>注册完成后的服务集合。</returns>
	/// <remarks>
	/// 必须再注册一个 <see cref="IAuditingStore"/>（例如 <see cref="AddInMemoryAuditingStore"/> 或 EF/Mongo 适配）。
	/// </remarks>
	public static IServiceCollection AddAuditing(this IServiceCollection services, Action<AuditingOptions>? configure = null)
	{
		ArgumentNullException.ThrowIfNull(services);

		var builder = services.AddOptions<AuditingOptions>();
		if (services.Any(x => x.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration)))
		{
			builder.BindConfiguration(AuditingOptions.ConfigurationSection);
		}

		if (configure != null)
		{
			builder.Configure(configure);
		}

		services.TryAddSingleton<IAuditingRecorder>(provider => new AuditingRecorder(
			provider.GetRequiredService<IOptions<AuditingOptions>>().Value,
			provider.GetService<IAuditingWriter>(),
			provider.GetService<Microsoft.Extensions.Logging.ILogger<AuditingRecorder>>()));

		services.TryAddSingleton<AuditingWriter>(provider => new AuditingWriter(
			provider,
			provider.GetRequiredService<IOptions<AuditingOptions>>(),
			provider.GetService<Microsoft.Extensions.Logging.ILogger<AuditingWriter>>(),
			provider.GetService<IAuditingSpool>()));
		services.TryAddSingleton<IAuditingWriter>(provider => provider.GetRequiredService<AuditingWriter>());
		// TryAddEnumerable 不支持工厂描述符；宿主服务与 IAuditingWriter 必须是同一实例。
		services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<AuditingWriter>());

		return services;
	}

	/// <summary>
	/// 注册基于文件的持久化暂存，使审计记录在进程崩溃后可重放。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="directory">暂存目录。</param>
	/// <returns>注册完成后的服务集合。</returns>
	public static IServiceCollection AddFileAuditingSpool(this IServiceCollection services, string directory)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentException.ThrowIfNullOrWhiteSpace(directory);

		services.TryAddSingleton<IAuditingSpool>(_ => new FileAuditingSpool(directory));
		return services;
	}

	/// <summary>
	/// 注册内存审计存储，适用于测试与本地开发。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <returns>注册完成后的服务集合。</returns>
	public static IServiceCollection AddInMemoryAuditingStore(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.TryAddSingleton<InMemoryAuditingStore>();
		services.TryAddSingleton<IAuditingStore>(provider => provider.GetRequiredService<InMemoryAuditingStore>());
		return services;
	}
}
