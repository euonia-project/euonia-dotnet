using AutoMapper;
using Nerosoft.Euonia.Mapping.Tests.Models;

namespace Nerosoft.Euonia.Mapping.Tests.Profiles;

/// <summary>
/// 定义 <see cref="SourceObject2"/> 到 <see cref="DestinationObject2"/> 的 AutoMapper 映射配置。
/// </summary>
public class Profile2 : Profile
{
    /// <summary>
    /// 初始化 <see cref="Profile2"/> 并注册拼接全名的映射规则。
    /// </summary>
    public Profile2()
    {
        CreateMap<SourceObject2, DestinationObject2>()
            .ForMember(dest => dest.FullName, options => options.MapFrom(GetFullName));
    }

    private static string GetFullName(SourceObject2 source, DestinationObject2 dest, string arg3, ResolutionContext context)
    {
        return $"{source.FirstName} {source.LastName}";
    }
}