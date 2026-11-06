using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Sample.Constants;

namespace Nerosoft.Euonia.Sample.Persist.Entities;

/// <summary>
/// 表示分配给用户的角色。
/// </summary>
public class UserRoleEntity : Entity<string>, IHasCreateTime
{
	/// <summary>
	/// 初始化 <see cref="UserRoleEntity"/> 类的新实例。
	/// 此构造函数为私有，以确保实例只能受控创建。
	/// </summary>
	private UserRoleEntity()
	{
	}

	/// <summary>
	/// 使用指定名称初始化 <see cref="UserRoleEntity"/> 类的新实例。
	/// </summary>
	/// <param name="name">角色的名称。</param>
	private UserRoleEntity(string name)
		: this()
	{
		Name = name;
	}

	/// <summary>
	/// 获取或设置角色的名称。
	/// </summary>
	public string Name { get; set; } = null!;

	/// <summary>
	/// 获取或设置与此角色关联的用户 ID。
	/// </summary>
	public string UserId { get; set; } = null!;

	/// <summary>
	/// 获取或设置角色的创建时间。
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// 获取或设置与此角色关联的用户。
	/// </summary>
	public UserEntity UserEntity { get; set; } = null!;

	/// <summary>
	/// 使用指定名称创建 <see cref="UserRoleEntity"/> 类的新实例。
	/// </summary>
	/// <param name="name">角色的名称。</param>
	/// <returns><see cref="UserRoleEntity"/> 类的新实例。</returns>
	internal static UserRoleEntity Create(string name)
	{
		name = name.Trim().ToLowerInvariant();
		if (!RoleName.All.Contains(name))
		{
			throw new ArgumentException($"Invalid role name '{name}'", nameof(name));
		}

		{
		}

		return new UserRoleEntity(name);
	}
}