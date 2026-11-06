using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Sample.Domain.Mappers;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// 为限界上下文注册领域层服务。
/// </summary>
/// <remarks>
/// 本模块负责组装并注册领域所需的服务、验证器与映射配置。它声明了对
/// <see cref="AutomapperModule"/> 与 <see cref="ValidationModule"/> 的依赖，
/// 以确保本模块初始化时映射与验证基础设施已可用。
/// </remarks>
/// <seealso cref="ModuleContextBase"/>
[DependsOn(typeof(AutomapperModule), typeof(ValidationModule))]
public class DomainServiceModule : ModuleContextBase
{
	/// <summary>
	/// 在模块服务配置的早期阶段注册短标识解析器与 AutoMapper 配置。
	/// </summary>
	/// <param name="context">模块化框架提供的服务配置上下文。</param>
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddTransient(typeof(ShortUniqueIdResolver<,>));
		Configure<AutomapperOptions>(options =>
		{
			options.AddProfile<UserMapperProfile>();
		});
	}
}
