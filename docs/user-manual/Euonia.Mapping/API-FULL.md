# Euonia.Mapping 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Mapping |
| 公共类型 | 5 |
| 命名空间 | 2 |

## 命名空间 (global)

### Extensions

**类型**：static class

- static TDestination ProjectedAs<TDestination>(System.Object source)
- static System.Object ProjectedAs(System.Object source, System.Type destinationType)
- static List<TDestination> ProjectedAsCollection<TDestination>(IEnumerable<System.Object> items)

## 命名空间 Nerosoft.Euonia.Mapping

### Nerosoft.Euonia.Mapping.ITypeAdapter

**类型**：interface

- TDestination Adapt<TSource, TDestination>(TSource source)
- TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
- TDestination Adapt<TDestination>(System.Object source)
- System.Object Adapt(System.Object source, System.Type destinationType)
- TDestination Adapt<TDestination>(System.Object source, TDestination destination)

### Nerosoft.Euonia.Mapping.ITypeAdapterFactory

**类型**：interface

- Nerosoft.Euonia.Mapping.ITypeAdapter Create()

### Nerosoft.Euonia.Mapping.TypeAdapter

**类型**：class

- new Nerosoft.Euonia.Mapping.TypeAdapter()
- static TDestination ProjectedAs<TSource, TDestination>(TSource source)
- static TDestination ProjectedAs<TSource, TDestination>(TSource source, TDestination destination)
- static TDestination ProjectedAs<TDestination>(System.Object item)
- static List<TDestination> ProjectedAsCollection<TDestination>(IEnumerable<System.Object> items)

### Nerosoft.Euonia.Mapping.TypeAdapterFactory

**类型**：static class

- static Nerosoft.Euonia.Mapping.ITypeAdapter CreateAdapter()
- static void SetCurrent(Nerosoft.Euonia.Mapping.ITypeAdapterFactory adapterFactory)
