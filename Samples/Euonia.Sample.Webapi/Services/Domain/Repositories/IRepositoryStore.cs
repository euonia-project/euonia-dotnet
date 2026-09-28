using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Domain.Repositories;

/// <summary>
/// 代码仓库数据的内存仓储。生产环境应换用持久化仓储；此处用内存数据便于演示，
/// 仓储只负责存取，权限判定全部由权限引擎完成。
/// </summary>
public interface IRepositoryStore
{
	/// <summary>返回全部仓库的数据源（供读侧下推使用）。</summary>
	IQueryable<CodeRepository> Query();

	/// <summary>按标识获取仓库；不存在时返回 <see langword="null"/>。</summary>
	Task<CodeRepository> GetAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>追加一个新仓库。</summary>
	Task AddAsync(CodeRepository repository, CancellationToken cancellationToken = default);

	/// <summary>替换标识相同的仓库数据。</summary>
	Task UpdateAsync(CodeRepository repository, CancellationToken cancellationToken = default);

	/// <summary>删除标识对应的仓库。</summary>
	Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}