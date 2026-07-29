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
/// The extensions method to add the <see cref="IRepository{TEntity,TKey}"/> to <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
	private const string CONNECTION_STRING_PATTERN = @"^(?<provider>(?:\w|\-)+):\/\/(?<conn>.*)";

	/// <param name="services"></param>
	extension(IServiceCollection services)
	{
		/// <summary>
		/// Adds ef core repository to <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="options"></param>
		/// <param name="contextLifeTime"></param>
		/// <typeparam name="TContext"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddEfCoreRepository<TContext>(Action<DbContextOptionsBuilder> options, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContext : DbContext, IRepositoryContext
		{
			services.AddDbContext<TContext>(options, contextLifeTime)
			        .AddEfCoreRepository(contextLifeTime);

			return services;
		}

		/// <summary>
		/// Adds ef core repository to <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="options"></param>
		/// <param name="contextLifeTime"></param>
		/// <typeparam name="TContextService"></typeparam>
		/// <typeparam name="TContextImplementation"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddEfCoreRepository<TContextService, TContextImplementation>(Action<DbContextOptionsBuilder> options, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContextService : DbContext, IRepositoryContext
			where TContextImplementation : TContextService
		{
			services.AddDbContext<TContextService, TContextImplementation>(options, contextLifeTime)
			        .AddEfCoreRepository(contextLifeTime);

			return services;
		}

		/// <summary>
		/// Adds ef core repository to <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="options"></param>
		/// <param name="contextLifeTime"></param>
		/// <typeparam name="TContext"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddEfCoreRepository<TContext>(Action<IServiceProvider, DbContextOptionsBuilder> options, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContext : DbContext, IRepositoryContext
		{
			services.AddDbContext<DbContext, TContext>(options, contextLifeTime)
			        .AddEfCoreRepository(contextLifeTime);

			return services;
		}

		/// <summary>
		/// Adds ef core repository to <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="options"></param>
		/// <param name="contextLifeTime"></param>
		/// <typeparam name="TContextService"></typeparam>
		/// <typeparam name="TContextImplementation"></typeparam>
		/// <returns></returns>
		public IServiceCollection AddEfCoreRepository<TContextService, TContextImplementation>(Action<IServiceProvider, DbContextOptionsBuilder> options, ServiceLifetime contextLifeTime = ServiceLifetime.Scoped)
			where TContextService : DbContext, IRepositoryContext
			where TContextImplementation : TContextService
		{
			services.AddDbContext<TContextService, TContextImplementation>(options, contextLifeTime)
			        .AddEfCoreRepository(contextLifeTime);

			return services;
		}

		/// <summary>
		/// Adds ef core repository to <see cref="IServiceCollection"/>.
		/// </summary>
		/// <param name="contextLifeTime"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
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
		/// Adds a data context factory to the service collection with the specified connection string.
		/// </summary>
		/// <typeparam name="TContext"></typeparam>
		/// <param name="connectionString"></param>
		/// <param name="seeding"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public IServiceCollection AddDataContextFactory<TContext>(string connectionString = null, Func<DbContext, bool, CancellationToken, Task> seeding = null)
			where TContext : DataContextBase<TContext>
		{
			return services.AddDataContextFactory<TContext>(_ => connectionString, seeding);
		}

		/// <summary>
		/// Adds a data context factory to the service collection with the specified connection string.
		/// </summary>
		/// <typeparam name="TContext"></typeparam>
		/// <param name="connectionStringFactory"></param>
		/// <param name="seeding"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
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
	/// Configures the data context based on the provided connection string name.
	/// </summary>
	/// <param name="connectionString"></param>
	/// <param name="provider"></param>
	/// <param name="options"></param>
	/// <param name="seeding"></param>
	/// <exception cref="ArgumentException"></exception>
	/// <exception cref="NotSupportedException"></exception>
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