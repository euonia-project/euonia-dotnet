# Euonia.Modularity 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Modularity |
| 公共类型 | 77 |
| 命名空间 | 3 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionCommonExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAssembly(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Reflection.Assembly assembly)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAssemblyOf<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAssemblyOf(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type type)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAutomaticRegistrar(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Nerosoft.Euonia.Modularity.IAutomaticRegistration registrar)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddNamedService<TService>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Func<System.String, System.IServiceProvider, TService> factory)
- static ObjectAccessor<T> AddObjectAccessor<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static ObjectAccessor<T> AddObjectAccessor<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, T obj)
- static ObjectAccessor<T> AddObjectAccessor<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, ObjectAccessor<T> accessor)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxied<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxied(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, System.Type implementationType, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedScoped<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedScoped(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, System.Type implementationType)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedScoped<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, IEnumerable<Castle.DynamicProxy.IInterceptor> interceptors)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedScoped<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, IEnumerable<System.Type> interceptorTypes)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedSingleton<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedSingleton(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, System.Type implementationType)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedSingleton<TService>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, TService implementation)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedSingleton<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, IEnumerable<Castle.DynamicProxy.IInterceptor> interceptors)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedSingleton<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, IEnumerable<System.Type> interceptorTypes)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedTransient<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedTransient(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, System.Type implementationType)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedTransient<TService>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, TService implementation)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedTransient<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, IEnumerable<Castle.DynamicProxy.IInterceptor> interceptors)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddProxiedTransient<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, IEnumerable<System.Type> interceptorTypes)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddScoped<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String name)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String name)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTransient<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String name)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTypes(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type[] types)
- static Nerosoft.Euonia.Modularity.ServiceExposingAction GetExposingActions(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static T GetObject<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static T GetObjectOrNull<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Nerosoft.Euonia.Modularity.ServiceRegistrationAction GetRegistrationActions(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static T GetRequiredService<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static System.Object GetRequiredService(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type type)
- static Lazy<T> GetRequiredServiceLazy<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Lazy<System.Object> GetRequiredServiceLazy(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type type)
- static Lazy<T> GetServiceLazy<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Lazy<System.Object> GetServiceLazy(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type type)
- static System.IServiceProvider GetServiceProviderOrNull(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static T GetSingletonInstance<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static T GetSingletonInstanceOrNull<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static System.Boolean IsAdded<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static System.Boolean IsAdded(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type type)
- static System.Boolean IsAddedImplementation<TService, TImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static System.Boolean IsAddedImplementation(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, System.Type implementationType)
- static System.Boolean IsAddedImplementation<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static System.Boolean IsAddedImplementation(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type implementationType)
- static void OnExposing(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Modularity.IServiceExposingContext> exposeAction)
- static void Registry(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Modularity.IServiceRegistrationContext> registrationAction)
- static ObjectAccessor<T> TryAddObjectAccessor<T>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

### Microsoft.Extensions.DependencyInjection.ServiceCollectionModularityExtensions

**类型**：static class

- static Nerosoft.Euonia.Modularity.IApplicationWithServiceProvider AddModularityApplication<TStartupModel>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration = null, Action<Nerosoft.Euonia.Modularity.ApplicationCreationOptions> optionsAction = null)
- static Microsoft.Extensions.Configuration.IConfiguration GetConfiguration(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection ReplaceConfiguration(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)

## 命名空间 Nerosoft.Euonia.Modularity

### Nerosoft.Euonia.Modularity.ApplicationCreationOptions

**类型**：class

- new Nerosoft.Euonia.Modularity.ApplicationCreationOptions(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- Nerosoft.Euonia.Modularity.ConfigurationBuilderOptions Configuration { get; }
- IList<Nerosoft.Euonia.Modularity.IModuleSource> ModuleSources { get; }
- Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
- Nerosoft.Euonia.Modularity.ApplicationCreationOptions AddModuleFiles(System.String[] assemblyFiles)
- Nerosoft.Euonia.Modularity.ApplicationCreationOptions AddModuleFolder(System.String path, System.IO.SearchOption searchOption = 0)
- Nerosoft.Euonia.Modularity.ApplicationCreationOptions AddModuleSource(Nerosoft.Euonia.Modularity.IModuleSource moduleSource)
- Nerosoft.Euonia.Modularity.ApplicationCreationOptions AddModuleSource(System.Type[] moduleTypes)

### Nerosoft.Euonia.Modularity.ApplicationFactory

**类型**：static class

- static Nerosoft.Euonia.Modularity.IApplicationWithServiceProvider Create<TStartupModule>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration, Action<Nerosoft.Euonia.Modularity.ApplicationCreationOptions> optionsAction = null)
- static Nerosoft.Euonia.Modularity.IApplicationWithServiceProvider Create<TStartupModule>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Nerosoft.Euonia.Modularity.IApplicationWithServiceProvider Create(System.Type startupModuleType, Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration, Action<Nerosoft.Euonia.Modularity.ApplicationCreationOptions> optionsAction = null)
- static Task<Nerosoft.Euonia.Modularity.IApplicationWithServiceProvider> CreateAsync<TStartupModule>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration, Action<Nerosoft.Euonia.Modularity.ApplicationCreationOptions> optionsAction = null, System.Threading.CancellationToken cancellationToken = default)
- static Task<Nerosoft.Euonia.Modularity.IApplicationWithServiceProvider> CreateAsync<TStartupModule>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Threading.CancellationToken cancellationToken = default)
- static Task<Nerosoft.Euonia.Modularity.IApplicationWithServiceProvider> CreateAsync(System.Type startupModuleType, Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration, Action<Nerosoft.Euonia.Modularity.ApplicationCreationOptions> optionsAction = null, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Modularity.ApplicationInitializationContext

**类型**：class
接口 Nerosoft.Euonia.Modularity.IServiceProviderAccessor

- new Nerosoft.Euonia.Modularity.ApplicationInitializationContext(System.IServiceProvider serviceProvider)
- System.IServiceProvider ServiceProvider { get; set; }

### Nerosoft.Euonia.Modularity.ApplicationShutdownContext

**类型**：class
接口 Nerosoft.Euonia.Modularity.IServiceProviderAccessor

- new Nerosoft.Euonia.Modularity.ApplicationShutdownContext(System.IServiceProvider serviceProvider)
- System.IServiceProvider ServiceProvider { get; }

### Nerosoft.Euonia.Modularity.ApplicationWithServiceProvider

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModularityApplicationBase；接口 Nerosoft.Euonia.Modularity.IModularityApplication、Nerosoft.Euonia.Modularity.IModuleContainer、System.IDisposable、Nerosoft.Euonia.Modularity.IApplicationWithServiceProvider、System.IAsyncDisposable

- new Nerosoft.Euonia.Modularity.ApplicationWithServiceProvider(System.Type startupModuleType, Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration, Action<Nerosoft.Euonia.Modularity.ApplicationCreationOptions> optionsAction)
- void Dispose()
- System.Threading.Tasks.ValueTask DisposeAsync()
- void Initialize(System.IServiceProvider serviceProvider)
- System.Threading.Tasks.Task InitializeAsync(System.IServiceProvider serviceProvider, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Modularity.AssemblyFinder

**类型**：class
接口 Nerosoft.Euonia.Modularity.IAssemblyFinder

- new Nerosoft.Euonia.Modularity.AssemblyFinder(Nerosoft.Euonia.Modularity.IModuleContainer moduleContainer)
- IReadOnlyList<System.Reflection.Assembly> Assemblies { get; }
- IReadOnlyList<System.Reflection.Assembly> FindAll()

### Nerosoft.Euonia.Modularity.AutomaticRegistrationBase

**类型**：class
接口 Nerosoft.Euonia.Modularity.IAutomaticRegistration

- void AddAssembly(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Reflection.Assembly assembly)
- void AddType(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type type)
- void AddTypes(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type[] types)

### Nerosoft.Euonia.Modularity.ConfigurationBuilderOptions

**类型**：class

- new Nerosoft.Euonia.Modularity.ConfigurationBuilderOptions()
- System.String BasePath { get; set; }
- System.String[] CommandLineArgs { get; set; }
- System.String EnvironmentName { get; set; }
- System.String EnvironmentVariablesPrefix { get; set; }
- System.String FileName { get; set; }
- System.Boolean ReloadOnChange { get; set; }
- System.Reflection.Assembly UserSecretsAssembly { get; set; }
- System.String UserSecretsId { get; set; }

### Nerosoft.Euonia.Modularity.ConfigurationHelper

**类型**：static class

- static Microsoft.Extensions.Configuration.IConfigurationBuilder AddEnvironmentVariableAliases(Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.String prefix = null)
- static Microsoft.Extensions.Configuration.IConfigurationBuilder AddEnvironmentVariables(Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.String prefix = null)
- static OptionsBuilder<TOptions> BindConfigurationIfAvailable<TOptions>(OptionsBuilder<TOptions> optionsBuilder, System.String[] sectionNames)
- static Microsoft.Extensions.Configuration.IConfigurationRoot BuildConfiguration(Nerosoft.Euonia.Modularity.ConfigurationBuilderOptions options = null, Action<Microsoft.Extensions.Configuration.IConfigurationBuilder> builderAction = null)
- static Microsoft.Extensions.Configuration.IConfiguration EnsureEnvironmentVariables(Microsoft.Extensions.Configuration.IConfiguration configuration, System.String prefix = null)

### Nerosoft.Euonia.Modularity.DefaultAutomaticRegistration

**类型**：class
基类 Nerosoft.Euonia.Modularity.AutomaticRegistrationBase；接口 Nerosoft.Euonia.Modularity.IAutomaticRegistration

- new Nerosoft.Euonia.Modularity.DefaultAutomaticRegistration()
- void AddType(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type type)

### Nerosoft.Euonia.Modularity.DefaultRequestContextAccessor

**类型**：class

- new Nerosoft.Euonia.Modularity.DefaultRequestContextAccessor()
- System.RequestContext Context { get; set; }
- void Clear()
- void SetContext(System.RequestContext context)

### Nerosoft.Euonia.Modularity.DelegateModuleSource

**类型**：class
接口 Nerosoft.Euonia.Modularity.IModuleSource

- new Nerosoft.Euonia.Modularity.DelegateModuleSource(Func<IEnumerable<System.Type>> factory)
- new Nerosoft.Euonia.Modularity.DelegateModuleSource(System.Type[] moduleTypes)
- IEnumerable<System.Type> GetModules()

### Nerosoft.Euonia.Modularity.DelegateRequestContextAccessor

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Modularity.DelegateRequestContextAccessor(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(System.AsyncCallback callback, System.Object object)
- System.RequestContext EndInvoke(System.IAsyncResult result)
- System.RequestContext Invoke()

### Nerosoft.Euonia.Modularity.DependsOnAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Modularity.DependsOnAttribute(System.Type[] types)
- System.Type[] ModuleTypes { get; }

### Nerosoft.Euonia.Modularity.DisableAutomaticRegistrationAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Modularity.DisableAutomaticRegistrationAttribute()

### Nerosoft.Euonia.Modularity.ExportServiceAsScopedAttribute

**类型**：class
基类 Nerosoft.Euonia.Modularity.ExportServiceAttribute

- new Nerosoft.Euonia.Modularity.ExportServiceAsScopedAttribute()
- new Nerosoft.Euonia.Modularity.ExportServiceAsScopedAttribute(System.Type serviceType)
- new Nerosoft.Euonia.Modularity.ExportServiceAsScopedAttribute(System.Type[] serviceTypes)

### Nerosoft.Euonia.Modularity.ExportServiceAsSingletonAttribute

**类型**：class
基类 Nerosoft.Euonia.Modularity.ExportServiceAttribute

- new Nerosoft.Euonia.Modularity.ExportServiceAsSingletonAttribute()
- new Nerosoft.Euonia.Modularity.ExportServiceAsSingletonAttribute(System.Type serviceType)
- new Nerosoft.Euonia.Modularity.ExportServiceAsSingletonAttribute(System.Type[] serviceTypes)

### Nerosoft.Euonia.Modularity.ExportServiceAsTransientAttribute

**类型**：class
基类 Nerosoft.Euonia.Modularity.ExportServiceAttribute

- new Nerosoft.Euonia.Modularity.ExportServiceAsTransientAttribute()
- new Nerosoft.Euonia.Modularity.ExportServiceAsTransientAttribute(System.Type serviceType)
- new Nerosoft.Euonia.Modularity.ExportServiceAsTransientAttribute(System.Type[] serviceTypes)

### Nerosoft.Euonia.Modularity.ExportServiceAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Modularity.ExportServiceAttribute(Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime)
- new Nerosoft.Euonia.Modularity.ExportServiceAttribute(Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime, System.Type[] serviceTypes)
- System.Boolean IncludeSelf { get; set; }
- Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; }
- System.Boolean ReplaceServices { get; set; }
- IReadOnlyList<System.Type> ServiceTypes { get; }
- System.Boolean TryRegister { get; set; }

### Nerosoft.Euonia.Modularity.ExposedServiceExplorer

**类型**：static class

- static List<System.Type> GetExposedServices(System.Type type)

### Nerosoft.Euonia.Modularity.ExposeServicesAttribute

**类型**：class
基类 System.Attribute；接口 Nerosoft.Euonia.Modularity.IExposedServiceTypesProvider

- new Nerosoft.Euonia.Modularity.ExposeServicesAttribute(System.Type[] serviceTypes)
- System.Boolean IncludeDefaults { get; set; }
- System.Boolean IncludeSelf { get; set; }
- System.Type[] ServiceTypes { get; }
- IEnumerable<System.Type> GetExposedServiceTypes(System.Type targetType)

### Nerosoft.Euonia.Modularity.FileModuleSource

**类型**：class
接口 Nerosoft.Euonia.Modularity.IModuleSource

- new Nerosoft.Euonia.Modularity.FileModuleSource(System.String[] assemblyFiles)
- IEnumerable<System.Type> GetModules()

### Nerosoft.Euonia.Modularity.FolderModuleSource

**类型**：class
接口 Nerosoft.Euonia.Modularity.IModuleSource

- new Nerosoft.Euonia.Modularity.FolderModuleSource(System.String path, System.IO.SearchOption searchOption = 0)
- IEnumerable<System.Type> GetModules()

### Nerosoft.Euonia.Modularity.IApplicationWithServiceProvider

**类型**：interface
接口 Nerosoft.Euonia.Modularity.IModularityApplication、Nerosoft.Euonia.Modularity.IModuleContainer、System.IDisposable、System.IAsyncDisposable

- void Initialize(System.IServiceProvider serviceProvider)
- System.Threading.Tasks.Task InitializeAsync(System.IServiceProvider serviceProvider, System.Threading.CancellationToken cancellationToken = default)
- void SetServiceProvider(System.IServiceProvider serviceProvider)

### Nerosoft.Euonia.Modularity.IAssemblyFinder

**类型**：interface

- IReadOnlyList<System.Reflection.Assembly> Assemblies { get; }

### Nerosoft.Euonia.Modularity.IAutomaticRegistration

**类型**：interface

- void AddAssembly(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Reflection.Assembly assembly)
- void AddType(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type type)
- void AddTypes(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type[] types)

### Nerosoft.Euonia.Modularity.IExposedServiceTypesProvider

**类型**：interface

- IEnumerable<System.Type> GetExposedServiceTypes(System.Type targetType)

### Nerosoft.Euonia.Modularity.IMethodInterceptor

**类型**：interface

- System.Threading.Tasks.Task InterceptAsync(Nerosoft.Euonia.Modularity.IMethodInvocation invocation)

### Nerosoft.Euonia.Modularity.IMethodInvocation

**类型**：interface

- System.Object[] Arguments { get; }
- IReadOnlyDictionary<System.String, System.Object> ArgumentsDictionary { get; }
- System.Type[] GenericArguments { get; }
- System.Reflection.MethodInfo Method { get; }
- System.Object ReturnValue { get; set; }
- System.Object TargetObject { get; }
- System.Threading.Tasks.Task ProceedAsync()

### Nerosoft.Euonia.Modularity.IModularityApplication

**类型**：interface
接口 Nerosoft.Euonia.Modularity.IModuleContainer、System.IDisposable

- Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }
- System.Boolean IsInitialized { get; }
- System.Boolean IsShutdown { get; }
- System.IServiceProvider ServiceProvider { get; }
- Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
- System.Type StartupModuleType { get; }
- void Shutdown()
- System.Threading.Tasks.Task ShutdownAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Modularity.IModuleContainer

**类型**：interface

- IReadOnlyList<Nerosoft.Euonia.Modularity.IModuleDescriptor> Modules { get; }

### Nerosoft.Euonia.Modularity.IModuleContext

**类型**：interface

- void AfterConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- System.Threading.Tasks.Task AfterConfigureServicesAsync(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void AheadConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- System.Threading.Tasks.Task AheadConfigureServicesAsync(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- System.Threading.Tasks.Task ConfigureServicesAsync(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void OnApplicationInitialization(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)
- System.Threading.Tasks.Task OnApplicationInitializationAsync(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)
- void OnApplicationShutdown(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context)
- System.Threading.Tasks.Task OnApplicationShutdownAsync(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context)

### Nerosoft.Euonia.Modularity.IModuleDescriptor

**类型**：interface

- System.Reflection.Assembly Assembly { get; }
- IReadOnlyList<Nerosoft.Euonia.Modularity.IModuleDescriptor> Dependencies { get; }
- Nerosoft.Euonia.Modularity.IModuleContext Instance { get; }
- System.Type Type { get; }

### Nerosoft.Euonia.Modularity.IModuleLifecycle

**类型**：interface
接口 System.ITransientDependency

- void Finalize(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context, Nerosoft.Euonia.Modularity.IModuleContext module)
- System.Threading.Tasks.Task FinalizeAsync(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context, Nerosoft.Euonia.Modularity.IModuleContext module)
- void Initialize(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context, Nerosoft.Euonia.Modularity.IModuleContext module)
- System.Threading.Tasks.Task InitializeAsync(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context, Nerosoft.Euonia.Modularity.IModuleContext module)

### Nerosoft.Euonia.Modularity.IModuleLoader

**类型**：interface

- Nerosoft.Euonia.Modularity.IModuleDescriptor[] LoadModules(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type startupModuleType)
- Nerosoft.Euonia.Modularity.IModuleDescriptor[] LoadModules(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type startupModuleType, IEnumerable<System.Type> additionalModuleTypes)

### Nerosoft.Euonia.Modularity.IModuleManager

**类型**：interface

- System.Boolean IsInitialized { get; }
- void InitializeModules(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)
- System.Threading.Tasks.Task InitializeModulesAsync(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context, System.Threading.CancellationToken cancellationToken = default)
- void UnloadModules(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context)
- System.Threading.Tasks.Task UnloadModulesAsync(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Modularity.IModuleSource

**类型**：interface

- IEnumerable<System.Type> GetModules()

### Nerosoft.Euonia.Modularity.IRequestContextAccessor

**类型**：interface
接口 System.IScopedDependency

- System.RequestContext Context { get; }

### Nerosoft.Euonia.Modularity.IServiceExposingContext

**类型**：interface

- List<System.Type> ExposedTypes { get; }
- System.Type ImplementationType { get; }

### Nerosoft.Euonia.Modularity.IServiceProviderAccessor

**类型**：interface

- System.IServiceProvider ServiceProvider { get; }

### Nerosoft.Euonia.Modularity.IServiceRegistrationContext

**类型**：interface

- List<System.Type> ExposedTypes { get; }
- System.Type ImplementationType { get; }
- ITypeList<Nerosoft.Euonia.Modularity.IMethodInterceptor> Interceptors { get; }
- Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; }
- Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }

### Nerosoft.Euonia.Modularity.ITypeFinder

**类型**：interface

- IReadOnlyList<System.Type> Types { get; }

### Nerosoft.Euonia.Modularity.ModularityApplicationBase

**类型**：class
接口 Nerosoft.Euonia.Modularity.IModularityApplication、Nerosoft.Euonia.Modularity.IModuleContainer、System.IDisposable

- Microsoft.Extensions.Configuration.IConfiguration Configuration { get; }
- System.Boolean IsInitialized { get; }
- System.Boolean IsShutdown { get; }
- IReadOnlyList<Nerosoft.Euonia.Modularity.IModuleDescriptor> Modules { get; }
- System.IServiceProvider ServiceProvider { get; }
- Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }
- System.Type StartupModuleType { get; }
- void Dispose()
- System.Threading.Tasks.ValueTask DisposeAsync()
- void Shutdown()
- System.Threading.Tasks.Task ShutdownAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Modularity.ModuleContainerExtensions

**类型**：static class

- static Nerosoft.Euonia.Modularity.IModuleDescriptor FindModule(Nerosoft.Euonia.Modularity.IModuleContainer container, System.Type moduleType)
- static Nerosoft.Euonia.Modularity.IModuleDescriptor FindModule<TModule>(Nerosoft.Euonia.Modularity.IModuleContainer container)
- static Nerosoft.Euonia.Modularity.IModuleDescriptor GetModule(Nerosoft.Euonia.Modularity.IModuleContainer container, System.Type moduleType)
- static Nerosoft.Euonia.Modularity.IModuleDescriptor GetModule<TModule>(Nerosoft.Euonia.Modularity.IModuleContainer container)
- static System.Boolean IsModuleLoaded<TModule>(Nerosoft.Euonia.Modularity.IModuleContainer container)
- static System.Boolean TryGetModule<TModule>(Nerosoft.Euonia.Modularity.IModuleContainer container, out Nerosoft.Euonia.Modularity.IModuleDescriptor module)

### Nerosoft.Euonia.Modularity.ModuleContextBase

**类型**：class
接口 Nerosoft.Euonia.Modularity.IModuleContext

- void AfterConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- System.Threading.Tasks.Task AfterConfigureServicesAsync(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void AheadConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- System.Threading.Tasks.Task AheadConfigureServicesAsync(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- System.Threading.Tasks.Task ConfigureServicesAsync(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void OnApplicationInitialization(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)
- System.Threading.Tasks.Task OnApplicationInitializationAsync(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)
- void OnApplicationShutdown(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context)
- System.Threading.Tasks.Task OnApplicationShutdownAsync(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context)

### Nerosoft.Euonia.Modularity.ModuleDescriptor

**类型**：class
接口 Nerosoft.Euonia.Modularity.IModuleDescriptor

- new Nerosoft.Euonia.Modularity.ModuleDescriptor(System.Type type, Nerosoft.Euonia.Modularity.IModuleContext instance)
- System.Reflection.Assembly Assembly { get; }
- IReadOnlyList<Nerosoft.Euonia.Modularity.IModuleDescriptor> Dependencies { get; }
- Nerosoft.Euonia.Modularity.IModuleContext Instance { get; }
- System.String Name { get; }
- System.Type Type { get; }
- void AddDependency(Nerosoft.Euonia.Modularity.IModuleDescriptor descriptor)
- System.String ToString()

### Nerosoft.Euonia.Modularity.ModuleHelper

**类型**：static class

- static System.Boolean IsModule(System.Type type)

### Nerosoft.Euonia.Modularity.ModuleLifecycleBase

**类型**：class
接口 Nerosoft.Euonia.Modularity.IModuleLifecycle、System.ITransientDependency

- void Finalize(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context, Nerosoft.Euonia.Modularity.IModuleContext module)
- System.Threading.Tasks.Task FinalizeAsync(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context, Nerosoft.Euonia.Modularity.IModuleContext module)
- void Initialize(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context, Nerosoft.Euonia.Modularity.IModuleContext module)
- System.Threading.Tasks.Task InitializeAsync(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context, Nerosoft.Euonia.Modularity.IModuleContext module)

### Nerosoft.Euonia.Modularity.ModuleLifecycleOptions

**类型**：class

- new Nerosoft.Euonia.Modularity.ModuleLifecycleOptions()
- ITypeList<Nerosoft.Euonia.Modularity.IModuleLifecycle> Lifecycle { get; }

### Nerosoft.Euonia.Modularity.ModuleLoader

**类型**：class
接口 Nerosoft.Euonia.Modularity.IModuleLoader

- new Nerosoft.Euonia.Modularity.ModuleLoader()
- Nerosoft.Euonia.Modularity.IModuleDescriptor[] LoadModules(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type startupModuleType)
- Nerosoft.Euonia.Modularity.IModuleDescriptor[] LoadModules(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type startupModuleType, IEnumerable<System.Type> additionalModuleTypes)

### Nerosoft.Euonia.Modularity.ModuleManager

**类型**：class
接口 Nerosoft.Euonia.Modularity.IModuleManager、System.ISingletonDependency

- new Nerosoft.Euonia.Modularity.ModuleManager(Nerosoft.Euonia.Modularity.IModuleContainer moduleContainer, IOptions<Nerosoft.Euonia.Modularity.ModuleLifecycleOptions> options)
- System.Boolean IsInitialized { get; }
- void InitializeModules(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)
- System.Threading.Tasks.Task InitializeModulesAsync(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context, System.Threading.CancellationToken cancellationToken = default)
- void UnloadModules(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context)
- System.Threading.Tasks.Task UnloadModulesAsync(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Modularity.OnApplicationInitializationModuleLifecycle

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleLifecycleBase；接口 Nerosoft.Euonia.Modularity.IModuleLifecycle、System.ITransientDependency

- new Nerosoft.Euonia.Modularity.OnApplicationInitializationModuleLifecycle()
- void Initialize(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context, Nerosoft.Euonia.Modularity.IModuleContext module)
- System.Threading.Tasks.Task InitializeAsync(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context, Nerosoft.Euonia.Modularity.IModuleContext module)

### Nerosoft.Euonia.Modularity.OnApplicationShutdownModuleLifecycle

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleLifecycleBase；接口 Nerosoft.Euonia.Modularity.IModuleLifecycle、System.ITransientDependency

- new Nerosoft.Euonia.Modularity.OnApplicationShutdownModuleLifecycle()
- void Finalize(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context, Nerosoft.Euonia.Modularity.IModuleContext module)
- System.Threading.Tasks.Task FinalizeAsync(Nerosoft.Euonia.Modularity.ApplicationShutdownContext context, Nerosoft.Euonia.Modularity.IModuleContext module)

### Nerosoft.Euonia.Modularity.RequestContextAccessor

**类型**：class
接口 Nerosoft.Euonia.Modularity.IRequestContextAccessor、System.IScopedDependency

- new Nerosoft.Euonia.Modularity.RequestContextAccessor(IOptions<Nerosoft.Euonia.Modularity.RequestContextAccessorOptions> options, System.IServiceProvider provider)
- System.RequestContext Context { get; }

### Nerosoft.Euonia.Modularity.RequestContextAccessorOptions

**类型**：class

- new Nerosoft.Euonia.Modularity.RequestContextAccessorOptions()
- System.Boolean UseDefaultAccessor { get; set; }

### Nerosoft.Euonia.Modularity.ServiceConfigurationContext

**类型**：class

- new Nerosoft.Euonia.Modularity.ServiceConfigurationContext(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- System.Object this[System.String key] { get; set; }
- IDictionary<System.String, System.Object> Items { get; }
- Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }

### Nerosoft.Euonia.Modularity.ServiceExposingAction

**类型**：class
基类 List<Action<Nerosoft.Euonia.Modularity.IServiceExposingContext>>；接口 IList<Action<Nerosoft.Euonia.Modularity.IServiceExposingContext>>、ICollection<Action<Nerosoft.Euonia.Modularity.IServiceExposingContext>>、IEnumerable<Action<Nerosoft.Euonia.Modularity.IServiceExposingContext>>、System.Collections.IEnumerable、System.Collections.IList、System.Collections.ICollection、IReadOnlyList<Action<Nerosoft.Euonia.Modularity.IServiceExposingContext>>、IReadOnlyCollection<Action<Nerosoft.Euonia.Modularity.IServiceExposingContext>>

- new Nerosoft.Euonia.Modularity.ServiceExposingAction()

### Nerosoft.Euonia.Modularity.ServiceExposingContext

**类型**：class
接口 Nerosoft.Euonia.Modularity.IServiceExposingContext

- new Nerosoft.Euonia.Modularity.ServiceExposingContext(System.Type implementationType, List<System.Type> exposedTypes)
- List<System.Type> ExposedTypes { get; }
- System.Type ImplementationType { get; }

### Nerosoft.Euonia.Modularity.ServiceRegistrationAction

**类型**：class
基类 List<Action<Nerosoft.Euonia.Modularity.IServiceRegistrationContext>>；接口 IList<Action<Nerosoft.Euonia.Modularity.IServiceRegistrationContext>>、ICollection<Action<Nerosoft.Euonia.Modularity.IServiceRegistrationContext>>、IEnumerable<Action<Nerosoft.Euonia.Modularity.IServiceRegistrationContext>>、System.Collections.IEnumerable、System.Collections.IList、System.Collections.ICollection、IReadOnlyList<Action<Nerosoft.Euonia.Modularity.IServiceRegistrationContext>>、IReadOnlyCollection<Action<Nerosoft.Euonia.Modularity.IServiceRegistrationContext>>

- new Nerosoft.Euonia.Modularity.ServiceRegistrationAction()

### Nerosoft.Euonia.Modularity.ServiceRegistrationContext

**类型**：class
接口 Nerosoft.Euonia.Modularity.IServiceRegistrationContext

- new Nerosoft.Euonia.Modularity.ServiceRegistrationContext(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type implementationType, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime, List<System.Type> exposedTypes)
- List<System.Type> ExposedTypes { get; }
- System.Type ImplementationType { get; }
- ITypeList<Nerosoft.Euonia.Modularity.IMethodInterceptor> Interceptors { get; }
- Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; }
- Microsoft.Extensions.DependencyInjection.IServiceCollection Services { get; }

### Nerosoft.Euonia.Modularity.TypeFinder

**类型**：class
接口 Nerosoft.Euonia.Modularity.ITypeFinder

- new Nerosoft.Euonia.Modularity.TypeFinder(Nerosoft.Euonia.Modularity.IAssemblyFinder assemblyFinder)
- IReadOnlyList<System.Type> Types { get; }

## 命名空间 System

### System.IHasLazyServiceProvider

**类型**：interface

- System.ILazyServiceProvider LazyServiceProvider { get; set; }

### System.ILazyServiceProvider

**类型**：interface

- System.Object GetKeyedService(System.Type serviceType, System.Object serviceKey)
- T GetKeyedService<T>(System.Object serviceKey)
- T GetKeyedService<T>(System.Object serviceKey, T defaultValue)
- T GetKeyedService<T>(System.Object serviceKey, Func<System.IServiceProvider, T> factory)
- System.Object GetKeyedService(System.Type serviceType, System.Object serviceKey, Func<System.IServiceProvider, System.Object> factory)
- System.Object GetKeyedService(System.Type serviceType, System.Object serviceKey, System.Object defaultValue)
- T GetRequiredKeyedService<T>(System.Object serviceKey)
- System.Object GetRequiredKeyedService(System.Type serviceType, System.Object serviceKey)
- T GetRequiredService<T>()
- System.Object GetRequiredService(System.Type serviceType)
- T GetService<T>()
- System.Object GetService(System.Type serviceType)
- T GetService<T>(T defaultValue)
- System.Object GetService(System.Type serviceType, System.Object defaultValue)
- System.Object GetService(System.Type serviceType, Func<System.IServiceProvider, System.Object> factory)
- T GetService<T>(Func<System.IServiceProvider, System.Object> factory)

### System.InjectAttribute

**类型**：class
基类 System.Attribute

- new System.InjectAttribute(System.Object serviceKey = null)
- System.Object ServiceKey { get; }

### IObjectAccessor<TValue>

**类型**：interface

- TValue Value { get; }

### System.IScopedDependency

**类型**：interface

_无公共成员。_

### System.IServiceAccessor

**类型**：interface
接口 System.IScopedDependency

- System.IServiceProvider ServiceProvider { get; }
- T GetKeyedService<T>(System.Object key)
- T GetRequiredKeyedService<T>(System.Object name)
- T GetRequiredService<T>()
- T GetService<T>()
- System.Object GetService(System.Type type)

### System.ISingletonDependency

**类型**：interface

_无公共成员。_

### System.ITransientDependency

**类型**：interface

_无公共成员。_

### System.LazyServiceProvider

**类型**：class
接口 System.ILazyServiceProvider

- new System.LazyServiceProvider(System.IServiceProvider serviceProvider)
- T GetKeyedService<T>(System.Object serviceKey)
- System.Object GetKeyedService(System.Type serviceType, System.Object serviceKey)
- T GetKeyedService<T>(System.Object serviceKey, T defaultValue)
- T GetKeyedService<T>(System.Object serviceKey, Func<System.IServiceProvider, T> factory)
- System.Object GetKeyedService(System.Type serviceType, System.Object serviceKey, Func<System.IServiceProvider, System.Object> factory)
- System.Object GetKeyedService(System.Type serviceType, System.Object serviceKey, System.Object defaultValue)
- T GetRequiredKeyedService<T>(System.Object serviceKey)
- System.Object GetRequiredKeyedService(System.Type serviceType, System.Object serviceKey)
- T GetRequiredService<T>()
- System.Object GetRequiredService(System.Type serviceType)
- T GetService<T>()
- System.Object GetService(System.Type serviceType)
- T GetService<T>(T defaultValue)
- System.Object GetService(System.Type serviceType, System.Object defaultValue)
- T GetService<T>(Func<System.IServiceProvider, System.Object> factory)
- System.Object GetService(System.Type serviceType, Func<System.IServiceProvider, System.Object> factory)

### NamedService<TService>

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new NamedService<TService>(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(System.String name, System.AsyncCallback callback, System.Object object)
- TService EndInvoke(System.IAsyncResult result)
- TService Invoke(System.String name)

### NamedServiceContainer<TService>

**类型**：class
基类 ConcurrentDictionary<System.String, System.Type>；接口 IDictionary<System.String, System.Type>、ICollection<KeyValuePair<System.String, System.Type>>、IEnumerable<KeyValuePair<System.String, System.Type>>、System.Collections.IEnumerable、System.Collections.IDictionary、System.Collections.ICollection、IReadOnlyDictionary<System.String, System.Type>、IReadOnlyCollection<KeyValuePair<System.String, System.Type>>

- new NamedServiceContainer<TService>()

### ObjectAccessor<TValue>

**类型**：class
接口 IObjectAccessor<TValue>

- new ObjectAccessor<TValue>()
- new ObjectAccessor<TValue>(TValue obj)
- TValue Value { get; set; }

### System.ServiceAccessor

**类型**：class
接口 System.IServiceAccessor、System.IScopedDependency

- new System.ServiceAccessor(System.IServiceProvider provider)
- System.IServiceProvider ServiceProvider { get; }
- T GetKeyedService<T>(System.Object key)
- T GetRequiredKeyedService<T>(System.Object name)
- T GetRequiredService<T>()
- T GetService<T>()
- System.Object GetService(System.Type type)

### System.ServiceProviderExtensions

**类型**：static class

- static System.Object GetKeyedService(System.IServiceProvider provider, System.Type serviceType, System.Object serviceKey)
- static TService GetNamedRequiredService<TService>(System.IServiceProvider provider, System.String name)
- static TService GetNamedService<TService>(System.IServiceProvider provider, System.String name)
- static TService GetNamedService<TService>(System.IServiceProvider provider, System.String name, TService defaultValue)
