# Euonia.Mapping.Mapster 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Mapping.Mapster |
| 公共类型 | 7 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMapster(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

## 命名空间 Nerosoft.Euonia.Mapping

### Nerosoft.Euonia.Mapping.ByteArrayToInt64Converter

**类型**：class
接口 Mapster.IRegister

- new Nerosoft.Euonia.Mapping.ByteArrayToInt64Converter()
- void Register(Mapster.TypeAdapterConfig config)

### JsonValueConverter<TDest>

**类型**：class
接口 Mapster.IRegister

- new JsonValueConverter<TDest>()
- void Register(Mapster.TypeAdapterConfig config)

### Nerosoft.Euonia.Mapping.MapsterModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Mapping.MapsterModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void OnApplicationInitialization(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)

### Nerosoft.Euonia.Mapping.MapsterOptions

**类型**：class

- new Nerosoft.Euonia.Mapping.MapsterOptions()
- void AddProfile<TRegister>()
- void AddProfile<TRegister>(Func<TRegister> factory)
- void AddProfile(System.Type registerType)
- void AddProfile(Mapster.IRegister register)
- void AddProfiles(IEnumerable<Mapster.IRegister> registers)
- void AddProfiles(System.Reflection.Assembly[] assemblies)

### Nerosoft.Euonia.Mapping.MapsterTypeAdapter

**类型**：class
接口 Nerosoft.Euonia.Mapping.ITypeAdapter

- new Nerosoft.Euonia.Mapping.MapsterTypeAdapter(MapsterMapper.IMapper mapper)
- TDestination Adapt<TSource, TDestination>(TSource source)
- TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
- TDestination Adapt<TDestination>(System.Object source)
- System.Object Adapt(System.Object source, System.Type destinationType)
- TDestination Adapt<TDestination>(System.Object source, TDestination destination)

### Nerosoft.Euonia.Mapping.MapsterTypeAdapterFactory

**类型**：class
接口 Nerosoft.Euonia.Mapping.ITypeAdapterFactory

- new Nerosoft.Euonia.Mapping.MapsterTypeAdapterFactory(MapsterMapper.IMapper mapper)
- Nerosoft.Euonia.Mapping.ITypeAdapter Create()
