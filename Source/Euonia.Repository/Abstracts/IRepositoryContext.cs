using System.Data;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 表示仓储上下文，用于统一封装底层数据访问技术的实体集合、连接、事务与变更提交能力。
/// </summary>
/// <remarks>
/// 该接口为不同数据访问实现（如 EF Core、MongoDB 等）提供统一抽象，
/// 通常借助依赖注入以 <see cref="IDisposable"/> 的生命周期方式使用。
/// </remarks>
public interface IRepositoryContext : IDisposable
{
	/// <summary>
	/// 获取当前上下文的唯一标识。
	/// </summary>
	/// <value>上下文实例的标识。</value>
	Guid Id { get; }

	/// <summary>
	/// 获取当前上下文所使用的数据提供程序名称。
	/// </summary>
	/// <value>数据提供程序的名称，例如 <c>MongoDB</c> 或 EF Core 的提供程序名。</value>
	string Provider { get; }

	/// <summary>
	/// 创建可用于查询和保存 <typeparamref name="TEntity" /> 实例的 <see cref="IQueryable{T}" />。
	/// </summary>
	/// <typeparam name="TEntity">需要返回实体集合的实体类型。</typeparam>
	/// <returns>指定实体类型对应的实体集合。</returns>
	IQueryable<TEntity> SetOf<TEntity>() where TEntity : class;

	/// <summary>
	/// 获取当前上下文所使用的数据库连接。
	/// </summary>
	/// <returns>底层数据库连接。</returns>
	/// <exception cref="NotSupportedException">当底层数据提供程序不支持关系型连接时抛出。</exception>
	IDbConnection GetConnection();

	/// <summary>
	/// 获取当前上下文中正在进行的数据库事务。
	/// </summary>
	/// <returns>当前事务；若没有活动事务则为 <c>null</c>。</returns>
	/// <exception cref="NotSupportedException">当底层数据提供程序不支持关系型事务时抛出。</exception>
	IDbTransaction GetTransaction();

	/// <summary>
	/// 以异步方式将上下文中的所有变更保存到数据存储。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务，其结果为实现保存而写入的记录数。</returns>
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 以异步方式提交当前事务。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	Task CommitAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 以异步方式回滚当前事务。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	Task RollbackAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 获取当前上下文中被跟踪的实体对象。
	/// </summary>
	/// <returns>被跟踪的实体对象序列；若实现不支持变更跟踪则返回空集合。</returns>
	IEnumerable<object> GetTrackedEntries();
}