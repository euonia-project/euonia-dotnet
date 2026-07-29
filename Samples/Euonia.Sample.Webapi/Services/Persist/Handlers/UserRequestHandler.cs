using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Sample.Domain.Dtos;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Persist.Requests;
using Nerosoft.Euonia.Sample.Persist.Specifications;

namespace Nerosoft.Euonia.Sample.Persist.Handlers;

internal class UserRequestHandler(IUserRepository repository)
	: IHandler<UserDetailQueryRequest, UserDetailDto>,
	IHandler<UserListQueryRequest>
{
	public async Task<UserDetailDto> HandleAsync(UserDetailQueryRequest message, MessageContext context, CancellationToken cancellationToken = default)
	{
		var entity = await repository.GetAsync(message.Id, false, cancellationToken);
		return TypeAdapter.ProjectedAs<UserDetailDto>(entity);
	}

	public Task HandleAsync(UserListQueryRequest message, MessageContext context, CancellationToken cancellationToken = default)
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
