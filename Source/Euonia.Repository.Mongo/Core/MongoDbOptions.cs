namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// The mongo database context options.
/// </summary>
public class MongoDbOptions
{
    /// <summary>
    /// Gets the connection string.
    /// </summary>
    public string ConnectionString { get; private set; } = null!;

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string Database { get; private set; } = null!;

    /// <summary>
    /// Gets the specified connection.
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public MongoDbOptions UseConnection(string connection)
    {
        ConnectionString = connection;
        return this;
    }

    /// <summary>
    /// Gets the specified database.
    /// </summary>
    /// <param name="database"></param>
    /// <returns></returns>
    public MongoDbOptions UseDatabase(string database)
    {
        Database = database;
        return this;
    }
}