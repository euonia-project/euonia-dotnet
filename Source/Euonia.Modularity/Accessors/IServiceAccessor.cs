using Microsoft.Extensions.DependencyInjection;

namespace System;

/// <summary>
/// 提供从 IoC 容器解析服务的统一访问入口。
/// </summary>
/// <remarks>
/// <para>
/// 本接口作为 <see cref="IServiceProvider"/> 的封装，为应用层代码提供强类型的服务解析能力，
/// 避免在业务代码中直接依赖 <see cref="IServiceProvider"/>。
/// </para>
/// <para>
/// 实现类型注册为 <see cref="ISingletonDependency"/>，
/// 因此在应用的整个生命周期内共享同一个实例。
/// </para>
/// </remarks>
/// <seealso cref="IServiceProvider"/>
/// <seealso cref="ISingletonDependency"/>
public interface IServiceAccessor : ISingletonDependency
{
	/// <summary>
	/// 获取或设置底层的 <see cref="IServiceProvider"/> 实例。
	/// </summary>
	/// <value>用于解析服务依赖的服务提供程序。</value>
	IServiceProvider ServiceProvider { get; set; }

	/// <summary>
	/// 从 <see cref="ServiceProvider"/> 中解析指定类型的服务实例。
	/// </summary>
	/// <typeparam name="T">要解析的服务类型。</typeparam>
	/// <returns>
	/// 服务实例；如果未注册该类型，则返回 <c>null</c>。
	/// </returns>
	T GetService<T>();

	/// <summary>
	/// 从 <see cref="ServiceProvider"/> 中解析指定类型的服务实例。
	/// </summary>
	/// <typeparam name="T">要解析的服务类型。</typeparam>
	/// <returns>服务实例。</returns>
	/// <exception cref="InvalidOperationException">
	/// 当请求的服务类型未在 IoC 容器中注册时抛出。
	/// </exception>
	T GetRequiredService<T>();

	/// <summary>
	/// 从 <see cref="ServiceProvider"/> 中按指定键解析服务实例。
	/// </summary>
	/// <param name="key">用于标识服务注册的键名。</param>
	/// <typeparam name="T">要解析的服务类型。</typeparam>
	/// <returns>与指定键关联的服务实例；如果未注册该类型，则返回 <c>null</c>。</returns>
	T GetKeyedService<T>(object key);

	/// <summary>
	/// 从 <see cref="ServiceProvider"/> 中按指定键解析必需的服务实例。
	/// </summary>
	/// <typeparam name="T">要解析的服务类型。</typeparam>
	/// <param name="name">用于标识服务注册的键名。</param>
	/// <returns>与指定键关联的服务实例。</returns>
	/// <exception cref="InvalidOperationException">
	/// 当请求的服务类型与指定键的组合未在 IoC 容器中注册时抛出。
	/// </exception>
	T GetRequiredKeyedService<T>(object name);

	/// <summary>
	/// 从 <see cref="ServiceProvider"/> 中按 <see cref="Type"/> 解析服务实例。
	/// </summary>
	/// <param name="type">要解析的服务类型。</param>
	/// <returns>
	/// 服务实例；如果未注册该类型，则返回 <c>null</c>。
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="type"/> 为 <c>null</c> 时抛出。
	/// </exception>
	object GetService(Type type);
}