# Euonia.Mapping.Automapper 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Mapping.Automapper |
| 公共类型 | 8 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAutomapper(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Func<IEnumerable<System.Type>> factory, Action<AutoMapper.MapperConfigurationExpression> config = null, System.Boolean assertConfiguration = false)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAutomapper(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<List<System.Type>> handler, Action<AutoMapper.MapperConfigurationExpression> config = null, System.Boolean assertConfiguration = false)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAutomapper(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAutomapper(Microsoft.Extensions.DependencyInjection.IServiceCollection services, IEnumerable<System.Type> types, Action<AutoMapper.MapperConfigurationExpression> config = null, System.Boolean assertConfiguration = false)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAutomapper(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<AutoMapper.MapperConfigurationExpression> config, System.Boolean assertConfiguration = false)

## 命名空间 Nerosoft.Euonia.Mapping

### Nerosoft.Euonia.Mapping.AutomapperExtensions

**类型**：static class

- static AutoMapper.IMappingExpression Ignore(AutoMapper.IMappingExpression expression, System.String[] propertyNames)
- static IMappingExpression<TSource, TDestination> Ignore<TSource, TDestination>(IMappingExpression<TSource, TDestination> expression, Expression<Func<TDestination, System.Object>>[] propertyExpressions)

### Nerosoft.Euonia.Mapping.AutomapperModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Mapping.AutomapperModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void OnApplicationInitialization(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)

### Nerosoft.Euonia.Mapping.AutomapperOptions

**类型**：class

- new Nerosoft.Euonia.Mapping.AutomapperOptions()
- void AddMaps(System.Reflection.Assembly assembly, System.Boolean validate = false)
- void AddProfile<TProfile>(System.Boolean validate = false)
- void AddProfile<TProfile>(TProfile profile, System.Boolean validate = false)
- void AddProfile(System.Type profileType, System.Boolean validate = false)
- void AddProfile(ICollection<System.Type> profileTypes, System.Boolean validate = false)
- void Configure(Action<System.IServiceProvider, AutoMapper.MapperConfigurationExpression> config)

### Nerosoft.Euonia.Mapping.AutomapperTypeAdapter

**类型**：class
接口 Nerosoft.Euonia.Mapping.ITypeAdapter

- new Nerosoft.Euonia.Mapping.AutomapperTypeAdapter(AutoMapper.IMapper mapper)
- TDestination Adapt<TSource, TDestination>(TSource source)
- TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
- TDestination Adapt<TDestination>(System.Object source)
- System.Object Adapt(System.Object source, System.Type destinationType)
- TDestination Adapt<TDestination>(System.Object source, TDestination destination)

### Nerosoft.Euonia.Mapping.AutomapperTypeAdapterFactory

**类型**：class
接口 Nerosoft.Euonia.Mapping.ITypeAdapterFactory

- new Nerosoft.Euonia.Mapping.AutomapperTypeAdapterFactory(AutoMapper.IMapper mapper)
- Nerosoft.Euonia.Mapping.ITypeAdapter Create()

### Nerosoft.Euonia.Mapping.ByteArrayToInt64Converter

**类型**：class
接口 IValueConverter<System.Byte[], System.Int64>

- new Nerosoft.Euonia.Mapping.ByteArrayToInt64Converter()
- System.Int64 Convert(System.Byte[] sourceMember, AutoMapper.ResolutionContext context)

### JsonValueConverter<T>

**类型**：class
接口 IValueConverter<System.String, T>

- new JsonValueConverter<T>()
- T Convert(System.String sourceMember, AutoMapper.ResolutionContext context)
