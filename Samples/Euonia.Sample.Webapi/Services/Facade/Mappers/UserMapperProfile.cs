using AutoMapper;
using Nerosoft.Euonia.Sample.Persist.Entities;
using Nerosoft.Euonia.Sample.Domain.Commands;
using Nerosoft.Euonia.Sample.Domain.Dtos;

namespace Nerosoft.Euonia.Sample.Domain.Mappers;

/// <summary>
/// 用户相关对象之间的 AutoMapper 映射配置。
/// </summary>
/// <remarks>
/// 负责创建/更新命令的入参映射，以及用户实体到详情、列表数据传输对象的投影；详情中的角色名由实体角色集合展开。
/// </remarks>
public class UserMapperProfile : Profile
{
	/// <summary>
	/// 初始化映射配置并注册用户相关的映射关系。
	/// </summary>
	public UserMapperProfile()
	{
		CreateMap<UserCreateDto, UserCreateCommand>();
		CreateMap<UserUpdateDto, UserUpdateCommand>();

		CreateMap<UserEntity, UserDetailDto>()
			.ForMember(dto => dto.Roles, opt => opt.MapFrom(entity => entity.Roles.Select(role => role.Name)));
		CreateMap<UserEntity, UserListDto>();
	}
}