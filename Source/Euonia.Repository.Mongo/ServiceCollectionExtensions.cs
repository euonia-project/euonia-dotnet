using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于 <see cref="IServiceCollection"/> 的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 用于 <see cref="IServiceCollection"/> 的扩展方法。
	/// </summary>
	/// <param name="services">服务集合实例。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 添加 Mongo 上下文与仓储。
		/// </summary>
		/// <typeparam name="TContext">Mongo 数据库上下文类型，须同时实现 <see cref="IRepositoryContext"/>。</typeparam>
		/// <param name="configure">Mongo 数据库选项配置操作。</param>
		/// <param name="contextLifeTime">上下文与仓储注册使用的服务生命周期，默认为 <see cref="ServiceLifetime.Scoped"/>。</param>
		/// <returns>同一 <paramref name="services"/> 实例，以便链式调用。</returns>
		/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="contextLifeTime"/> 不是 <see cref="ServiceLifetime"/> 的有效取值时抛出。</exception>
		public IServiceCollection AddMongoRepository<TContext>(Action<MongoDbOptions> configure, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContext : MongoDbContext, IRepositoryContext
		{
			services.AddMongoDbContext<TContext>(configure, contextLifeTime)
			        .AddMongoRepository(contextLifeTime);

			return services;
		}

		/// <summary>
		/// 添加 Mongo 上下文与仓储。
		/// </summary>
		/// <typeparam name="TContextService">作为服务契约注册的 Mongo 上下文类型。</typeparam>
		/// <typeparam name="TContextImplementation">实际实例化的上下文类型，须可赋值给 <typeparamref name="TContextService"/>。</typeparam>
		/// <param name="configure">Mongo 数据库选项配置操作。</param>
		/// <param name="contextLifeTime">上下文与仓储注册使用的服务生命周期，默认为 <see cref="ServiceLifetime.Scoped"/>。</param>
		/// <returns>同一 <paramref name="services"/> 实例，以便链式调用。</returns>
		/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="contextLifeTime"/> 不是 <see cref="ServiceLifetime"/> 的有效取值时抛出。</exception>
		public IServiceCollection AddMongoRepository<TContextService, TContextImplementation>(Action<MongoDbOptions> configure, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContextService : MongoDbContext, IRepositoryContext
			where TContextImplementation : TContextService
		{
			services.AddMongoDbContext<TContextService, TContextImplementation>(configure, contextLifeTime)
			        .AddMongoRepository(contextLifeTime);

			return services;
		}

		/// <summary>
		/// 添加 Mongo 仓储。
		/// </summary>
		/// <param name="contextLifeTime">仓储注册使用的服务生命周期，默认为 <see cref="ServiceLifetime.Scoped"/>。</param>
		/// <returns>同一 <paramref name="services"/> 实例，以便链式调用。</returns>
		/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="contextLifeTime"/> 不是 <see cref="ServiceLifetime"/> 的有效取值时抛出。</exception>
		/// <remarks>
		/// 将开放泛型 <see cref="IRepository{TContext, TEntity, TKey}"/> 注册为 <see cref="MongoRepository{TContext, TEntity, TKey}"/>，
		/// 使任意实体类型都可解析到 Mongo 仓储实现。
		/// </remarks>
		public IServiceCollection AddMongoRepository(ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
		{
			switch (contextLifeTime)
			{
				case ServiceLifetime.Scoped:
					services.AddScoped(typeof(IRepository<,,>), typeof(MongoRepository<,,>));
					break;
				case ServiceLifetime.Singleton:
					services.AddSingleton(typeof(IRepository<,,>), typeof(MongoRepository<,,>));
					break;
				case ServiceLifetime.Transient:
					services.AddTransient(typeof(IRepository<,,>), typeof(MongoRepository<,,>));
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(contextLifeTime), contextLifeTime, null);
			}

			return services;
		}

		/// <summary>
		/// 添加 Mongo 数据库上下文。
		/// </summary>
		/// <typeparam name="TContextService">作为服务契约注册的 Mongo 上下文类型。</typeparam>
		/// <typeparam name="TContextImplementation">实际实例化的上下文类型，须可赋值给 <typeparamref name="TContextService"/>。</typeparam>
		/// <param name="configure">Mongo 数据库选项配置操作。</param>
		/// <param name="contextLifeTime">上下文注册使用的服务生命周期，默认为 <see cref="ServiceLifetime.Scoped"/>。</param>
		/// <returns>同一 <paramref name="services"/> 实例，以便链式调用。</returns>
		/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="contextLifeTime"/> 不是 <see cref="ServiceLifetime"/> 的有效取值时抛出。</exception>
		/// <remarks>
		/// 同时以单例方式注册由 <see cref="MongoDbOptions.ConnectionString"/> 与 <see cref="MongoDbOptions.Database"/>
		/// 构建的 <see cref="IMongoDatabase"/>，供上下文复用。
		/// </remarks>
		public IServiceCollection AddMongoDbContext<TContextService, TContextImplementation>(Action<MongoDbOptions> configure, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContextService : MongoDbContext, IRepositoryContext
			where TContextImplementation : TContextService
		{
			services.Configure(configure);
			services.AddSingleton(provider =>
			{
				var options = provider.GetService<IOptions<MongoDbOptions>>()!.Value;
				var client = new MongoClient(options.ConnectionString);
				return client.GetDatabase(options.Database);
			});

			switch (contextLifeTime)
			{
				case ServiceLifetime.Scoped:
					services.AddScoped<TContextService, TContextImplementation>();
					break;
				case ServiceLifetime.Singleton:
					services.AddSingleton<TContextService, TContextImplementation>();
					break;
				case ServiceLifetime.Transient:
					services.AddTransient<TContextService, TContextImplementation>();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(contextLifeTime), contextLifeTime, null);
			}

			return services;
		}

		/// <summary>
		/// 添加 Mongo 数据库上下文。
		/// </summary>
		/// <typeparam name="TContext">Mongo 数据库上下文类型，须同时实现 <see cref="IRepositoryContext"/>。</typeparam>
		/// <param name="configure">Mongo 数据库选项配置操作。</param>
		/// <param name="contextLifeTime">上下文注册使用的服务生命周期，默认为 <see cref="ServiceLifetime.Scoped"/>。</param>
		/// <returns>同一 <paramref name="services"/> 实例，以便链式调用。</returns>
		/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="contextLifeTime"/> 不是 <see cref="ServiceLifetime"/> 的有效取值时抛出。</exception>
		/// <remarks>
		/// 同时以单例方式注册由 <see cref="MongoDbOptions.ConnectionString"/> 与 <see cref="MongoDbOptions.Database"/>
		/// 构建的 <see cref="IMongoDatabase"/>，供上下文复用。
		/// </remarks>
		public IServiceCollection AddMongoDbContext<TContext>(Action<MongoDbOptions> configure, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContext : MongoDbContext, IRepositoryContext
		{
			services.Configure(configure);
			services.AddSingleton(provider =>
			{
				var options = provider.GetService<IOptions<MongoDbOptions>>()!.Value;
				var client = new MongoClient(options.ConnectionString);
				return client.GetDatabase(options.Database);
			});

			switch (contextLifeTime)
			{
				case ServiceLifetime.Scoped:
					services.AddScoped<TContext>();
					break;
				case ServiceLifetime.Singleton:
					services.AddSingleton<TContext>();
					break;
				case ServiceLifetime.Transient:
					services.AddTransient<TContext>();
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(contextLifeTime), contextLifeTime, null);
			}

			return services;
		}
	}
}