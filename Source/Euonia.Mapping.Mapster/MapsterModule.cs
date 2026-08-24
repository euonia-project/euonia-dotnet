using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Modularity;

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
		context.Services.TryAddKeyedSingleton<ITypeAdapterFactory, MapsterTypeAdapterFactory>(SERVICE_KEY);
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