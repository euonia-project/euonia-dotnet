using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Sample.Constants;

namespace Nerosoft.Euonia.Sample.Persist.Entities;

/// <summary>
/// Represents a role assigned to a user.
/// </summary>
public class UserRoleEntity : Entity<string>, IHasCreateTime
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UserRoleEntity"/> class.
	/// This constructor is private to enforce controlled creation of instances.
	/// </summary>
	private UserRoleEntity()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="UserRoleEntity"/> class with the specified name.
	/// </summary>
	/// <param name="name">The name of the role.</param>
	private UserRoleEntity(string name)
		: this()
	{
		Name = name;
	}

	/// <summary>
	/// Gets or sets the name of the role.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Gets or sets the ID of the user associated with this role.
	/// </summary>
	public string UserId { get; set; }

	/// <summary>
	/// Gets or sets the creation time of the role.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the user associated with this role.
	/// </summary>
	public UserEntity UserEntity { get; set; }

	/// <summary>
	/// Creates a new instance of the <see cref="UserRoleEntity"/> class with the specified name.
	/// </summary>
	/// <param name="name">The name of the role.</param>
	/// <returns>A new instance of the <see cref="UserRoleEntity"/> class.</returns>
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