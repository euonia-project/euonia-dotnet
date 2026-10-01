using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 
/// </summary>
public abstract class ModuleContextBase : IModuleContext
{
    /// <summary>
    /// Gets a value to indicate whether the service should registry automatically or not.
    /// </summary>
    protected internal virtual bool AutomaticRegisterService => true;

    /// <summary>
    /// Gets or sets the service configuration context.
    /// </summary>
    /// <exception cref="Exception"></exception>
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

    private ServiceConfigurationContext _configurationContext;

    /// <summary>
    /// Gets or sets the application configuration service.
    /// </summary>
    protected internal IConfiguration Configuration { get; internal set; }

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
    /// 
    /// </summary>
    /// <param name="configureOptions"></param>
    /// <typeparam name="TOptions"></typeparam>
    protected void Configure<TOptions>(Action<TOptions> configureOptions)
        where TOptions : class
    {
        ConfigurationContext.Services.Configure(configureOptions);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="configureOptions"></param>
    /// <typeparam name="TOptions"></typeparam>
    protected void Configure<TOptions>(string name, Action<TOptions> configureOptions)
        where TOptions : class
    {
        ConfigurationContext.Services.Configure(name, configureOptions);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <typeparam name="TOptions"></typeparam>
    protected void Configure<TOptions>(IConfiguration configuration)
        where TOptions : class
    {
        ConfigurationContext.Services.Configure<TOptions>(configuration);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="configureBinder"></param>
    /// <typeparam name="TOptions"></typeparam>
    protected void Configure<TOptions>(IConfiguration configuration, Action<BinderOptions> configureBinder)
        where TOptions : class
    {
        ConfigurationContext.Services.Configure<TOptions>(configuration, configureBinder);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="configuration"></param>
    /// <typeparam name="TOptions"></typeparam>
    protected void Configure<TOptions>(string name, IConfiguration configuration)
        where TOptions : class
    {
        ConfigurationContext.Services.Configure<TOptions>(name, configuration);
    }
}