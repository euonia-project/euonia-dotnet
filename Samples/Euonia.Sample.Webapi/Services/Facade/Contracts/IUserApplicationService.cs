using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Sample.Domain.Dtos;

namespace Nerosoft.Euonia.Sample.Facade.Contracts;

public interface IUserApplicationService : IApplicationService
{
	Task<UserDetailDto> GetAsync(string id, CancellationToken cancellationToken = default);

	Task<List<UserListDto>> FindAsync(string keyword, int skip, int take, CancellationToken cancellationToken = default);

	Task<string> CreateAsync(UserCreateDto data, CancellationToken cancellationToken = default);

	/// <summary>
	/// 更新指定用户的资料（昵称 / 邮箱 / 手机号）。
	/// </summary>
	/// <param name="id">目标用户标识。</param>
	/// <param name="data">资料字段。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	Task UpdateAsync(string id, UserUpdateDto data, CancellationToken cancellationToken = default);

	/// <summary>
	/// 修改当前登录用户的密码。
	/// </summary>
	/// <param name="data">新旧密码。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	Task ChangePasswordAsync(UserChangePasswordDto data, CancellationToken cancellationToken = default);
}