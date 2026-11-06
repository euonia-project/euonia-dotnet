using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// 示例应用持久化层的辅助类型，负责把 <c>dbtype://connection</c> 形式的连接字符串解析为对应的 EF Core 数据库提供程序。
/// </summary>
internal static class ServiceCollectionExtensions
{
	private const string CONNECTION_STRING_PATTERN = @"^(?<dbtype>(?:\w|\-)+):\/\/(?<conn>.*)";

	/// <summary>
	/// 定义数据库类型别名到对应 DatabaseType 枚举值的映射。
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
	/// 根据提供的连接字符串名称配置数据上下文。
	/// </summary>
	/// <param name="connectionString">形如 <c>dbtype://connection</c> 的连接字符串，不能为 <see langword="null"/> 或空白。</param>
	/// <param name="provider">服务提供程序；当前实现未使用该参数。</param>
	/// <param name="options">要配置的 EF Core 数据上下文选项构建器。</param>
	/// <param name="seeding">可选的异步初始化委托，接收数据上下文、是否种子阶段与取消令牌。</param>
	/// <exception cref="ArgumentException">连接字符串为空、格式无效或数据库类型别名未知时抛出。</exception>
	/// <exception cref="NotSupportedException">连接字符串中的数据库类型不受支持时抛出。</exception>
	private static void ConfigureDataContext(string connectionString, IServiceProvider provider, DbContextOptionsBuilder options, Func<DbContext, bool, CancellationToken, Task>? seeding = null)
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
