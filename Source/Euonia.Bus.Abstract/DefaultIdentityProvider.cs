using System.Security.Principal;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The default message identity provider.
/// </summary>
internal class DefaultIdentityProvider : IIdentityProvider
{
	private readonly IdentityAccessor _accessor;

	public DefaultIdentityProvider(IdentityAccessor accessor)
	{
		_accessor = accessor;
	}

	public IPrincipal GetIdentity()
	{
		return _accessor();
	}
}