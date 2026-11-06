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
	public string Id { get; set; } = null!;

	/// <summary>
	/// Gets or sets the nickname.
	/// </summary>
	public string Nickname { get; set; } = null!;

	/// <summary>
	/// Gets or sets the email address.
	/// </summary>
	public string Email { get; set; } = null!;

	/// <summary>
	/// Gets or sets the phone number.
	/// </summary>
	public string Phone { get; set; } = null!;
}