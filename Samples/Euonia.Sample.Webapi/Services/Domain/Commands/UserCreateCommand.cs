using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

internal sealed class UserCreateCommand : Command
{
	public string Username { get; set; }

	public string Password { get; set; }

	public string Email { get; set; }

	public string Phone { get; set; }

	public string Nickname { get; set; }
}

