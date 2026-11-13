namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// 表示不同数据库类型的枚举。
/// </summary>
internal enum DatabaseType
{
	/// <summary>
	/// Microsoft SQL Server 数据库。
	/// </summary>
	SqlServer,

	/// <summary>
	/// MySQL 数据库。
	/// </summary>
	MySql,

	/// <summary>
	/// PostgreSQL 数据库。
	/// </summary>
	PostgreSql,

	/// <summary>
	/// SQLite 数据库。
	/// </summary>
	Sqlite,

	/// <summary>
	/// MongoDB 数据库。
	/// </summary>
	MongoDb,

	/// <summary>
	/// 内存数据库（主要用于测试）。
	/// </summary>
	InMemory,
}
