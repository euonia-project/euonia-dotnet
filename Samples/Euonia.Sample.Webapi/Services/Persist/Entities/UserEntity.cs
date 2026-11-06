using Nerosoft.Euonia.Repository;

namespace Nerosoft.Euonia.Sample.Persist.Entities;

/// <summary>
/// 表示领域中的用户聚合根。
/// </summary>
public sealed class UserEntity : Entity<string>, IHasCreateTime, IHasUpdateTime, ITombstone
{
	/// <summary>
	/// 初始化 <see cref="UserEntity"/> 类的新实例。
	/// </summary>
	/// <remarks>
	/// 此构造函数为 Entity Framework 所必需，不应直接使用。
	/// </remarks>
	private UserEntity()
	{
	}

	/// <summary>
	/// 使用给定的凭据创建用户账户。调用方负责将其持久化。
	/// </summary>
	/// <param name="userId">用户标识。</param>
	/// <param name="username">登录用户名。</param>
	/// <param name="nickname">显示名。</param>
	/// <param name="passwordHash">加盐密码的哈希值（见 <c>Cryptography.SHA.Encrypt(salt + password)</c>）。</param>
	/// <param name="passwordSalt">对密码进行哈希时使用的盐。</param>
	/// <returns>新的用户账户实例。</returns>
	internal static UserEntity Create(string userId, string username, string nickname, string passwordHash, string passwordSalt)
	{
		return new UserEntity
		{
			Id = userId,
			Username = username,
			Nickname = nickname,
			PasswordHash = passwordHash,
			PasswordSalt = passwordSalt,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
	}

	/// <summary>
	/// 获取或设置用户名。
	/// </summary>
	public string Username { get; set; } = null!;

	/// <summary>
	/// 获取或设置加密后的密码。
	/// </summary>
	public string PasswordHash { get; set; } = null!;

	/// <summary>
	/// 获取或设置用于加密密码的盐。
	/// </summary>
	public string PasswordSalt { get; set; } = null!;

	/// <summary>
	/// 获取或设置昵称。
	/// </summary>
	public string Nickname { get; set; } = null!;

	/// <summary>
	/// 获取或设置电子邮件地址。
	/// </summary>
	public string Email { get; set; } = null!;

	/// <summary>
	/// 获取或设置电话号码。
	/// </summary>
	public string Phone { get; set; } = null!;

	/// <summary>
	/// 获取或设置访问失败次数。
	/// </summary>
	public int AccessFailedCount { get; set; }

	/// <summary>
	/// 获取或设置锁定结束时间。
	/// </summary>
	public DateTime? LockoutEnd { get; set; }

	/// <summary>
	/// 获取或设置创建时间。
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// 获取或设置更新时间。
	/// </summary>
	public DateTime UpdatedAt { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示此实例是否已删除。
	/// </summary>
	public bool IsDeleted { get; set; }

	/// <summary>
	/// 获取或设置删除时间。
	/// </summary>
	public DateTime? DeletedAt { get; set; }

	/// <summary>
	/// 获取或设置密码最后更改的时间。
	/// </summary>
	public DateTime? PasswordChangedTime { get; set; }

	/// <summary>
	/// 获取或设置与用户关联的角色。
	/// </summary>
	public HashSet<UserRoleEntity> Roles { get; set; } = null!;

	/// <summary>
	/// 设置用户的电子邮件地址。
	/// </summary>
	/// <param name="email">要设置的电子邮件地址。</param>
	internal void SetEmail(string email)
	{
		Email = email.Normalize(TextCaseType.Lower);
	}

	/// <summary>
	/// 设置用户的电话号码。
	/// </summary>
	/// <param name="phone">要设置的电话号码。</param>
	internal void SetPhone(string phone)
	{
		Phone = phone;
	}

	/// <summary>
	/// 设置用户的昵称。
	/// </summary>
	/// <param name="nickname">要设置的昵称。</param>
	internal void SetNickname(string nickname)
	{
		Nickname = nickname;
	}

	/// <summary>
	/// 增加访问失败次数。
	/// </summary>
	internal void IncreaseAccessFailedCount()
	{
		AccessFailedCount++;
		if (AccessFailedCount >= 10)
		{
			LockoutEnd = DateTime.Now.AddMinutes(30);
		}
	}

	/// <summary>
	/// 重置访问失败次数。
	/// </summary>
	internal void ResetAccessFailedCount()
	{
		AccessFailedCount = 0;
		LockoutEnd = null;
	}

	

	/// <summary>
	/// 设置用户的锁定结束时间。
	/// </summary>
	/// <param name="until">锁定结束时间。</param>
	internal void SetLockoutEnd(DateTime? until)
	{
		LockoutEnd = until;
	}
}