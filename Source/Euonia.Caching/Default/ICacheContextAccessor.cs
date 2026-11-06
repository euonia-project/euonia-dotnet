namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 提供当前缓存获取上下文 <see cref="IAcquireContext"/> 的访问入口。
/// </summary>
public interface ICacheContextAccessor
{
    /// <summary>
    /// 获取或设置当前的 <see cref="IAcquireContext"/>。
    /// </summary>
    /// <value><see cref="IAcquireContext"/> 实例。</value>
    IAcquireContext? Current { get; set; }
}