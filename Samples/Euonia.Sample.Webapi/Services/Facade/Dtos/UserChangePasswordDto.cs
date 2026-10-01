namespace Nerosoft.Euonia.Sample.Domain.Dtos;

/// <summary>
/// Password change input for the current user.
/// </summary>
public class UserChangePasswordDto
{
	/// <summary>
	/// Gets or sets the current password, used to prove ownership of the account.
	/// </summary>
	public string OldPassword { get; set; }

	/// <summary>
	/// Gets or sets the new password.
	/// </summary>
	public string NewPassword { get; set; }
}