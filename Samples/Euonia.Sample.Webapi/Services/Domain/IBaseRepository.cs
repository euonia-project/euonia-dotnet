using System.Linq.Expressions;
using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// 定义实体的通用异步仓储操作。
/// </summary>
/// <typeparam name="TEntity">仓储所处理的实体类型。</typeparam>
/// <typeparam name="TKey">实体主键类型。</typeparam>
public interface IBaseRepository<TEntity, in TKey>
	where TEntity : class, IEntity<TKey>
	where TKey : IEquatable<TKey>
{
	/// <summary>
	/// 向底层数据存储插入一个新实体。
	/// </summary>
	/// <param name="entity">要插入的实体实例。不能为 <see langword="null"/>。</param>
	/// <param name="autoSave">
	/// 如果为 <see langword="true"/>，仓储会在插入操作之后把更改持久化到数据存储
	/// （例如调用 <c>SaveChangesAsync</c>）。
	/// 如果为 <see langword="false"/>，则由调用方负责保存更改。
	/// </param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>插入的实体实例，包含存储所应用的任何更改（例如生成的键）。</returns>
	Task<TEntity> InsertAsync(TEntity entity, bool autoSave, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新底层数据存储中的现有实体。
	/// </summary>
	/// <param name="entity">包含更新值的实体实例。不能为 <see langword="null"/>。</param>
	/// <param name="autoSave">
	/// 如果为 <see langword="true"/>，仓储会在更新操作之后把更改持久化到数据存储。
	/// 如果为 <see langword="false"/>，则由调用方负责保存更改。
	/// </param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	Task UpdateAsync(TEntity entity, bool autoSave, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按主键检索实体。
	/// </summary>
	/// <param name="id">要检索的实体的主键值。</param>
	/// <param name="tracking">
	/// 为 <see langword="true"/> 时，返回的实体将由底层上下文跟踪（便于后续更新）。
	/// 为 <see langword="false"/> 时，实体以不跟踪状态返回，适用于只读场景。
	/// </param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>找到时返回实体实例；否则取决于实现（通常是 <see langword="null"/> 或抛出异常）。</returns>
	Task<TEntity> GetAsync(TKey id, bool tracking, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按主键检索实体，并包含相关的导航属性。
	/// </summary>
	/// <param name="id">要检索的实体的主键值。</param>
	/// <param name="tracking">
	/// 为 <see langword="true"/> 时，返回的实体将由底层上下文跟踪。
	/// 为 <see langword="false"/> 时，实体以不跟踪状态返回，适用于只读场景。
	/// </param>
	/// <param name="properties">
	/// 要包含的相关属性名数组（例如导航属性）。实现应忽略未知的名称。
	/// 可用于立即加载相关数据。
	/// </param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>找到时返回包含所请求相关数据的实体实例；否则取决于实现。</returns>
	Task<TEntity> GetAsync(TKey id, bool tracking, string[] properties, CancellationToken cancellationToken = default);

	/// <summary>
	/// 判断是否存在满足指定谓词的实体。
	/// </summary>
	/// <param name="expression">用于测试实体的谓词。</param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>如果至少存在一个匹配的实体，则为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
	Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default);

	/// <summary>
	/// 统计匹配所提供谓词并应用可选查询修改的实体数量。
	/// </summary>
	/// <param name="expression">用于筛选实体的谓词。如果为 <see langword="null"/>，则统计全部实体。</param>
	/// <param name="handle">
	/// 一个委托，接收 <see cref="IQueryable{TEntity}"/> 并返回 <see cref="IQueryable{TEntity}"/>，
	/// 允许调用方附加额外的查询运算符（筛选、包含、投影）。
	/// </param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>匹配查询的实体数量。</returns>
	Task<int> CountAsync(Expression<Func<TEntity, bool>> expression, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 查找满足指定谓词的实体，并可对查询作额外定制。
	/// </summary>
	/// <param name="predicate">用于筛选实体的谓词。</param>
	/// <param name="handle">
	/// 用于定制查询的委托（例如添加包含、排序或额外筛选）。
	/// 该委托接收基础 <see cref="IQueryable{TEntity}"/> 并返回修改后的查询。
	/// </param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>匹配查询的实体列表。没有匹配项时返回空列表。</returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 查找满足指定谓词的实体，并支持分页与额外的查询定制。
	/// </summary>
	/// <param name="predicate">用于筛选实体的谓词。</param>
	/// <param name="handle">
	/// 用于定制查询的委托（例如添加包含、排序或额外筛选）。
	/// </param>
	/// <param name="offset">要返回的第一项从零开始的偏移量。</param>
	/// <param name="count">要返回的最大项数。</param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>表示所请求页面的实体列表。没有匹配项时返回空列表。</returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, int offset, int count, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除由给定主键标识的实体。
	/// </summary>
	/// <param name="id">要删除的实体的主键值。</param>
	/// <param name="autoSave">
	/// 如果为 <see langword="true"/>，仓储会在删除后持久化更改。
	/// 如果为 <see langword="false"/>，调用方必须手动保存更改。</param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <exception cref="NotFoundException">当不存在指定键的实体时抛出（取决于实现）。</exception>
	Task DeleteAsync(TKey id, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除所提供的实体实例。
	/// </summary>
	/// <param name="entity">要删除的实体实例。</param>
	/// <param name="autoSave">如果为 <see langword="true"/>，则在删除后持久化更改；否则调用方必须保存更改。</param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>表示异步删除操作的任务。</returns>
	Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 删除所提供的实体实例，并引发由 <paramref name="eventFactory"/> 生成的领域事件。
	/// </summary>
	/// <typeparam name="TEvent">删除后要生成并发布的领域事件类型。</typeparam>
	/// <param name="entity">要删除的实体实例。</param>
	/// <param name="eventFactory">一个工厂委托，用于生成要为此删除操作引发的领域事件。</param>
	/// <param name="autoSave">如果为 <see langword="true"/>，则在删除后持久化更改；否则调用方必须保存更改。</param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	Task DeleteAsync<TEvent>(TEntity entity, Func<TEvent> eventFactory, bool autoSave = true, CancellationToken cancellationToken = default)
		where TEvent : DomainEvent;

	/// <summary>
	/// 删除由给定键标识的实体，并引发由 <paramref name="eventFactory"/> 生成的领域事件。
	/// </summary>
	/// <typeparam name="TEvent">删除后要生成并发布的领域事件类型。</typeparam>
	/// <param name="id">要删除的实体的主键值。</param>
	/// <param name="eventFactory">一个工厂委托，用于生成要为此删除操作引发的领域事件。</param>
	/// <param name="autoSave">如果为 <see langword="true"/>，则在删除后持久化更改；否则调用方必须保存更改。</param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>表示异步删除操作的任务。</returns>
	Task DeleteAsync<TEvent>(TKey id, Func<TEvent> eventFactory, bool autoSave = true, CancellationToken cancellationToken = default)
		where TEvent : DomainEvent;

	/// <summary>
	/// 使用查询定制委托查找实体。
	/// </summary>
	/// <param name="handle">
	/// 一个委托，接收 <see cref="IQueryable{TEntity}"/> 并返回 <see cref="IQueryable{TEntity}"/>，
	/// 其中包含所需的修改（包含、排序、筛选、投影为实体）。
	/// </param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>定制查询返回的实体列表。未找到任何实体时返回空列表。</returns>
	Task<List<TEntity>> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 基于可定制的查询委托统计实体数量。
	/// </summary>
	/// <param name="handle">用于配置统计实体所用查询的委托。</param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>配置的查询所产生的实体数量。</returns>
	Task<int> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 执行预期返回单个实体的定制查询。
	/// </summary>
	/// <param name="handle">
	/// 一个委托，用于配置查询（例如添加筛选和包含），并且应返回单个实体。
	/// 实现应说明是返回第一个匹配的实体，还是在存在多个/没有结果时抛出异常。
	/// </param>
	/// <param name="cancellationToken">在等待任务完成期间观察的令牌。</param>
	/// <returns>配置的查询返回的单个实体；零个或多个结果时的行为取决于实现。</returns>
	Task<TEntity> GetAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);
}
