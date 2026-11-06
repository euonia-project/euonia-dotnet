namespace Nerosoft.Euonia.Sample.Domain.Dtos;

public class UserListDto
{
	/// <summary>
	/// Unique identifier of the user.
	/// </summary>
	public string Id { get; set; } = null!;

	/// <summary>
	/// Unique identifier of the user.
	/// </summary>
	public string Username { get; set; } = null!;

	/// <summary>
	/// Nickname of the user.
	/// </summary>
	public string Nickname { get; set; } = null!;

	/// <summary>
	/// Email address of the user.
	/// </summary>
	public string Email { get; set; } = null!;

	/// <summary>
	/// Phone number of the user.
	/// </summary>
	public string Phone { get; set; } = null!;
}
