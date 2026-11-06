using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块上下文基类，为模块提供配置服务注册、应用程序配置访问以及应用生命周期回调的默认实现。
/// </summary>
public abstract class ModuleContextBase : IModuleContext
{
    /// <summary>
    /// 获取一个值，指示服务是否应自动注册。
    /// </summary>
    protected internal virtual bool AutomaticRegisterService => true;

    /// <summary>
    /// 获取或设置服务配置上下文。
    /// </summary>
    /// <exception cref="Exception">当配置上下文尚未初始化（即模块尚未执行服务配置）时引发。</exception>
    protected internal ServiceConfigurationContext ConfigurationContext
    {
        get
        {
            if (_configurationContext == null)
            {
                throw new Exception(string.Format(Resources.IDS_CONFIGURATION_CONTEXT_UNAVAILABLE, nameof(ConfigurationContext), nameof(ConfigureServices)));
            }

            return _configurationContext;
        }
        internal set => _configurationContext = value;
    }

    private ServiceConfigurationContext? _configurationContext;

    /// <summary>
    /// 获取或设置应用程序配置服务。
    /// </summary>
    protected internal IConfiguration Configuration { get; internal set; } = null!;

    /// <inheritdoc />
    public virtual void AheadConfigureServices(ServiceConfigurationContext context)
    {
    }

    /// <inheritdoc />
    public virtual void ConfigureServices(ServiceConfigurationContext context)
    {
    }

    /// <inheritdoc />
    public virtual void AfterConfigureServices(ServiceConfigurationContext context)
    {
    }

    /// <inheritdoc />
    public virtual void OnApplicationInitialization(ApplicationInitializationContext context)
    {
    }

    /// <inheritdoc />
    public virtual void OnApplicationShutdown(ApplicationShutdownContext context)
    {
    }

    /// <summary>
    /// 注册用于配置 <typeparamref name="TOptions"/> 选项实例的委托。
    /// </summary>
    /// <typeparam name="TOptions">要配置的选项类型，必须为引用类型。</typeparam>
    /// <param name="configureOptions">接收选项实例并对其进行配置的委托。</param>
    /// <exception cref="Exception">当 <see cref="ConfigurationContext"/> 尚未初始化时引发。</exception>
    protected void Configure<TOptions>(Action<TOptions> configureOptions)
        where TOptions : class
    {
        ConfigurationContext.Services.Configure(configureOptions);
    }

    /// <summary>
    /// 注册用于配置具名选项实例 <paramref name="name"/> 的委托。
    /// </summary>
    /// <typeparam name="TOptions">要配置的选项类型，必须为引用类型。</typeparam>
    /// <param name="name">选项实例的名称，用于区分同一选项类型的多组配置。</param>
    /// <param name="configureOptions">接收选项实例并对其进行配置的委托。</param>
    /// <exception cref="Exception">当 <see cref="ConfigurationContext"/> 尚未初始化时引发。</exception>
    protected void Configure<TOptions>(string name, Action<TOptions> configureOptions)
        where TOptions : class
    {
        ConfigurationContext.Services.Configure(name, configureOptions);
    }

    /// <summary>
    /// 将 <typeparamref name="TOptions"/> 绑定到指定的配置节。
    /// </summary>
    /// <typeparam name="TOptions">要绑定的选项类型，必须为引用类型。</typeparam>
    /// <param name="configuration">提供选项值的配置节。</param>
    /// <exception cref="Exception">当 <see cref="ConfigurationContext"/> 尚未初始化时引发。</exception>
    protected void Configure<TOptions>(IConfiguration configuration)
        where TOptions : class
    {
        ConfigurationContext.Services.Configure<TOptions>(configuration);
    }

    /// <summary>
    /// 将 <typeparamref name="TOptions"/> 绑定到指定的配置节，并自定义绑定行为。
    /// </summary>
    /// <typeparam name="TOptions">要绑定的选项类型，必须为引用类型。</typeparam>
    /// <param name="configuration">提供选项值的配置节。</param>
    /// <param name="configureBinder">用于调整绑定行为的委托，例如绑定错误与未知键的处理方式。</param>
    /// <exception cref="Exception">当 <see cref="ConfigurationContext"/> 尚未初始化时引发。</exception>
    protected void Configure<TOptions>(IConfiguration configuration, Action<BinderOptions> configureBinder)
        where TOptions : class
    {
        ConfigurationContext.Services.Configure<TOptions>(configuration, configureBinder);
    }

    /// <summary>
    /// 将具名选项实例 <paramref name="name"/> 绑定到指定的配置节。
    /// </summary>
    /// <typeparam name="TOptions">要绑定的选项类型，必须为引用类型。</typeparam>
    /// <param name="name">选项实例的名称，用于区分同一选项类型的多组配置。</param>
    /// <param name="configuration">提供选项值的配置节。</param>
    /// <exception cref="Exception">当 <see cref="ConfigurationContext"/> 尚未初始化时引发。</exception>
    protected void Configure<TOptions>(string name, IConfiguration configuration)
        where TOptions : class
    {
        ConfigurationContext.Services.Configure<TOptions>(name, configuration);
    }
}