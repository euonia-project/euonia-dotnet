using Nerosoft.Euonia.Application;

namespace Nerosoft.Euonia.Sample.Facade;

internal class FacadeServiceContext : ServiceContextBase
{
	/// <inheritdoc/>
	public override bool AutoRegisterApplicationService => true;
}