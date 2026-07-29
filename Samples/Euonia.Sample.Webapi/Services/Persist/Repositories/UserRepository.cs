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
		return FindAsync(predicate, Handle, cancellationToken);

		IQueryable<UserEntity> Handle(IQueryable<UserEntity> query)
		{
			if (properties?.Length > 0)
			{
				query = properties.Aggregate(query, (current, property) => current.Include(property));
			}

			return query.OrderByDescending(t => t.CreatedAt)
						.Skip(skip)
						.Take(take);
		}
	}
}
