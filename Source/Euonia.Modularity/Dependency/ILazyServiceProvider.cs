namespace System;

/// <summary>
/// 表示可用于以延迟初始化方式检索服务的服务提供程序。
/// </summary>
/// <remarks>
/// 服务实例在首次访问时解析，并按服务类型（带键重载还包含服务键）缓存，后续请求返回同一实例；
/// <c>GetRequiredService</c> 系列在服务不可用时抛出异常，其余重载返回 <c>null</c> 或指定的默认值。
/// </remarks>
public interface ILazyServiceProvider
{
	/// <summary>
	/// 获取类型为 <typeparamref name="T"/> 的服务；若该服务未注册则抛出异常。
	/// </summary>
	/// <typeparam name="T">要获取的服务类型。</typeparam>
	/// <returns>解析得到的服务实例。</returns>
	/// <exception cref="InvalidOperationException">当 <typeparamref name="T"/> 类型的服务未注册时抛出。</exception>
	T GetRequiredService<T>();

	/// <summary>
	/// 获取指定类型的服务；若该服务未注册则抛出异常。
	/// </summary>
	/// <param name="serviceType">要获取的服务类型；不能为 <c>null</c>。</param>
	/// <returns>解析得到的服务实例。</returns>
	/// <exception cref="InvalidOperationException">当 <paramref name="serviceType"/> 对应的服务未注册时抛出。</exception>
	object GetRequiredService(Type serviceType);

	/// <summary>
	/// 获取类型为 <typeparamref name="T"/> 的服务；若该服务未注册则返回 <typeparamref name="T"/> 的默认值。
	/// </summary>
	/// <typeparam name="T">要获取的服务类型。</typeparam>
	/// <returns>解析得到的服务实例；服务未注册时，引用类型为 <c>null</c>，值类型为其默认值。</returns>
	T GetService<T>();

	/// <summary>
	/// 获取指定类型的服务；若该服务未注册则返回 <c>null</c>。
	/// </summary>
	/// <param name="serviceType">要获取的服务类型；不能为 <c>null</c>。</param>
	/// <returns>解析得到的服务实例；服务未注册时为 <c>null</c>。</returns>
	object GetService(Type serviceType);

	/// <summary>
	/// 获取类型为 <typeparamref name="T"/> 的服务；若该服务未注册则返回 <paramref name="defaultValue"/>。
	/// </summary>
	/// <typeparam name="T">要获取的服务类型。</typeparam>
	/// <param name="defaultValue">服务未注册时返回的默认值。</param>
	/// <returns>解析得到的服务实例，或 <paramref name="defaultValue"/>。</returns>
	T GetService<T>(T defaultValue);

	/// <summary>
	/// 获取指定类型的服务；若该服务未注册则返回 <paramref name="defaultValue"/>。
	/// </summary>
	/// <param name="serviceType">要获取的服务类型；不能为 <c>null</c>。</param>
	/// <param name="defaultValue">服务未注册时返回的默认值。</param>
	/// <returns>解析得到的服务实例，或 <paramref name="defaultValue"/>。</returns>
	object GetService(Type serviceType, object defaultValue);

	/// <summary>
	/// 使用指定的工厂方法获取指定类型的服务，并按服务类型缓存工厂结果。
	/// </summary>
	/// <param name="serviceType">要获取的服务类型；不能为 <c>null</c>。</param>
	/// <param name="factory">接收 <see cref="IServiceProvider"/> 并返回服务实例的工厂方法。</param>
	/// <returns>由 <paramref name="factory"/> 创建的服务实例。</returns>
	/// <remarks>
	/// 该重载不查询服务容器中已注册的服务，而是直接调用 <paramref name="factory"/>。
	/// </remarks>
	object GetService(Type serviceType, Func<IServiceProvider, object> factory);

	/// <summary>
	/// 使用指定的工厂方法获取类型为 <typeparamref name="T"/> 的服务，并按服务类型缓存工厂结果。
	/// </summary>
	/// <typeparam name="T">要获取的服务类型。</typeparam>
	/// <param name="factory">接收 <see cref="IServiceProvider"/> 并返回服务实例的工厂方法。</param>
	/// <returns>由 <paramref name="factory"/> 创建的服务实例。</returns>
	/// <remarks>
	/// 该重载不查询服务容器中已注册的服务，而是直接调用 <paramref name="factory"/>。
	/// </remarks>
	T GetService<T>(Func<IServiceProvider, object> factory);

	/// <summary>
	/// 获取与指定服务键关联的指定类型的服务；若不存在则返回 <c>null</c>。
	/// </summary>
	/// <param name="serviceType">要获取的服务类型；不能为 <c>null</c>。</param>
	/// <param name="serviceKey">用于标识服务的键。</param>
	/// <returns>与 <paramref name="serviceKey"/> 关联的服务实例；不存在时为 <c>null</c>。</returns>
	object GetKeyedService(Type serviceType, object serviceKey);

	/// <summary>
	/// 获取与指定服务键关联的类型为 <typeparamref name="T"/> 的服务；若不存在则抛出异常。
	/// </summary>
	/// <typeparam name="T">要获取的服务类型。</typeparam>
	/// <param name="serviceKey">用于标识服务的键。</param>
	/// <returns>与 <paramref name="serviceKey"/> 关联的服务实例。</returns>
	/// <exception cref="InvalidOperationException">当 <paramref name="serviceKey"/> 对应的服务未注册时抛出。</exception>
	T GetRequiredKeyedService<T>(object serviceKey);

	/// <summary>
	/// 获取与指定服务键关联的类型为 <typeparamref name="T"/> 的服务；若不存在则返回 <typeparamref name="T"/> 的默认值。
	/// </summary>
	/// <typeparam name="T">要获取的服务类型。</typeparam>
	/// <param name="serviceKey">用于标识服务的键。</param>
	/// <returns>与 <paramref name="serviceKey"/> 关联的服务实例；不存在时，引用类型为 <c>null</c>，值类型为其默认值。</returns>
	T GetKeyedService<T>(object serviceKey);

	/// <summary>
	/// 获取与指定服务键关联的指定类型的服务；若不存在则抛出异常。
	/// </summary>
	/// <param name="serviceType">要获取的服务类型；不能为 <c>null</c>。</param>
	/// <param name="serviceKey">用于标识服务的键。</param>
	/// <returns>与 <paramref name="serviceKey"/> 关联的服务实例。</returns>
	/// <exception cref="InvalidOperationException">当 <paramref name="serviceKey"/> 对应的服务未注册时抛出。</exception>
	object GetRequiredKeyedService(Type serviceType, object serviceKey);

	/// <summary>
	/// 获取与指定服务键关联的类型为 <typeparamref name="T"/> 的服务；无法获取时返回 <paramref name="defaultValue"/>。
	/// </summary>
	/// <typeparam name="T">要获取的服务类型。</typeparam>
	/// <param name="serviceKey">用于标识服务的键。</param>
	/// <param name="defaultValue">服务不存在时返回的默认值。</param>
	/// <returns>与 <paramref name="serviceKey"/> 关联的服务实例，或 <paramref name="defaultValue"/>。</returns>
	/// <remarks>
	/// <see cref="LazyServiceProvider"/> 的实现按服务键与类型缓存 <paramref name="defaultValue"/>，不查询服务容器。
	/// </remarks>
	T GetKeyedService<T>(object serviceKey, T defaultValue);

	/// <summary>
	/// 使用指定的工厂方法获取与指定服务键关联的类型为 <typeparamref name="T"/> 的服务，并按服务键与类型缓存工厂结果。
	/// </summary>
	/// <typeparam name="T">要获取的服务类型。</typeparam>
	/// <param name="serviceKey">用于标识服务的键。</param>
	/// <param name="factory">接收 <see cref="IServiceProvider"/> 并返回服务实例的工厂方法。</param>
	/// <returns>由 <paramref name="factory"/> 创建的服务实例。</returns>
	/// <remarks>
	/// 该重载不查询服务容器中已注册的服务，而是直接调用 <paramref name="factory"/>。
	/// </remarks>
	T GetKeyedService<T>(object serviceKey, Func<IServiceProvider, T> factory);

	/// <summary>
	/// 使用指定的工厂方法获取与指定服务键关联的指定类型的服务，并按服务键与类型缓存工厂结果。
	/// </summary>
	/// <param name="serviceType">要获取的服务类型；不能为 <c>null</c>。</param>
	/// <param name="serviceKey">用于标识服务的键。</param>
	/// <param name="factory">接收 <see cref="IServiceProvider"/> 并返回服务实例的工厂方法。</param>
	/// <returns>由 <paramref name="factory"/> 创建的服务实例。</returns>
	/// <remarks>
	/// 该重载不查询服务容器中已注册的服务，而是直接调用 <paramref name="factory"/>。
	/// </remarks>
	object GetKeyedService(Type serviceType, object serviceKey, Func<IServiceProvider, object> factory);

	/// <summary>
	/// 获取与指定服务键关联的指定类型的服务；无法获取时返回 <paramref name="defaultValue"/>。
	/// </summary>
	/// <param name="serviceType">要获取的服务类型；不能为 <c>null</c>。</param>
	/// <param name="serviceKey">用于标识服务的键。</param>
	/// <param name="defaultValue">服务不存在时返回的默认值。</param>
	/// <returns>与 <paramref name="serviceKey"/> 关联的服务实例，或 <paramref name="defaultValue"/>。</returns>
	/// <remarks>
	/// <see cref="LazyServiceProvider"/> 的实现按服务键与类型缓存 <paramref name="defaultValue"/>，不查询服务容器。
	/// </remarks>
	object GetKeyedService(Type serviceType, object serviceKey, object defaultValue);
}