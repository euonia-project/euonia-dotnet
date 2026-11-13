using Mapster;
using Nerosoft.Euonia.Mapping.Tests.Models;

namespace Nerosoft.Euonia.Mapping.Tests.Profiles;

/// <summary>
/// 注册 <see cref="SourceObject1"/> 到 <see cref="DestinationObject1"/> 的 Mapster 映射配置。
/// </summary>
public class Profile1 : IRegister
{
    /// <summary>
    /// 在给定配置中注册源对象到目标对象的映射。
    /// </summary>
    /// <param name="config">Mapster 类型适配配置。</param>
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<SourceObject1, DestinationObject1>();
    }
}