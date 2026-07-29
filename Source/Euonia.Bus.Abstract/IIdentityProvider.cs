using System.Security.Principal;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// Defines the identity provider.
/// </summary>
public interface IIdentityProvider
{
	/// <summary>
	/// Get the identity from authorization header.
	/// </summary>
	/// <returns></returns>
	IPrincipal GetIdentity();
}