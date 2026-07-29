using Nerosoft.Euonia.Repository;

namespace Nerosoft.Euonia.Sample.Persist.Entities;

/// <summary>
/// Represents a user aggregate in the domain.
/// </summary>
public sealed class UserEntity : Entity<string>, IHasCreateTime, IHasUpdateTime, ITombstone
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UserEntity"/> class.
	/// </summary>
	/// <remarks>
	/// The constructor is required by Entity Framework, and should not be used directly.
	/// </remarks>
	private UserEntity()
	{
	}

	/// <summary>
	/// Gets or sets the username.
	/// </summary>
	public string Username { get; set; }

	/// <summary>
	/// Gets or sets the encrypted password.
	/// </summary>
	public string PasswordHash { get; set; }

	/// <summary>
	/// Gets or sets the salt used to encrypt the password.
	/// </summary>
	public string PasswordSalt { get; set; }

	/// <summary>
	/// Gets or sets the nickname.
	/// </summary>
	public string Nickname { get; set; }

	/// <summary>
	/// Gets or sets the email address.
	/// </summary>
	public string Email { get; set; }

	/// <summary>
	/// Gets or sets the phone number.
	/// </summary>
	public string Phone { get; set; }

	/// <summary>
	/// Gets or sets the count of failed access attempts.
	/// </summary>
	public int AccessFailedCount { get; set; }

	/// <summary>
	/// Gets or sets the lockout end time.
	/// </summary>
	public DateTime? LockoutEnd { get; set; }

	/// <summary>
	/// Gets or sets the creation time.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the update time.
	/// </summary>
	public DateTime UpdatedAt { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this instance is deleted.
	/// </summary>
	public bool IsDeleted { get; set; }

	/// <summary>
	/// Gets or sets the delete time.
	/// </summary>
	public DateTime? DeletedAt { get; set; }

	/// <summary>
	/// Gets or sets the time when the password was last changed.
	/// </summary>
	public DateTime? PasswordChangedTime { get; set; }

	/// <summary>
	/// Gets or sets the roles associated with the user.
	/// </summary>
	public HashSet<UserRoleEntity> Roles { get; set; }

	/// <summary>
	/// Sets the email address for the user.
	/// </summary>
	/// <param name="email">The email address to set.</param>
	internal void SetEmail(string email)
	{
		Email = email.Normalize(TextCaseType.Lower);
	}

	/// <summary>
	/// Sets the phone number for the user.
	/// </summary>
	/// <param name="phone">The phone number to set.</param>
	internal void SetPhone(string phone)
	{
		Phone = phone;
	}

	/// <summary>
	/// Sets the nickname for the user.
	/// </summary>
	/// <param name="nickname">The nickname to set.</param>
	internal void SetNickname(string nickname)
	{
		Nickname = nickname;
	}

	/// <summary>
	/// Increases the count of failed access attempts.
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
	/// Resets the count of failed access attempts.
	/// </summary>
	internal void ResetAccessFailedCount()
	{
		AccessFailedCount = 0;
		LockoutEnd = null;
	}

	

	/// <summary>
	/// Sets the lockout end time for the user.
	/// </summary>
	/// <param name="until">The lockout end time.</param>
	internal void SetLockoutEnd(DateTime? until)
	{
		LockoutEnd = until;
	}
}