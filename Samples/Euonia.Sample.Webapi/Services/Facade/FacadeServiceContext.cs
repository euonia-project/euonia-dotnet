using Nerosoft.Euonia.Application;

namespace Nerosoft.Euonia.Sample.Facade;

/// <summary>
/// 示例应用的服务上下文：开启应用服务自动注册，使门面实现按约定装配到容器。
/// </summary>
internal class FacadeServiceContext : ServiceContextBase
{
	/// <inheritdoc/>
	public override bool AutoRegisterApplicationService => true;
}