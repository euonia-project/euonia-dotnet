using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Sample.Domain.Dtos;

namespace Nerosoft.Euonia.Sample.Facade.Contracts;

/// <summary>
/// 账号应用服务契约：提供账号资料的读取、搜索、创建与自助维护操作。
/// </summary>
public interface IUserApplicationService : IApplicationService
{
	/// <summary>
	/// 按标识读取账号详情。
	/// </summary>
	/// <param name="id">账号标识。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>账号详情。</returns>
	Task<UserDetailDto> GetAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>
	/// 按关键字分页搜索账号。
	/// </summary>
	/// <param name="keyword">搜索关键字。</param>
	/// <param name="skip">跳过的记录数。</param>
	/// <param name="take">返回的最大记录数。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>账号摘要列表。</returns>
	Task<List<UserListDto>> FindAsync(string keyword, int skip, int take, CancellationToken cancellationToken = default);

	/// <summary>
	/// 创建账号并返回新账号标识。
	/// </summary>
	/// <param name="data">创建账号所需的入参。</param>
	/// <param name="cancellationToken">取消操作的令牌。</param>
	/// <returns>新账号的标识。</returns>
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