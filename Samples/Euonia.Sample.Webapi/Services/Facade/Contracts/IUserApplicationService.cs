using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Sample.Domain.Dtos;

namespace Nerosoft.Euonia.Sample.Facade.Contracts;

public interface IUserApplicationService : IApplicationService
{
	Task<UserDetailDto> GetAsync(string id, CancellationToken cancellationToken = default);

	Task<List<UserListDto>> FindAsync(string keyword, int skip, int take, CancellationToken cancellationToken = default);

	Task<string> CreateAsync(UserCreateDto data, CancellationToken cancellationToken = default);
}
