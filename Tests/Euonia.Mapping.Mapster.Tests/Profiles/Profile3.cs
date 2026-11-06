using Mapster;
using Nerosoft.Euonia.Mapping.Tests.Models;

namespace Nerosoft.Euonia.Mapping.Tests.Profiles;

/// <summary>
/// 注册 <see cref="SourceObject3"/> 到 <see cref="DestinationObject3"/> 的 Mapster 映射配置。
/// </summary>
public class Profile3 : IRegister
{
	private readonly StringHelper _helper;

	/// <summary>
	/// 初始化映射配置并注入用于拼接姓名的字符串辅助工具。
	/// </summary>
	/// <param name="helper">用于拼接姓名的字符串辅助工具。</param>
	public Profile3(StringHelper helper)
	{
		_helper = helper;
	}

	/// <summary>
	/// 在给定配置中注册源对象到目标对象的映射，并使用字符串辅助工具拼接全名。
	/// </summary>
	/// <param name="config">Mapster 类型适配配置。</param>
	public void Register(TypeAdapterConfig config)
	{
		config.ForType<SourceObject3, DestinationObject3>()
		      .Map(dest => dest.FullName, src => _helper.Combine(src.FirstName, src.LastName));
	}
}