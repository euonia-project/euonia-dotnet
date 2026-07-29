namespace Nerosoft.Euonia.Sample.Domain.Dtos;

public class UserListDto
{
	/// <summary>
	/// Unique identifier of the user.
	/// </summary>
	public string Username { get; set; }

	/// <summary>
	/// Nickname of the user.
	/// </summary>
	public string Nickname { get; set; }

	/// <summary>
	/// Email address of the user.
	/// </summary>
	public string Email { get; set; }

	/// <summary>
	/// Phone number of the user.
	/// </summary>
	public string Phone { get; set; }
}
