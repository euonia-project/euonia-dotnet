namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 充当特定类型 <see cref="ILockProvider"/> 实例的工厂。
/// 在依赖注入场景中，此接口可能比 <see cref="ILockProvider"/> 更易于使用。
/// </summary>
public interface ILockFactory
{
    /// <summary>
    /// 使用给定的 <paramref name="name"/> 构造一个 <see cref="ILockProvider"/> 实例。
    /// </summary>
    /// <param name="name">唯一标识该锁的名称。</param>
    /// <returns>指定 <paramref name="name"/> 对应的 <see cref="ILockProvider"/> 实例。</returns>
    ILockProvider Create(string name);
}