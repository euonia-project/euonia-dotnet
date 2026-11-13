using System.Data;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// MongoDB 的 <see cref="IRepositoryContext"/> 抽象实现。
/// </summary>
/// <typeparam name="TContext">MongoDB 数据上下文的类型。</typeparam>
public abstract class DataContextBase<TContext> : MongoDbContext, IRepositoryContext
	where TContext : MongoDbContext, IRepositoryContext
{
	private readonly ILogger<TContext> _logger;

	/// <summary>
	/// 使用数据库和日志记录器初始化 <see cref="DataContextBase{TContext}"/> 的新实例。
	/// </summary>
	/// <param name="database">要使用的 MongoDB 数据库。</param>
	/// <param name="logger">用于创建数据上下文日志记录器的日志工厂。</param>
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
	/// 获取一个值，指示是否启用事件发布。
	/// </summary>
	protected abstract bool EnabledPublishEvents { get; }

	/// <summary>
	/// 获取会话句柄。
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
	/// 获取数据提供程序名称。
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
	public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
	{
		Session.StartTransaction();
		return Task.CompletedTask;
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