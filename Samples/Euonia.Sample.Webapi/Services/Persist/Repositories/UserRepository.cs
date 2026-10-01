using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Sample.Persist.Entities;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Persist.Repositories;

internal class UserRepository(IContextProvider provider)
	: BaseRepository<SampleDataContext, UserEntity, string>(provider), IUserRepository
{
	public Task<List<UserEntity>> FindAsync(Expression<Func<UserEntity, bool>> predicate, string[] properties, int skip, int take, CancellationToken cancellationToken = default)
	{
		// 用 (predicate, handle, offset, count) 重载：基类先 BuildQuery（Where(predicate) + handle），
		// 再在外层 Skip/Take。若把 OrderBy/Skip/Take 都塞进 handle，基类是先 Take 后 Where，
		// 分页会先截断再过滤（如 admin 永远取不到——最新一行总是别人）。
		return FindAsync(predicate, Handle, skip, take, cancellationToken);

		IQueryable<UserEntity> Handle(IQueryable<UserEntity> query)
		{
			if (properties?.Length > 0)
			{
				query = properties.Aggregate(query, (current, property) => current.Include(property));
			}

			return query.OrderByDescending(t => t.CreatedAt);
		}
	}

	public async Task<List<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
	{
		return await Context.Set<UserRoleEntity>()
		                    .AsNoTracking()
		                    .Where(role => role.UserId == userId)
		                    .Select(role => role.Name)
		                    .ToListAsync(cancellationToken);
	}

	public async Task AddRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
	{
		var names = roles.Select(role => role.Trim().ToLowerInvariant())
		                 .Where(role => role.Length > 0)
		                 .Distinct()
		                 .ToArray();
		if (names.Length == 0)
		{
			return;
		}

		var existing = await Context.Set<UserRoleEntity>()
		                            .Where(role => role.UserId == userId && names.Contains(role.Name))
		                            .Select(role => role.Name)
		                            .ToListAsync(cancellationToken);
		await Context.Set<UserRoleEntity>()
		             .AddRangeAsync(
			             names.Except(existing)
			                  .Select(name =>
			                  {
				                  var role = UserRoleEntity.Create(name);
				                  role.UserId = userId;
				                  return role;
			                  })
			                  .ToArray(),
			             cancellationToken);
		await Context.SaveChangesAsync(cancellationToken);
	}

	public async Task RemoveRolesAsync(string userId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
	{
		var names = roles.Select(role => role.Trim().ToLowerInvariant())
		                 .Where(role => role.Length > 0)
		                 .Distinct()
		                 .ToArray();
		if (names.Length == 0)
		{
			return;
		}

		var matches = await Context.Set<UserRoleEntity>()
		                           .Where(role => role.UserId == userId && names.Contains(role.Name))
		                           .ToListAsync(cancellationToken);
		if (matches.Count > 0)
		{
			Context.Set<UserRoleEntity>().RemoveRange(matches);
			await Context.SaveChangesAsync(cancellationToken);
		}
	}
}
