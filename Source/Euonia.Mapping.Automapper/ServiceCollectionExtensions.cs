using AutoMapper;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Mapping;
using Type = System.Type;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Collections;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于注册对象映射提供程序的 <see cref="IServiceCollection"/> 扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <param name="services">要向其添加服务的 <see cref="IServiceCollection"/>。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 注册 <see cref="Mapper"/> 作为对象映射提供程序。
		/// </summary>
		/// <param name="factory">返回要注册的 Profile 类型集合的委托。</param>
		/// <param name="config">用于配置映射表达式的委托；可为 <see langword="null"/>。</param>
		/// <param name="assertConfiguration">是否在创建映射器前断言配置有效性。</param>
		/// <returns>操作完成后返回该实例的引用。</returns>
		public IServiceCollection AddAutomapper(Func<IEnumerable<Type>> factory, Action<MapperConfigurationExpression> config = null, bool assertConfiguration = false)
		{
			var types = factory?.Invoke();
			return services.AddAutomapper(types, config, assertConfiguration);
		}

		/// <summary>
		/// 注册 <see cref="Mapper"/> 作为对象映射提供程序。
		/// </summary>
		/// <param name="handler">用于向列表填充要注册的 Profile 类型的委托。</param>
		/// <param name="config">用于配置映射表达式的委托；可为 <see langword="null"/>。</param>
		/// <param name="assertConfiguration">是否在创建映射器前断言配置有效性。</param>
		/// <returns>操作完成后返回该实例的引用。</returns>
		public IServiceCollection AddAutomapper(Action<List<Type>> handler, Action<MapperConfigurationExpression> config = null, bool assertConfiguration = false)
		{
			var types = new List<Type>();
			handler?.Invoke(types);
			return services.AddAutomapper(types, config, assertConfiguration);
		}

		/// <summary>
		/// 注册 <see cref="Mapper"/> 作为对象映射提供程序。
		/// </summary>
		/// <exception cref="Exception">已注册的 Profile 无法从服务提供程序创建实例。</exception>
		/// <returns>操作完成后返回该实例的引用。</returns>
		/// <remarks>根据已配置的 <see cref="AutomapperOptions"/> 构建映射配置，并对其中的 Profile 逐一断言配置有效性。</remarks>
		public IServiceCollection AddAutomapper()
		{
		

			return services.AddSingleton(provider =>
			{
				var logger = provider.GetService<ILoggerFactory>();
			
				var expression = new MapperConfigurationExpression();
				expression.ConstructServicesUsing(type => ActivatorUtilities.GetServiceOrCreateInstance(provider, type));
				var options = provider.GetService<IOptions<AutomapperOptions>>()?.Value;
				if (options != null)
				{
					foreach (var configurator in options.Configurators)
					{
						configurator(provider, expression);
					}
				}

				var mapperConfiguration = new MapperConfiguration(expression, logger);

				foreach (var profileType in (options?.ValidatingProfiles ?? new TypeList<Profile>()))
				{
					var profile = (Profile)ActivatorUtilities.CreateInstance(provider, profileType);
					if (profile == null)
					{
						throw new Exception($"{profileType} is a not valid AutoMapper profile.");
					}

					mapperConfiguration.AssertConfigurationIsValid();
					//mapperConfiguration.AssertConfigurationIsValid(profile.ProfileName);
				}

				var mapper = mapperConfiguration.CreateMapper();
				return mapper;
			});
		}

		/// <summary>
		/// 注册 <see cref="Mapper"/> 作为对象映射提供程序。
		/// </summary>
		/// <param name="types">要注册的 Profile 类型集合；可为 <see langword="null"/>。</param>
		/// <param name="config">用于配置映射表达式的委托；可为 <see langword="null"/>。</param>
		/// <param name="assertConfiguration">是否在创建映射器前断言配置有效性。</param>
		/// <returns>操作完成后返回该实例的引用。</returns>
		public IServiceCollection AddAutomapper(IEnumerable<Type> types, Action<MapperConfigurationExpression> config = null, bool assertConfiguration = false)
		{
			return services.AddSingleton(provider =>
			{
				var logger = provider.GetService<ILoggerFactory>();

				var expression = new MapperConfigurationExpression();

				if (types != null)
				{
					foreach (var type in types)
					{
						expression.AddProfile(type);
					}
				}

				config?.Invoke(expression);
				var mapperConfiguration = new MapperConfiguration(expression, logger);

				if (assertConfiguration)
				{
					mapperConfiguration.AssertConfigurationIsValid();
				}

				var mapper = mapperConfiguration.CreateMapper();
				return mapper;
			});
		}

		/// <summary>
		/// 注册 <see cref="Mapper"/> 作为对象映射提供程序。
		/// </summary>
		/// <param name="config">用于配置映射表达式的委托；可为 <see langword="null"/>。</param>
		/// <param name="assertConfiguration">是否在创建映射器前断言配置有效性。</param>
		/// <returns>操作完成后返回该实例的引用。</returns>
		public IServiceCollection AddAutomapper(Action<MapperConfigurationExpression> config, bool assertConfiguration = false)
		{
			return services.AddSingleton(provider =>
			{
				var logger = provider.GetService<ILoggerFactory>();

				var expression = new MapperConfigurationExpression();

				config?.Invoke(expression);
				var mapperConfiguration = new MapperConfiguration(expression, logger);

				if (assertConfiguration)
				{
					mapperConfiguration.AssertConfigurationIsValid();
				}

				var mapper = mapperConfiguration.CreateMapper();
				return mapper;
			});
		}
	}
}