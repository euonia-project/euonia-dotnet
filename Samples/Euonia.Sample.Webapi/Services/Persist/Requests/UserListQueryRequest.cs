using Nerosoft.Euonia.Sample.Domain.Dtos;

namespace Nerosoft.Euonia.Sample.Persist.Requests;

/// <summary>
/// 按关键字分页查询用户列表的请求。
/// </summary>
/// <param name="Keyword">用于匹配用户名、昵称、邮箱地址或手机号码的关键字。</param>
public record UserListQueryRequest(string Keyword) : PagedQueryRequest<UserListDto>;