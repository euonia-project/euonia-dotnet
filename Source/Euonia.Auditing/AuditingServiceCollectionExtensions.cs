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

		// 幂等：重复调用不能重复注册 IHostedService，但选项配置仍应生效。
		var alreadyRegistered = services.Any(descriptor => descriptor.ServiceType == typeof(AuditingRegistrationMarker));
		var writerInterface = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IAuditingWriter));
		var writerConcrete = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(AuditingWriter));

		var builder = services.AddOptions<AuditingOptions>();
		if (services.Any(x => x.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration)))
		{
			builder.BindConfiguration(AuditingOptions.ConfigurationSection);
		}

		if (configure != null)
		{
			builder.Configure(configure);
		}

		// 无 Host 场景也能在首次解析 Options 时安装授权策略。
		builder.PostConfigure(options => AuditingQueryAuthorizer.ConfigureCrossTenantAuthorization(options.CrossTenantQueryAuthorizer));

		if (alreadyRegistered)
		{
			return services;
		}

		services.AddSingleton<AuditingRegistrationMarker>();

		services.TryAddSingleton<IAuditingRecorder>(provider => new AuditingRecorder(
			provider.GetRequiredService<IOptions<AuditingOptions>>().Value,
			provider.GetService<IAuditingWriter>(),
			provider.GetService<Microsoft.Extensions.Logging.ILogger<AuditingRecorder>>()));

		if (writerInterface == null && writerConcrete == null)
		{
			// 没有任何写入器：注册内置写入器（单实例 + hosted service）。
			services.TryAddSingleton<AuditingWriter>(provider => new AuditingWriter(
				provider,
				provider.GetRequiredService<IOptions<AuditingOptions>>(),
				provider.GetService<Microsoft.Extensions.Logging.ILogger<AuditingWriter>>(),
				provider.GetService<IAuditingSpool>()));
			services.TryAddSingleton<IAuditingWriter>(provider => provider.GetRequiredService<AuditingWriter>());
		}
		else if (writerInterface == null)
		{
			// 宿主只注册了具体 AuditingWriter：补 IAuditingWriter 别名并启动同一实例。
			services.TryAddSingleton<IAuditingWriter>(provider => provider.GetRequiredService<AuditingWriter>());
		}

		// 无论接口注册是类型、实例还是工厂，hosted service 都解析同一个 IAuditingWriter；
		// 仅当它确实是内置 AuditingWriter 时才启动，自定义写入器自行管理生命周期。
		services.AddSingleton<IHostedService, AuditingWriterHostedService>();

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

		services.TryAddSingleton<IAuditingSpool>(provider => new FileAuditingSpool(
			directory,
			provider.GetService<Microsoft.Extensions.Logging.ILogger<FileAuditingSpool>>()));
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
		if (!services.Any(descriptor => descriptor.ServiceType == typeof(IAuditingStore)))
		{
			services.AddSingleton<IAuditingStore>(provider => provider.GetRequiredService<InMemoryAuditingStore>());
			services.AddSingleton<IAuditingStoreQuery>(provider => provider.GetRequiredService<InMemoryAuditingStore>());
		}
		return services;
	}
}
