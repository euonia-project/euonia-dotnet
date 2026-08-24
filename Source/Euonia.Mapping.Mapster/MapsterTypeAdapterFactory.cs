using MapsterMapper;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 使用 Mapster 实现 <see cref="ITypeAdapterFactory"/>。
/// </summary>
public class MapsterTypeAdapterFactory : ITypeAdapterFactory
{
    private readonly IMapper _mapper;

    /// <summary>
    /// 初始化 <see cref="MapsterTypeAdapterFactory"/> 的新实例。
    /// </summary>
    /// <param name="mapper">用于创建类型适配器的 Mapster 映射器。</param>
    public MapsterTypeAdapterFactory(IMapper mapper)
    {
        _mapper = mapper;
    }

    /// <inheritdoc />
    public ITypeAdapter Create()
    {
        return new MapsterTypeAdapter(_mapper);
    }
}