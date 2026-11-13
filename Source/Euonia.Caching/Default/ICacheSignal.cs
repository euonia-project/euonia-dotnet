namespace Nerosoft.Euonia.Caching;

/// <summary>
/// <see cref="ICacheSignal"/> 接口。
/// 实现 <see cref="IVolatileProvider" />。
/// </summary>
/// <seealso cref="IVolatileProvider" />
public interface ICacheSignal : IVolatileProvider
{
    /// <summary>
    /// 触发指定的信号。
    /// </summary>
    /// <typeparam name="T">信号值的类型。</typeparam>
    /// <param name="signal">要触发的信号；与之关联的令牌（若存在）将被标记为失效。</param>
    void Trigger<T>(T signal);

    /// <summary>
    /// 获取或创建指定信号的令牌。
    /// </summary>
    /// <typeparam name="T">信号值的类型。</typeparam>
    /// <param name="signal">要获取或创建令牌的信号值。</param>
    /// <returns>与 <paramref name="signal"/> 关联的令牌；若尚不存在则创建新令牌并返回。</returns>
    IVolatileToken When<T>(T signal);
}