namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务上下文访问器。
/// </summary>
public class BusinessContextAccessor
{
    /// <summary>
    /// 初始化 <see cref="BusinessContextAccessor"/> 类的新实例。
    /// </summary>
    /// <param name="provider">用于提供服务解析的服务提供程序。</param>
    public BusinessContextAccessor(IServiceProvider provider)
    {
        ServiceProvider = provider;
    }

    /// <summary>
    /// 获取服务提供程序。
    /// </summary>
    internal IServiceProvider ServiceProvider { get; private set; }
}