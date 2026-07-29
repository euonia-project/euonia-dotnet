using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Sample.Persist;

internal static class ServiceCollectionExtensions
{
	private const string CONNECTION_STRING_PATTERN = @"^(?<dbtype>(?:\w|\-)+):\/\/(?<conn>.*)";

	/// <summary>
	/// Defines a mapping of database type aliases to their corresponding DatabaseType enum values.
	/// </summary>
	private static readonly Dictionary<string, DatabaseType> _databaseTypeAlias = new()
	{
		{ "mssql", DatabaseType.SqlServer },
		{ "sqlserver", DatabaseType.SqlServer },
		{ "mysql", DatabaseType.MySql },
		{ "postgresql", DatabaseType.PostgreSql },
		{ "postgre", DatabaseType.PostgreSql },
		{ "pg", DatabaseType.PostgreSql },
		{ "pgsql", DatabaseType.PostgreSql },
		{ "postgres", DatabaseType.PostgreSql },
		{ "sqlite", DatabaseType.Sqlite },
		{ "mongodb", DatabaseType.MongoDb },
		{ "mongo", DatabaseType.MongoDb },
		{ "memory", DatabaseType.InMemory },
		{ "inmemory", DatabaseType.InMemory },
		{ "in-memory", DatabaseType.InMemory }
	};

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

		var databaseType = match.Groups["dbtype"].Value;
		var connection = match.Groups["conn"].Value;

		if (_databaseTypeAlias.TryGetValue(databaseType, out var dbType))
		{
			switch (dbType)
			{
				case DatabaseType.SqlServer:
					options.UseSqlServer(connection, builder =>
					{
						builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
						builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
					});
					break;
				//case DatabaseType.MySql:
				//	options.UseMySql(connection, ServerVersion.AutoDetect(connection), builder =>
				//	{
				//		builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
				//		builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
				//	});
				//	break;
				//case DatabaseType.PostgreSql:
				//	options.UseNpgsql(connection, builder =>
				//	{
				//		builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
				//		builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
				//	});
				//	break;
				case DatabaseType.Sqlite:
					options.UseSqlite(connection, builder =>
					{
						builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
					});
					break;
				//case DatabaseType.MongoDb:
				//	options.UseMongoDB(connection, "");
				//	break;
				case DatabaseType.InMemory:
					options.UseInMemoryDatabase(connection);
					break;
				default:
					throw new NotSupportedException("Unsupported database provider type: '{dbType}'");
			}
		}
		else
		{
			throw new ArgumentException("Unknown database type alias: '{databaseType}'");
		}

		if (seeding != null)
		{
			options.UseAsyncSeeding(seeding);
		}
	}
}
