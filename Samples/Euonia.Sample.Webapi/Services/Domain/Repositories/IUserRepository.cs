using System.Linq.Expressions;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Domain.Repositories;

public interface IUserRepository
{
	Task<UserEntity> InsertAsync(UserEntity entity, bool autoSave, CancellationToken cancellationToken = default);

	Task UpdateAsync(UserEntity entity, bool autoSave, CancellationToken cancellationToken = default);

	Task<UserEntity> GetAsync(string id, bool tracking, CancellationToken cancellationToken = default);

	Task<UserEntity> GetAsync(string id, bool tracking, string[] properties, CancellationToken cancellationToken = default);

	/// <summary>
	/// Check if any user exists with the given expression.
	/// </summary>
	/// <param name="expression"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	Task<bool> AnyAsync(Expression<Func<UserEntity, bool>> expression, CancellationToken cancellationToken = default);

	Task<List<UserEntity>> FindAsync(Expression<Func<UserEntity, bool>> predicate, Func<IQueryable<UserEntity>, IQueryable<UserEntity>> handle, CancellationToken cancellationToken = default);

	Task<List<UserEntity>> FindAsync(Expression<Func<UserEntity, bool>> predicate, string[] properties, int skip, int take, CancellationToken cancellationToken = default);
}
