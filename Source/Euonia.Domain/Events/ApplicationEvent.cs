namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 表示由应用程序服务引发的事件的抽象基类。
/// 继承自 <see cref="Event"/> 并实现 <see cref="IApplicationEvent"/>。
/// </summary>
/// <seealso cref="Event" />
/// <seealso cref="IApplicationEvent" />
public abstract class ApplicationEvent : Event, IApplicationEvent
{
}