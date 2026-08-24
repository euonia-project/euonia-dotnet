using MapsterMapper;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 使用 Mapster 实现 <see cref="ITypeAdapter"/>。
/// </summary>
public class MapsterTypeAdapter : ITypeAdapter
{
    private readonly IMapper _mapper;

    /// <summary>
    /// 初始化 <see cref="MapsterTypeAdapter"/> 的新实例。
    /// </summary>
    /// <param name="mapper">用于执行映射的 Mapster 映射器。</param>
    public MapsterTypeAdapter(IMapper mapper)
    {
        _mapper = mapper;
    }

    /// <inheritdoc />
    public TDestination Adapt<TSource, TDestination>(TSource source)
        where TSource : class
        where TDestination : class
    {
        return _mapper.Map<TSource, TDestination>(source);
    }

    /// <inheritdoc />
    public TDestination Adapt<TSource, TDestination>(TSource source, TDestination destination)
        where TSource : class
        where TDestination : class
    {
        return _mapper.Map(source, destination);
    }

    /// <inheritdoc />
    public TDestination Adapt<TDestination>(object source) where TDestination : class
    {
        return _mapper.Map<TDestination>(source);
    }

    /// <inheritdoc />
    public object Adapt(object source, Type destinationType)
    {
        return _mapper.Map(source, source.GetType(), destinationType);
    }

    /// <inheritdoc />
    public TDestination Adapt<TDestination>(object source, TDestination destination)
        where TDestination : class
    {
        return _mapper.Map(source, destination);
    }
}