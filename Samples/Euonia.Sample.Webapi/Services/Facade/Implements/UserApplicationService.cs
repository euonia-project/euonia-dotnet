using System.Reactive.Subjects;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Sample.Domain.Commands;
using Nerosoft.Euonia.Sample.Domain.Dtos;
using Nerosoft.Euonia.Sample.Facade.Contracts;
using Nerosoft.Euonia.Sample.Persist.Requests;

namespace Nerosoft.Euonia.Sample.Facade.Implements;

internal class UserApplicationService : BaseApplicationService, IUserApplicationService
{
	public async Task<UserDetailDto> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		var request = new UserDetailQueryRequest(id);
		return await Bus.CallAsync(request, null, cancellationToken);
	}

	public Task<List<UserListDto>> FindAsync(string keyword, int skip, int take, CancellationToken cancellationToken = default)
	{
		var request = new UserListQueryRequest(keyword) { Skip = skip, Take = take };
		return Bus.CallAsync(request, null, cancellationToken);
	}

	public async Task<string> CreateAsync(UserCreateDto data, CancellationToken cancellationToken = default)
	{
		var command = TypeAdapter.ProjectedAs<UserCreateCommand>(data);
		var tcs = new TaskCompletionSource<string>();

		var subject = new Subject<string>();
		subject.Subscribe
		(
			id => tcs.SetResult(id),
			ex => tcs.SetException(ex),
			() => tcs.TrySetResult(null)
		);

		await Bus.SendAsync(command, subject, cancellationToken);
		return await tcs.Task;
	}
}