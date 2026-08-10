namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="IRules"/> 的公共接口。
/// </summary>
public interface IRules
{
    /// <summary>
    /// 获取目标业务对象。
    /// </summary>
    /// <value>业务对象。</value>
    object Target { get; }
}