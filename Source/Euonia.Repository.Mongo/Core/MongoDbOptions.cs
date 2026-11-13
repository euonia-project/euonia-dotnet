namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// Mongo 数据库上下文选项。
/// </summary>
public class MongoDbOptions
{
    /// <summary>
    /// 获取连接字符串。
    /// </summary>
    public string ConnectionString { get; private set; } = null!;

    /// <summary>
    /// 获取数据库名称。
    /// </summary>
    public string Database { get; private set; } = null!;

    /// <summary>
    /// 获取指定的连接。
    /// </summary>
    /// <param name="connection">MongoDB 连接字符串。</param>
    /// <returns>当前选项实例，以支持链式调用。</returns>
    public MongoDbOptions UseConnection(string connection)
    {
        ConnectionString = connection;
        return this;
    }

    /// <summary>
    /// 获取指定的数据库。
    /// </summary>
    /// <param name="database">数据库名称。</param>
    /// <returns>当前选项实例，以支持链式调用。</returns>
    public MongoDbOptions UseDatabase(string database)
    {
        Database = database;
        return this;
    }
}