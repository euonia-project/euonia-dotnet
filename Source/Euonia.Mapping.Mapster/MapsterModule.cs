using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Modularity;
using ServiceMapper = MapsterMapper.ServiceMapper;
using TypeAdapterConfig = Mapster.TypeAdapterConfig;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 用于配置 Mapster 服务的模块。
/// </summary>
/// <remarks>
/// 注册 Mapster 映射服务，并以键为 <c>"mapster"</c> 的单例形式
/// 注册 <see cref="ITypeAdapterFactory"/>；应用初始化时将当前工厂设置为 <see cref="TypeAdapterFactory"/> 的当前工厂。
/// </remarks>
public class MapsterModule : ModuleContextBase
{
	private const string SERVICE_KEY = "mapster";

	/// <inheritdoc />
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddMapster();

		// 不能按类型注册为单例：MapsterTypeAdapterFactory 依赖作用域的 IMapper，
		// 单例会在 ValidateScopes 下启动失败，并捕获根作用域的映射器。
		// ServiceMapper 本身只持有单例 TypeAdapterConfig，可安全地按单例构造。
		context.Services.TryAddKeyedSingleton<ITypeAdapterFactory>(SERVICE_KEY,
			(provider, _) => new MapsterTypeAdapterFactory(new ServiceMapper(provider, provider.GetRequiredService<TypeAdapterConfig>())));
	}

	/// <inheritdoc />
	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
		var factory = context.ServiceProvider.GetKeyedService<ITypeAdapterFactory>(SERVICE_KEY);
		if (factory != null)
		{
			TypeAdapterFactory.SetCurrent(factory);
		}
	}
}