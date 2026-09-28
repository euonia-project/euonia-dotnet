namespace Nerosoft.Euonia.Sample.Domain.Dtos;

/// <summary>
/// User profile update input (nickname / email / phone).
/// </summary>
public class UserUpdateDto
{
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
}