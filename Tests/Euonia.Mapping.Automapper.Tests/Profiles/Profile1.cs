using AutoMapper;
using Nerosoft.Euonia.Mapping.Tests.Models;

namespace Nerosoft.Euonia.Mapping.Tests.Profiles;

/// <summary>
/// 定义 <see cref="SourceObject1"/> 到 <see cref="DestinationObject1"/> 的 AutoMapper 映射配置。
/// </summary>
public class Profile1 : Profile
{
    /// <summary>
    /// 初始化 <see cref="Profile1"/> 并注册源对象到目标对象的映射。
    /// </summary>
    public Profile1()
    {
        CreateMap<SourceObject1, DestinationObject1>();
    }
}