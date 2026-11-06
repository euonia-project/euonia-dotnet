namespace Nerosoft.Euonia.Bus.Tests.Events;

/// <summary>
/// 示例多播事件，用于验证事件发布能否投递到订阅者。
/// </summary>
public class UserCreatedEvent
{
	/// <summary>
	/// 获取或设置用户标识。
	/// </summary>
	public string UserId { get; set; } = string.Empty;
}