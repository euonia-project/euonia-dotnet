using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;
using Nerosoft.Euonia.Threading;

// ReSharper disable MemberCanBePrivate.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于向 <see cref="IServiceCollection"/> 注册 <see cref="IRepository{TEntity,TKey}"/> 及 EF Core 数据上下文的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	private const string CONNECTION_STRING_PATTERN = @"^(?<provider>(?:\w|\-)+):\/\/(?<conn>.*)";

	/// <param name="services">接收扩展方法的 <see cref="IServiceCollection"/>，即服务注册的目标容器。</param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// 将 EF Core 仓储及数据上下文注册到 <see cref="IServiceCollection"/>。
		/// </summary>
		/// <param name="options">用于配置数据上下文选项的委托。</param>
		/// <param name="contextLifeTime">仓储与数据上下文的注册生命周期，默认为作用域（Scoped）。</param>
		/// <typeparam name="TContext">数据上下文类型。</typeparam>
		/// <returns>注册完成后的 <paramref name="services"/>，以便链式调用。</returns>
		public IServiceCollection AddEfCoreRepository<TContext>(Action<DbContextOptionsBuilder> options, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContext : DbContext, IRepositoryContext
		{
			services.AddDbContext<TContext>(options, contextLifeTime)
			        .AddEfCoreRepository(contextLifeTime);

			return services;
		}

		/// <summary>
		/// 将 EF Core 仓储及数据上下文注册到 <see cref="IServiceCollection"/>，并指定服务类型与实现类型。
		/// </summary>
		/// <param name="options">用于配置数据上下文选项的委托。</param>
		/// <param name="contextLifeTime">仓储与数据上下文的注册生命周期，默认为作用域（Scoped）。</param>
		/// <typeparam name="TContextService">注册为服务的数据上下文类型。</typeparam>
		/// <typeparam name="TContextImplementation">实际使用的数据上下文实现类型。</typeparam>
		/// <returns>注册完成后的 <paramref name="services"/>，以便链式调用。</returns>
		public IServiceCollection AddEfCoreRepository<TContextService, TContextImplementation>(Action<DbContextOptionsBuilder> options, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContextService : DbContext, IRepositoryContext
			where TContextImplementation : TContextService
		{
			services.AddDbContext<TContextService, TContextImplementation>(options, contextLifeTime)
			        .AddEfCoreRepository(contextLifeTime);

			return services;
		}

		/// <summary>
		/// 将 EF Core 仓储及数据上下文注册到 <see cref="IServiceCollection"/>，并使用可访问服务提供程序的选项配置。
		/// </summary>
		/// <param name="options">使用 <see cref="IServiceProvider"/> 配置数据上下文选项的委托。</param>
		/// <param name="contextLifeTime">仓储与数据上下文的注册生命周期，默认为作用域（Scoped）。</param>
		/// <typeparam name="TContext">数据上下文类型。</typeparam>
		/// <returns>注册完成后的 <paramref name="services"/>，以便链式调用。</returns>
		public IServiceCollection AddEfCoreRepository<TContext>(Action<IServiceProvider, DbContextOptionsBuilder> options, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContext : DbContext, IRepositoryContext
		{
			services.AddDbContext<DbContext, TContext>(options, contextLifeTime)
			        .AddEfCoreRepository(contextLifeTime);

			return services;
		}

		/// <summary>
		/// 将 EF Core 仓储及数据上下文注册到 <see cref="IServiceCollection"/>，并使用可访问服务提供程序的选项配置及指定的服务类型与实现类型。
		/// </summary>
		/// <param name="options">使用 <see cref="IServiceProvider"/> 配置数据上下文选项的委托。</param>
		/// <param name="contextLifeTime">仓储与数据上下文的注册生命周期，默认为作用域（Scoped）。</param>
		/// <typeparam name="TContextService">注册为服务的数据上下文类型。</typeparam>
		/// <typeparam name="TContextImplementation">实际使用的数据上下文实现类型。</typeparam>
		/// <returns>注册完成后的 <paramref name="services"/>，以便链式调用。</returns>
		public IServiceCollection AddEfCoreRepository<TContextService, TContextImplementation>(Action<IServiceProvider, DbContextOptionsBuilder> options, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContextService : DbContext, IRepositoryContext
			where TContextImplementation : TContextService
		{
			services.AddDbContext<TContextService, TContextImplementation>(options, contextLifeTime)
			        .AddEfCoreRepository(contextLifeTime);

			return services;
		}

		/// <summary>
		/// 以泛型开放式类型注册 EF Core 仓储实现，按指定生命周期注册 <see cref="IRepository{TContext,TEntity,TKey}"/>。
		/// </summary>
		/// <param name="contextLifeTime">仓储的注册生命周期，默认为作用域（Scoped）。</param>
		/// <returns>注册完成后的 <paramref name="services"/>，以便链式调用。</returns>
		/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="contextLifeTime"/> 不是受支持的生命周期值时抛出。</exception>
		/// <remarks>
		/// 同时以瞬态生命周期注册 <c>UnitOfWorkContextFactory</c> 作为 <see cref="IContextFactory"/> 实现。
		/// </remarks>
		public IServiceCollection AddEfCoreRepository(ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
		{
			services.AddTransient<IContextFactory, UnitOfWorkContextFactory>();
			switch (contextLifeTime)
			{
				case ServiceLifetime.Scoped:
					services.AddScoped(typeof(IRepository<,,>), typeof(EfCoreRepository<,,>));
					break;
				case ServiceLifetime.Singleton:
					services.AddSingleton(typeof(IRepository<,,>), typeof(EfCoreRepository<,,>));
					break;
				case ServiceLifetime.Transient:
					services.AddTransient(typeof(IRepository<,,>), typeof(EfCoreRepository<,,>));
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(contextLifeTime), contextLifeTime, null);
			}

			return services;
		}

		/// <summary>
		/// 使用固定连接字符串注册数据上下文工厂。
		/// </summary>
		/// <typeparam name="TContext">数据上下文类型。</typeparam>
		/// <param name="connectionString">连接字符串；为 <c>null</c> 时将回退到其它解析来源（如 <see cref="ConnectionStringAttribute"/> 或配置）。</param>
		/// <param name="seeding">可选的数据初始化委托，参数依次为数据上下文、是否为新库以及取消令牌。</param>
		/// <returns>注册完成后的 <paramref name="services"/>，以便链式调用。</returns>
		/// <exception cref="InvalidOperationException">无法解析到任何有效连接字符串时抛出。</exception>
		public IServiceCollection AddDataContextFactory<TContext>(string connectionString = null, Func<DbContext, bool, CancellationToken, Task> seeding = null)
			where TContext : DataContextBase<TContext>
		{
			return services.AddDataContextFactory<TContext>(_ => connectionString, seeding);
		}

		/// <summary>
		/// 使用动态获取连接字符串的工厂注册数据上下文工厂。
		/// </summary>
		/// <typeparam name="TContext">数据上下文类型。</typeparam>
		/// <param name="connectionStringFactory">根据服务提供程序返回连接字符串的工厂委托。</param>
		/// <param name="seeding">可选的数据初始化委托，参数依次为数据上下文、是否为新库以及取消令牌。</param>
		/// <returns>注册完成后的 <paramref name="services"/>，以便链式调用。</returns>
		/// <exception cref="InvalidOperationException">无法解析到任何有效连接字符串时抛出。</exception>
		/// <remarks>
		/// 连接字符串按以下优先级依次尝试，取第一个非空白结果：
		/// <list type="number">
		/// <item><paramref name="connectionStringFactory"/> 的返回值。</item>
		/// <item><see cref="ConnectionStringAttribute"/> 的 <see cref="ConnectionStringAttribute.Value"/>。</item>
		/// <item>以 <see cref="ConnectionStringAttribute"/> 的 <see cref="ConnectionStringAttribute.Name"/> 为键读取配置。</item>
		/// <item>以数据上下文类型名称为键读取配置。</item>
		/// <item><c>Default</c> 连接字符串，以及可选的 <see cref="IConnectionStringResolver{TContext}"/>。</item>
		/// </list>
		/// 连接字符串需包含 <c>{provider}://{connection}</c> 格式的提供程序前缀，用于选择已注册的 <see cref="ConnectionConfigurator"/>。
		/// </remarks>
		public IServiceCollection AddDataContextFactory<TContext>(Func<IServiceProvider, string> connectionStringFactory, Func<DbContext, bool, CancellationToken, Task> seeding = null)
			where TContext : DataContextBase<TContext>
		{
			services.AddDbContextFactory<TContext>((provider, options) =>
			{
				var connectionString = connectionStringFactory?.Invoke(provider);

				var connection = PriorityValueFinder.Find<string>(queue =>
				{
					queue.Enqueue(() => connectionString, 1);
					queue.Enqueue(() =>
					{
						var attribute = typeof(TContext).GetCustomAttribute<ConnectionStringAttribute>();
						return attribute?.Value;
					}, 2);
					queue.Enqueue(() =>
					{
						var attribute = typeof(TContext).GetCustomAttribute<ConnectionStringAttribute>();
						if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Name))
						{
							return provider.GetRequiredService<IConfiguration>().GetConnectionString(attribute.Name);
						}

						return null;
					}, 3);
					queue.Enqueue(() => provider.GetRequiredService<IConfiguration>().GetConnectionString(typeof(TContext).Name), 5);
					queue.Enqueue(() => provider.GetRequiredService<IConfiguration>().GetConnectionString("Default"), 6);
					queue.Enqueue(() =>
					{
						var connectionStringResolver = provider.GetService<IConnectionStringResolver<TContext>>();
						return AsyncContext.Run(() => connectionStringResolver?.GetConnectionStringAsync());
					}, 5);
				}, t => !string.IsNullOrWhiteSpace(t));

				if (string.IsNullOrWhiteSpace(connection))
				{
					throw new InvalidOperationException();
				}

				ConfigureDataContext(connection, provider, options, seeding);
			});
			return services;
		}
	}

	/// <summary>
	/// 根据连接字符串解析数据库提供程序并配置数据上下文。
	/// </summary>
	/// <param name="connectionString">连接字符串，需符合 <c>{provider}://{connection}</c> 格式。</param>
	/// <param name="provider">用于解析 <see cref="ConnectionConfigurator"/> 的服务提供程序。</param>
	/// <param name="options">要配置的数据上下文选项构建器。</param>
	/// <param name="seeding">可选的数据初始化委托。</param>
	/// <exception cref="ArgumentException"><paramref name="connectionString"/> 为 <c>null</c>、空白或格式不合法时抛出。</exception>
	/// <exception cref="NotSupportedException">连接字符串中指定的数据库提供程序未注册对应的 <see cref="ConnectionConfigurator"/> 时抛出。</exception>
	private static void ConfigureDataContext(string connectionString, IServiceProvider provider, DbContextOptionsBuilder options, Func<DbContext, bool, CancellationToken, Task> seeding = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

		var match = Regex.Match(connectionString, CONNECTION_STRING_PATTERN);
		if (!match.Success)
		{
			throw new ArgumentException("Invalid connection string format.");
		}

		var databaseProvider = match.Groups["provider"].Value;
		var connection = match.Groups["conn"].Value;

		var configurer = provider.GetKeyedService<ConnectionConfigurator>(databaseProvider);
		if (configurer == null)
		{
			throw new NotSupportedException($"The database provider '{databaseProvider}' is not supported.");
		}

		configurer(options, connection);

		if (seeding != null)
		{
			options.UseAsyncSeeding(seeding);
		}
	}
}