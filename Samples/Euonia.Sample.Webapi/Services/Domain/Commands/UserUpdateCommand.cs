using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// Update user profile command (nickname / email / phone).
/// </summary>
public class UserUpdateCommand : Command
{
	/// <summary>
	/// Gets or sets the identifier of the user to update.
	/// </summary>
	public string Id { get; set; }

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