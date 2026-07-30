namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示被标记的类是单播消息（命令）。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class UnicastAttribute : Attribute
{
}