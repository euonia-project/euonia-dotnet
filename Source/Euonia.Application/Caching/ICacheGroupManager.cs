namespace Nerosoft.Euonia.Application;

/// <summary>
/// 维护缓存键到缓存组的索引，支持按组批量失效缓存。
/// </summary>
/// <remarks>
/// <see cref="CacheInterceptor"/> 写回缓存时通过 <see cref="Register"/> 登记键—组映射；
/// 数据变更方法标注 <see cref="CacheEvictAttribute"/> 后可经 <see cref="Evict"/> 一次性删除整组缓存键。
/// </remarks>
public interface ICacheGroupManager
{
	/// <summary>
	/// 将 <paramref name="key"/> 登记到指定的缓存组。
	/// </summary>
	/// <param name="key">缓存键。</param>
	/// <param name="groups">所属缓存组。</param>
	void Register(string key, IEnumerable<string> groups);

	/// <summary>
	/// 获取指定缓存组登记的全部缓存键。
	/// </summary>
	/// <param name="group">缓存组名。</param>
	/// <returns>该组的缓存键集合（可能为空）。</returns>
	IReadOnlyCollection<string> GetKeys(string group);

	/// <summary>
	/// 从全部组的索引中移除指定缓存键（不删除缓存本身，仅清理索引）。
	/// </summary>
	/// <param name="key">缓存键。</param>
	void Remove(string key);

	/// <summary>
	/// 失效指定缓存组：删除组内全部缓存键，并清空组索引。
	/// </summary>
	/// <param name="groups">需失效的缓存组。</param>
	/// <returns>被删除的缓存键数量。</returns>
	int Evict(IEnumerable<string> groups);
}