namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// Enum representing different types of databases.
/// </summary>
internal enum DatabaseType
{
	/// <summary>
	/// Microsoft SQL Server
	/// </summary>
	SqlServer,

	/// <summary>
	/// MySQL
	/// </summary>
	MySql,

	/// <summary>
	/// PostgreSQL
	/// </summary>
	PostgreSql,

	/// <summary>
	/// SQLite
	/// </summary>
	Sqlite,

	/// <summary>
	/// MongoDB
	/// </summary>
	MongoDb,

	/// <summary>
	/// InMemory
	/// </summary>
	InMemory,
}
