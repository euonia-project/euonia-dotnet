using System.Linq.Expressions;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Domain.Repositories;

/// <summary>
/// 账号数据仓库的扩展写入与读取接口，供应用服务按标识访问账号实体。
/// </summary>
public interface IUserRepository
{
	/// <summary>
	/// 插入账号实体，并按 <paramref name="autoSave"/> 决定是否立即提交。
	/// </summary>
	/// <param name="entity">要插入的账号实体。</param>
	/// <param name="autoSave">是否立即保存更改。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>插入后的账号实体。</returns>
	Task<UserEntity> InsertAsync(UserEntity entity, bool autoSave, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新账号实体，并按 <paramref name="autoSave"/> 决定是否立即提交。
	/// </summary>
	/// <param name="entity">要更新的账号实体。</param>
	/// <param name="autoSave">是否立即保存更改。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>表示异步更新操作的任务。</returns>
	Task UpdateAsync(UserEntity entity, bool autoSave, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按标识读取账号实体。
	/// </summary>
	/// <param name="id">账号标识。</param>
	/// <param name="tracking">是否启用变更跟踪。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>账号实体；不存在时为 <see langword="null"/>。</returns>
	Task<UserEntity?> GetAsync(string id, bool tracking, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按标识读取账号实体，并只加载指定的导航属性。
	/// </summary>
	/// <param name="id">账号标识。</param>
	/// <param name="tracking">是否启用变更跟踪。</param>
	/// <param name="properties">需要一并加载的导航属性名。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>账号实体；不存在时为 <see langword="null"/>。</returns>
	Task<UserEntity?> GetAsync(string id, bool tracking, string[] properties, CancellationToken cancellationToken = default);

	/// <summary>
	/// 检查是否存在满足给定表达式的用户。
	/// </summary>
	/// <param name="expression">判断用户是否存在的条件表达式。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>存在满足条件的用户时为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
	Task<bool> AnyAsync(Expression<Func<UserEntity, bool>> expression, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按谓词查询账号，并允许调用方自定义后续的查询组合。
	/// </summary>
	/// <param name="predicate">筛选账号的条件表达式。</param>
	/// <param name="handle">对查询结果进一步投影或排序的委托。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>满足条件的账号列表。</returns>
	Task<List<UserEntity>> FindAsync(Expression<Func<UserEntity, bool>> predicate, Func<IQueryable<UserEntity>, IQueryable<UserEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按谓词分页查询账号，并只加载指定的导航属性。
	/// </summary>
	/// <param name="predicate">筛选账号的条件表达式。</param>
	/// <param name="properties">需要一并加载的导航属性名。</param>
	/// <param name="skip">跳过的记录数。</param>
	/// <param name="take">返回的最大记录数。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>满足条件的分页账号列表。</returns>
	Task<List<UserEntity>> FindAsync(Expression<Func<UserEntity, bool>> predicate, string[] properties, int skip, int take, CancellationToken cancellationToken = default);

	/// <summary>
	/// 读取账号的角色名列表。
	/// </summary>
	Task<List<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// 为一个存在的账号追加角色（<see cref="UserRoleEntity.Create"/> 校验角色名合法性）。
	/// </summary>
	Task AddRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default);

	/// <summary>
	/// 移除账号的角色。
	/// </summary>
	Task RemoveRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default);
}
