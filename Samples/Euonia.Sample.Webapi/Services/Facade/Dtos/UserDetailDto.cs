namespace Nerosoft.Euonia.Sample.Domain.Dtos;

/// <summary>
/// User detail (read model).
/// </summary>
public class UserDetailDto
{
	/// <summary>
	/// Gets or sets the unique identifier of the user.
	/// </summary>
	public string Id { get; set; }

	/// <summary>
	/// Gets or sets the login username.
	/// </summary>
	public string Username { get; set; }

	/// <summary>
	/// Gets or sets the nickname of the user.
	/// </summary>
	public string Nickname { get; set; }

	/// <summary>
	/// Gets or sets the email address of the user.
	/// </summary>
	public string Email { get; set; }

	/// <summary>
	/// Gets or sets the phone number of the user.
	/// </summary>
	public string Phone { get; set; }

	/// <summary>
	/// Gets or sets the time when the password was last changed.
	/// </summary>
	public DateTime? PasswordChangedTime { get; set; }

	/// <summary>
	/// Gets or sets the creation time.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the last update time.
	/// </summary>
	public DateTime UpdatedAt { get; set; }

	/// <summary>
	/// Gets or sets the role names granted to the user.
	/// </summary>
	public string[] Roles { get; set; }
}