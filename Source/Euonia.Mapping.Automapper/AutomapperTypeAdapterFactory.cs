using AutoMapper;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 使用 AutoMapper 实现 <see cref="ITypeAdapterFactory"/>。
/// </summary>
public class AutomapperTypeAdapterFactory : ITypeAdapterFactory
{
    private readonly IMapper _mapper;

    /// <summary>
    /// 初始化 <see cref="AutomapperTypeAdapterFactory"/> 的新实例。
    /// </summary>
    /// <param name="mapper">用于创建类型适配器的 AutoMapper 映射器。</param>
    public AutomapperTypeAdapterFactory(IMapper mapper)
    {
        _mapper = mapper;
    }

    /// <inheritdoc />
    public ITypeAdapter Create()
    {
        return new AutomapperTypeAdapter(_mapper);
    }
}