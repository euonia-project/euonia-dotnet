using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

internal sealed class UserCreateCommand : Command
{
	public string Username { get; set; } = null!;

	public string Password { get; set; } = null!;

	public string Email { get; set; } = null!;

	public string Phone { get; set; } = null!;

	public string Nickname { get; set; } = null!;
}

