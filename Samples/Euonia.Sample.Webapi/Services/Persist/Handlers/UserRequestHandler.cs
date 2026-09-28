using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Sample.Domain.Dtos;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Persist.Entities;
using Nerosoft.Euonia.Sample.Persist.Requests;
using Nerosoft.Euonia.Sample.Persist.Specifications;

namespace Nerosoft.Euonia.Sample.Persist.Handlers;

internal class UserRequestHandler(IUserRepository repository)
	: IHandler<UserDetailQueryRequest, UserDetailDto>,
	  IHandler<UserListQueryRequest>
{
	public async Task<UserDetailDto> HandleAsync(UserDetailQueryRequest message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		// Roles 是集合导航属性，必须显式 Include，否则 AsNoTracking 查询拿不到角色。
		var entity = await repository.GetAsync(message.Id, false, [nameof(UserEntity.Roles)], cancellationToken);
		if (entity == null)
		{
			throw new NotFoundException($"User with ID '{message.Id}' not found.");
		}

		return new UserDetailDto
		{
			Id = entity.Id,
			Username = entity.Username,
			Nickname = entity.Nickname,
			Email = entity.Email,
			Phone = entity.Phone,
			PasswordChangedTime = entity.PasswordChangedTime,
			CreatedAt = entity.CreatedAt,
			UpdatedAt = entity.UpdatedAt,
			Roles = entity.Roles?.Select(role => role.Name).ToArray() ?? []
		};
	}

	public Task HandleAsync(UserListQueryRequest message, IMessageContext context, CancellationToken cancellationToken = default)
	{
		var specification = UserSpecification.All;

		if (!string.IsNullOrWhiteSpace(message.Keyword))
		{
			specification &= UserSpecification.Matches(message.Keyword);
		}

		var predicate = specification.Satisfy();

		return repository.FindAsync(predicate, [], message.Skip, message.Take, cancellationToken)
		                 .ContinueWith(task =>
		                 {
			                 var dtos = TypeAdapter.ProjectedAs<List<UserListDto>>(task.Result);
			                 context.Response(dtos);
		                 }, cancellationToken);
	}
}