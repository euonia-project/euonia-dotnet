using Mapster;
using Nerosoft.Euonia.Mapping.Tests.Models;

namespace Nerosoft.Euonia.Mapping.Tests.Profiles;

/// <summary>
/// 注册 <see cref="SourceObject2"/> 到 <see cref="DestinationObject2"/> 的 Mapster 映射配置。
/// </summary>
public class Profile2 : IRegister
{
    /// <summary>
    /// 在给定配置中注册源对象到目标对象的映射，并拼接全名。
    /// </summary>
    /// <param name="config">Mapster 类型适配配置。</param>
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<SourceObject2, DestinationObject2>()
              .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
    }
}