using Nerosoft.Euonia.Sample.Domain.Dtos;

namespace Nerosoft.Euonia.Sample.Persist.Requests;

public record UserListQueryRequest(string Keyword) : PagedQueryRequest<UserListDto>;