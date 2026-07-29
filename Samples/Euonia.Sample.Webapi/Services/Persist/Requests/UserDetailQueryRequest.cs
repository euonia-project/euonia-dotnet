using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Sample.Domain.Dtos;

namespace Nerosoft.Euonia.Sample.Persist.Requests;

public record UserDetailQueryRequest(string Id) : IRequest<UserDetailDto>;
