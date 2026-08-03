namespace Nerosoft.Euonia.Threading;

/// <summary>
/// 充当特定类型 <see cref="ISemaphoreProvider"/> 实例的工厂。
/// 在依赖注入场景中，此接口可能比 <see cref="ISemaphoreProvider"/> 更易于使用。
/// </summary>
public interface ISemaphoreFactory
{
    /// <summary>
    /// 使用给定的 <paramref name="name"/> 和最大计数构造一个 <see cref="ISemaphoreProvider"/> 实例。
    /// </summary>
    /// <param name="name">唯一标识该信号量的名称。</param>
    /// <param name="maxCount">信号量可同时授予的最大请求数。</param>
    /// <returns>指定 <paramref name="name"/> 对应的 <see cref="ISemaphoreProvider"/> 实例。</returns>
    ISemaphoreProvider Create(string name, int maxCount);
}