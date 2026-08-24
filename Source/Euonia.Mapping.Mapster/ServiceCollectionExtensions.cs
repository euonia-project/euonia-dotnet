using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Mapping;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于注册对象映射提供程序的 <see cref="IServiceCollection"/> 扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 注册 <see cref="IMapper"/> 作为对象映射提供程序。
	/// </summary>
	/// <param name="services">要向其添加服务的 <see cref="IServiceCollection"/>。</param>
	/// <returns>操作完成后返回该实例的引用。</returns>
	/// <remarks>
	/// 注册单例 <see cref="TypeAdapterConfig.GlobalSettings"/> 与作用域 <see cref="ServiceMapper"/>。
	/// 构建全局配置时应用 <see cref="MapsterOptions"/> 中注册的映射（Profile）与配置委托，
	/// 以及内置的默认映射注册（如字节数组与 <see cref="long"/> 之间的转换）。
	/// </remarks>
	public static IServiceCollection AddMapster(this IServiceCollection services)
	{
		services.AddSingleton(provider =>
		{
			var options = provider.GetService<IOptions<MapsterOptions>>()?.Value;

			if (options != null)
			{
				foreach (var (type, instance) in options.Profiles)
				{
					IRegister register;
					if (instance == null)
					{
						register = (IRegister)ActivatorUtilities.GetServiceOrCreateInstance(provider, type);
					}
					else
					{
						register = instance;
					}

					TypeAdapterConfig.GlobalSettings.Apply(register);
				}

				foreach (var configurator in options.Configuration)
				{
					configurator(TypeAdapterConfig.GlobalSettings);
				}
			}

			TypeAdapterConfig.GlobalSettings.Apply(GetRegisters());
			return TypeAdapterConfig.GlobalSettings;
		});
		services.AddScoped<IMapper, ServiceMapper>();
		return services;
	}

	private static IEnumerable<IRegister> GetRegisters()
	{
		yield return new ByteArrayToInt64Converter();
	}
}