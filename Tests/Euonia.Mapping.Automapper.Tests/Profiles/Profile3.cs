using AutoMapper;
using Nerosoft.Euonia.Mapping.Tests.Models;

namespace Nerosoft.Euonia.Mapping.Tests;

/// <summary>
/// 定义 <see cref="SourceObject3"/> 到 <see cref="DestinationObject3"/> 的 AutoMapper 映射配置。
/// </summary>
public class Profile3 : Profile
{
	/// <summary>
	/// 初始化 <see cref="Profile3"/> 并注册使用值解析器拼接全名的映射规则。
	/// </summary>
	public Profile3()
	{
		CreateMap<SourceObject3, DestinationObject3>()
			.ForMember(dest => dest.FullName, options => options.MapFrom<FullNameValueResolver<SourceObject3,DestinationObject3>>());
	}
}