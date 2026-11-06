using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Events;

/// <summary>
/// 用户密码被修改时引发的领域事件。
/// </summary>
public class UserPasswordChangedEvent : DomainEvent
{
	/// <summary>
	/// 初始化 <see cref="UserPasswordChangedEvent"/> 的新实例。
	/// </summary>
	/// <param name="userId">密码被修改的用户标识。</param>
	/// <param name="type">操作类型，如 <c>change</c> 或 <c>reset</c>。</param>
	/// <param name="time">密码被修改的时间。</param>
	public UserPasswordChangedEvent(string userId, string type, DateTime time)
	{
		UserId = userId;
		Type = type;
		ChangedAt = time;
	}

	/// <summary>
	/// 获取或设置操作类型。
	/// </summary>
	/// <value>
	/// <para>- change</para>
	/// <para>- reset</para>
	/// </value>
	public string Type { get; set; }

	/// <summary>
	/// 获取或设置密码被修改的用户标识。
	/// </summary>
	public string UserId { get; }

	/// <summary>
	/// 获取或设置密码被修改的时间。
	/// </summary>
	public DateTime ChangedAt { get; }
}
