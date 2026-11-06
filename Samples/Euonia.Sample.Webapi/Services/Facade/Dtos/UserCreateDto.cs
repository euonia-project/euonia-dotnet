namespace Nerosoft.Euonia.Sample.Domain.Dtos;

public class UserCreateDto
{
	public string Username { get; set; } = null!;

	public string Password { get; set; } = null!;

	public string Email { get; set; } = null!;

	public string Phone { get; set; } = null!;

	public string Nickname { get; set; } = null!;
}
