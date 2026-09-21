using System.Data;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// The <see cref="IRepositoryContext"/> abstract implementation for MongoDB.
/// </summary>
/// <typeparam name="TContext"></typeparam>
public abstract class DataContextBase<TContext> : MongoDbContext, IRepositoryContext
	where TContext : MongoDbContext, IRepositoryContext
{
	private readonly ILogger<TContext> _logger;

	/// <summary>
	/// Initialize a new instance of <see cref="DataContextBase{TContext}"/> with database and logger.
	/// </summary>
	/// <param name="database"></param>
	/// <param name="logger"></param>
	protected DataContextBase(IMongoDatabase database, ILoggerFactory logger)
		: base(database)
	{
		Session = Database.Client.StartSession();
		_logger = logger.CreateLogger<TContext>();
		//using (var cursor = database.Watch())
		//{
		//    foreach (var change in cursor.ToEnumerable())
		//    {
		//        change.
		//    }
		//}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Session.Dispose();
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Gets a value indicating whether the publish events is enabled.
	/// </summary>
	protected abstract bool EnabledPublishEvents { get; }

	/// <summary>
	/// Gets the session handle.
	/// </summary>
	protected IClientSessionHandle Session { get; }

	/// <summary>
	/// 获取当前上下文实例的唯一标识。
	/// </summary>
	/// <remarks>
	/// 此前该属性返回 <c>Guid.NewGuid()</c>，每次读取都是一个新的值，
	/// 任何以它为键、为日志关联标识或为幂等依据的逻辑都失效。现改为实例创建时固定一次。
	/// </remarks>
	public Guid Id { get; } = Guid.NewGuid();

	/// <summary>
	/// 
	/// </summary>
	public virtual string Provider => "MongoDB";

	/// <inheritdoc />
	public IQueryable<TEntity> SetOf<TEntity>()
		where TEntity : class
	{
		return Collection<TEntity>().AsQueryable();
	}

	/// <inheritdoc />
	public IDbConnection GetConnection()
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public IDbTransaction GetTransaction()
	{
		throw new NotSupportedException();
	}

	/// <inheritdoc />
	public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return await Task.FromResult(0);
	}

	/// <inheritdoc />
	public Task CommitAsync(CancellationToken cancellationToken = default)
	{
		return Session.CommitTransactionAsync(cancellationToken);
	}

	/// <inheritdoc />
	public Task RollbackAsync(CancellationToken cancellationToken = default)
	{
		return Session.AbortTransactionAsync(cancellationToken);
	}

	/// <inheritdoc />
	public virtual IEnumerable<object> GetTrackedEntries()
	{
		return new List<object>();
	}
}