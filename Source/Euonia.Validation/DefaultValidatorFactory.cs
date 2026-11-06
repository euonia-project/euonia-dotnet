namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 默认验证器工厂。
/// </summary>
public class DefaultValidatorFactory : IValidatorFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化 <see cref="DefaultValidatorFactory"/> 类的新实例。
    /// </summary>
    /// <param name="serviceProvider">用于创建验证器时解析依赖服务的服务提供程序。</param>
    public DefaultValidatorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 创建新的验证器。
    /// </summary>
    /// <returns>新的 <see cref="IValidator"/> 实例。</returns>
    public IValidator Create()
    {
        return new DefaultValidator(_serviceProvider);
    }
}