using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Sample.Domain.Dtos;

namespace Nerosoft.Euonia.Sample.Persist.Requests;

/// <summary>
/// 按用户标识查询用户详情的请求。
/// </summary>
/// <param name="Id">要查询的用户唯一标识。</param>
public record UserDetailQueryRequest(string Id) : IRequest<UserDetailDto>;
