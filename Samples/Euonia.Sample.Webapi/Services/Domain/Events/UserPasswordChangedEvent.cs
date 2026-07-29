using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Events;

/// <summary>
/// A domain event that is raised when a user's password is changed.
/// </summary>
public class UserPasswordChangedEvent : DomainEvent
{
	public UserPasswordChangedEvent(string userId, string type, DateTime time)
	{
		UserId = userId;
		Type = type;
		ChangedAt = time;
	}

	/// <summary>
	/// Gets or sets the action type.
	/// </summary>
	/// <value>
	/// <para>- change</para>
	/// <para>- reset</para>
	/// </value>
	public string Type { get; set; }

	/// <summary>
	/// Gets or sets the identifier of the user whose password was changed.
	/// </summary>
	public string UserId { get; }

	/// <summary>
	/// Gets or sets the time when the password was changed.
	/// </summary>
	public DateTime ChangedAt { get; }
}
