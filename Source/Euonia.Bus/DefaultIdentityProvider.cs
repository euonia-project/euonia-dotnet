using System.Security.Principal;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 默认的消息身份提供程序，通过访问器委托获取当前消息的身份主体。
/// </summary>
internal class DefaultIdentityProvider : IIdentityProvider
{
	/// <summary>
	/// 用于获取当前身份主体的访问器委托。
	/// </summary>
	private readonly IdentityAccessor _accessor;

	/// <summary>
	/// 初始化 <see cref="DefaultIdentityProvider"/> 类的新实例。
	/// </summary>
	/// <param name="accessor">用于获取当前身份主体的访问器委托。</param>
	public DefaultIdentityProvider(IdentityAccessor accessor)
	{
		_accessor = accessor;
	}

	/// <inheritdoc />
	public IPrincipal GetIdentity()
	{
		return _accessor();
	}
}