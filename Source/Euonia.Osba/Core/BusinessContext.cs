using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 为业务逻辑执行提供上下文入口。
/// 封装对环境 <see cref="IServiceProvider"/>、当前用户主体的访问，
/// 以及解析服务和创建实例的辅助方法，同时将此 <see cref="BusinessContext"/>
/// 传播到实现了 <see cref="IUseBusinessContext"/> 的已创建对象。
/// </summary>
public class BusinessContext
{
	/// <summary>
	/// 初始化 <see cref="BusinessContext"/> 类的新实例。
	/// </summary>
	/// <param name="contextAccessor">用于公开当前 <see cref="IServiceProvider"/> 的访问器。</param>
	public BusinessContext(BusinessContextAccessor contextAccessor)
	{
		ContextAccessor = contextAccessor;
		User = contextAccessor.ServiceProvider.GetService<UserPrincipal>();
	}

	/// <summary>
	/// 获取用于获取当前服务提供程序的底层 <see cref="BusinessContextAccessor"/>。
	/// 内部使用，以限制程序集外部的访问。
	/// </summary>
	internal BusinessContextAccessor ContextAccessor { get; }

	/// <summary>
	/// 获取当前活动用户的 <see cref="ClaimsPrincipal"/>（如果可用）。
	/// 当未设置用户时返回 <c>null</c>。
	/// </summary>
	public ClaimsPrincipal Principal => User?.Claims;

	/// <summary>
	/// 获取或设置表示应用程序用户上下文的当前 <see cref="UserPrincipal"/>。
	/// </summary>
	public UserPrincipal User { get; }

	/// <summary>
	/// 从 <see cref="ContextAccessor"/> 获取当前的 <see cref="IServiceProvider"/>。
	/// 如果没有可用的服务提供程序，则可能为 <c>null</c>。
	/// </summary>
	public IServiceProvider CurrentServiceProvider => ContextAccessor.ServiceProvider;

	/// <summary>
	/// 从当前服务提供程序解析 <typeparamref name="T"/> 类型的必需服务。
	/// </summary>
	/// <typeparam name="T">要解析的服务类型。</typeparam>
	/// <returns>解析出的服务实例。</returns>
	/// <exception cref="NullReferenceException">当 <see cref="CurrentServiceProvider"/> 为 <c>null</c> 时抛出。</exception>
	public T GetRequiredService<T>()
	{
		if (CurrentServiceProvider == null)
		{
			throw new NullReferenceException(nameof(CurrentServiceProvider));
		}

		var result = CurrentServiceProvider.GetRequiredService<T>();
		return result;
	}

	/// <summary>
	/// 从当前服务提供程序解析指定 <paramref name="serviceType"/> 的必需服务。
	/// </summary>
	/// <param name="serviceType">要解析的服务类型。</param>
	/// <returns>解析出的服务实例。</returns>
	/// <exception cref="NullReferenceException">当 <see cref="CurrentServiceProvider"/> 为 <c>null</c> 时抛出。</exception>
	public object GetRequiredService(Type serviceType)
	{
		if (CurrentServiceProvider == null)
		{
			throw new NullReferenceException(nameof(CurrentServiceProvider));
		}

		return CurrentServiceProvider.GetRequiredService(serviceType);
	}

	/// <summary>
	/// 尝试从当前服务提供程序解析 <typeparamref name="T"/> 类型的服务。
	/// 如果服务未注册，则返回 <c>null</c>。
	/// </summary>
	/// <typeparam name="T">要解析的服务类型。</typeparam>
	/// <returns>解析出的服务实例；如果未找到，则为 <c>null</c>。</returns>
	/// <exception cref="NullReferenceException">当 <see cref="CurrentServiceProvider"/> 为 <c>null</c> 时抛出。</exception>
	public T GetService<T>()
	{
		if (CurrentServiceProvider == null)
		{
			throw new NullReferenceException(nameof(CurrentServiceProvider));
		}

		var result = CurrentServiceProvider.GetService<T>();
		return result;
	}

	/// <summary>
	/// 解析使用提供的 <paramref name="key"/> 的 <typeparamref name="T"/> 类型的键服务。
	/// 一些 DI 容器允许注册键服务；这会委托到一个扩展辅助。
	/// </summary>
	/// <typeparam name="T">要解析的服务类型。</typeparam>
	/// <param name="key">用于识别特定注册的键。必须不为 <c>null</c>。</param>
	/// <returns>解析出的键服务实例。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="key"/> 为 <c>null</c>（在 .NET 5+ 上）时抛出。</exception>
	/// <exception cref="NullReferenceException">当 <see cref="CurrentServiceProvider"/> 为 <c>null</c> 时抛出。</exception>
	public T GetKeyedService<T>(object key)
	{
#if NET5_0_OR_GREATER
  ArgumentNullException.ThrowIfNull(key);
#else
		ArgumentAssert.ThrowIfNull(key, nameof(key));
#endif
		if (CurrentServiceProvider == null)
		{
			throw new NullReferenceException(nameof(CurrentServiceProvider));
		}

		{
		}

		return CurrentServiceProvider.GetKeyedService<T>(key);
	}

	/// <summary>
	/// 使用构造函数最佳匹配的 <paramref name="parameters"/> 创建 <typeparamref name="T"/> 的实例。
	/// 如果服务提供程序可用，<see cref="ActivatorUtilities.CreateInstance(IServiceProvider, Type, object[])"/>
	/// 会被使用，以便构造函数注入可以发生。如果创建的实例实现了 <see cref="IUseBusinessContext"/>,
	/// 其 <see cref="IUseBusinessContext.BusinessContext"/> 会被设置为当前上下文。
	/// </summary>
	/// <typeparam name="T">要创建的具体类型。</typeparam>
	/// <param name="parameters">用于实例化的构造函数参数。</param>
	/// <returns><typeparamref name="T"/> 的新实例。</returns>
	public T CreateInstance<T>(params object[] parameters)
	{
		return (T)CreateInstance(typeof(T), parameters);
	}

	/// <summary>
	/// 使用构造函数最佳匹配的 <paramref name="parameters"/> 创建指定 <paramref name="objectType"/> 的实例。
	/// 如果服务提供程序可用，<see cref="ActivatorUtilities.CreateInstance(IServiceProvider, Type, object[])"/>
	/// 会被使用，以便构造函数注入可以发生。如果创建的实例实现了 <see cref="IUseBusinessContext"/>,
	/// 其 <see cref="IUseBusinessContext.BusinessContext"/> 会被设置为当前上下文。
	/// </summary>
	/// <param name="objectType">要创建的具体类型。</param>
	/// <param name="parameters">用于实例化的构造函数参数。</param>
	/// <returns><paramref name="objectType"/> 的新实例。</returns>
	public object CreateInstance(Type objectType, params object[] parameters)
	{
		object result;
		if (CurrentServiceProvider != null)
		{
			result = ActivatorUtilities.CreateInstance(CurrentServiceProvider, objectType, parameters);
		}
		else
		{
			result = Activator.CreateInstance(objectType, parameters);
		}

		if (result is IUseBusinessContext tmp)
		{
			tmp.BusinessContext = this;
		}

		return result;
	}

	/// <summary>
	/// 通过提供泛型类型参数创建一个泛型类型定义的实例。
	/// <paramref name="type"/> 参数必须是泛型类型定义（例如 typeof(Foo&lt;&gt;)）。
	/// </summary>
	/// <param name="type">要实例化的泛型类型定义。</param>
	/// <param name="paramTypes">要应用到泛型定义的具体类型参数。</param>
	/// <returns>构造后的泛型类型的新实例。</returns>
	public object CreateGenericInstance(Type type, params Type[] paramTypes)
	{
		var genericType = type.GetGenericTypeDefinition();
		var gt = genericType.MakeGenericType(paramTypes);
		return CreateInstance(gt);
	}
}