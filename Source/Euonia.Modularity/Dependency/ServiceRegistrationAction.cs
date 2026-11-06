namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 服务注册操作。
/// </summary>
public class ServiceRegistrationAction : List<Action<IServiceRegistrationContext>>
{
    /// <summary>
    /// 获取或设置一个值，该值指示是否禁用类拦截器。
    /// </summary>
    public bool IsClassInterceptorsDisabled { get; set; }
}