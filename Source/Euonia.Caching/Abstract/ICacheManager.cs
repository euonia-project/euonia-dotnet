using System.Diagnostics.CodeAnalysis;
using Nerosoft.Euonia.Caching.Internal;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 此接口通过一些缓存管理器特有的方法和事件扩展了 <c>ICache</c> 接口。
/// </summary>
/// <typeparam name="TValue">缓存项值的类型。</typeparam>
public interface ICacheManager<TValue> : ICache<TValue>
{
    /// <summary>
    /// 当某项已成功添加到缓存时发生。
    /// <para>如果 <c>Add</c> 返回 false，则不会触发该事件。</para>
    /// </summary>
    event EventHandler<CacheActionEventArgs> OnAdd;

    /// <summary>
    /// 当调用 <c>Clear</c> 且缓存已被清空后发生。
    /// </summary>
    event EventHandler<CacheClearEventArgs> OnClear;

    /// <summary>
    /// 当调用 <c>ClearRegion</c> 且缓存区域已被清空后发生。
    /// </summary>
    event EventHandler<CacheClearRegionEventArgs> OnClearRegion;

    /// <summary>
    /// 当从缓存中检索到某项时发生。
    /// <para>该事件仅在缓存命中时触发，未命中不会触发！</para>
    /// </summary>
    event EventHandler<CacheActionEventArgs> OnGet;

    /// <summary>
    /// 当某项被放入缓存时发生。
    /// </summary>
    event EventHandler<CacheActionEventArgs> OnPut;

    /// <summary>
    /// 当某项已成功从缓存中移除时发生。
    /// </summary>
    event EventHandler<CacheActionEventArgs> OnRemove;

    /// <summary>
    /// 当某项因过期或例如内存压力驱逐而被缓存句柄移除时发生。
    /// <see cref="CacheItemRemovedEventArgs.Reason"/> 属性指示原因，而 <see cref="CacheItemRemovedEventArgs.Level"/> 指示
    /// 是哪个句柄触发了该事件。
    /// </summary>
    event EventHandler<CacheItemRemovedEventArgs> OnRemoveByHandle;

    /// <summary>
    /// 当某项已成功更新时发生。
    /// </summary>
    event EventHandler<CacheActionEventArgs> OnUpdate;

    /// <summary>
    /// 获取配置。
    /// </summary>
    /// <value>配置。</value>
    CacheManagerConfiguration Configuration { get; }

    /// <summary>
    /// 获取缓存名称。
    /// </summary>
    /// <value>缓存名称。</value>
    string Name { get; }

    /// <summary>
    /// 获取当前在缓存管理器中注册的缓存句柄列表。
    /// </summary>
    /// <value>缓存句柄。</value>
    /// <remarks>
    /// 此列表为只读，对返回的列表实例所做的任何更改都不会影响
    /// 缓存管理器实例的状态。
    /// </remarks>
    IEnumerable<BaseCacheHandle<TValue>> CacheHandles { get; }

    /// <summary>
    /// 向缓存中添加项，如果该项已存在，则使用
    /// <paramref name="updateValue"/> 函数更新该项。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="key">要更新的键。</param>
    /// <param name="addValue">
    /// 项不存在时应添加的值。
    /// </param>
    /// <param name="updateValue">
    /// 项已存在时执行更新的函数。
    /// </param>
    /// <returns>
    /// 已添加或已更新的值；如果更新未成功，则为 null。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    TValue AddOrUpdate(string key, TValue addValue, Func<TValue, TValue> updateValue);

    /// <summary>
    /// 向缓存中添加项，如果该项已存在，则使用
    /// <paramref name="updateValue"/> 函数更新该项。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="key">要更新的键。</param>
    /// <param name="region">要更新的键所属的区域。</param>
    /// <param name="addValue">
    /// 项不存在时应添加的值。
    /// </param>
    /// <param name="updateValue">
    /// 项已存在时执行更新的函数。
    /// </param>
    /// <returns>
    /// 已添加或已更新的值；如果更新未成功，则为 null。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/>、<paramref name="region"/> 或 <paramref name="updateValue"/>
    /// 为 null。
    /// </exception>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    TValue AddOrUpdate(string key, string region, TValue addValue, Func<TValue, TValue> updateValue);

    /// <summary>
    /// 向缓存中添加项，如果该项已存在，则使用
    /// <paramref name="updateValue"/> 函数更新该项。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="key">要更新的键。</param>
    /// <param name="addValue">
    /// 项不存在时应添加的值。
    /// </param>
    /// <param name="updateValue">
    /// 项已存在时执行更新的函数。
    /// </param>
    /// <param name="maxRetries">
    /// 发生版本冲突时应执行的尝试次数。
    /// 如果缓存在 <paramref name="maxRetries"/> 次尝试内无法完成更新，
    /// 此方法将返回 <c>Null</c>。
    /// </param>
    /// <returns>
    /// 已添加或已更新的值；如果更新未成功，则为 null。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    TValue AddOrUpdate(string key, TValue addValue, Func<TValue, TValue> updateValue, int maxRetries);

    /// <summary>
    /// 向缓存中添加项，如果该项已存在，则使用
    /// <paramref name="updateValue"/> 函数更新该项。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="key">要更新的键。</param>
    /// <param name="region">要更新的键所属的区域。</param>
    /// <param name="addValue">
    /// 项不存在时应添加的值。
    /// </param>
    /// <param name="updateValue">
    /// 项已存在时执行更新的函数。
    /// </param>
    /// <param name="maxRetries">
    /// 发生版本冲突时应执行的尝试次数。
    /// 如果缓存在 <paramref name="maxRetries"/> 次尝试内无法完成更新，
    /// 此方法将返回 <c>Null</c>。
    /// </param>
    /// <returns>
    /// 已添加或已更新的值；如果更新未成功，则为 null。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/>、<paramref name="region"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    TValue AddOrUpdate(string key, string region, TValue addValue, Func<TValue, TValue> updateValue, int maxRetries);

    /// <summary>
    /// 向缓存中添加项，如果该项已存在，则使用
    /// <paramref name="updateValue"/> 函数更新该项。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="addItem">应添加或更新的项。</param>
    /// <param name="updateValue">项存在时执行更新的函数。</param>
    /// <returns>
    /// 已添加或已更新的值；如果更新未成功，则为 null。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="addItem"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    TValue AddOrUpdate(CacheItem<TValue> addItem, Func<TValue, TValue> updateValue);

    /// <summary>
    /// 向缓存中添加项，如果该项已存在，则使用
    /// <paramref name="updateValue"/> 函数更新该项。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="addItem">应添加或更新的项。</param>
    /// <param name="updateValue">项存在时执行更新的函数。</param>
    /// <param name="maxRetries">
    /// 发生版本冲突时应执行的尝试次数。
    /// 如果缓存在 <paramref name="maxRetries"/> 次尝试内无法完成更新，
    /// 此方法将返回 <c>Null</c>。
    /// </param>
    /// <returns>
    /// 已添加或已更新的值；如果更新未成功，则为 null。
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="addItem"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    TValue AddOrUpdate(CacheItem<TValue> addItem, Func<TValue, TValue> updateValue, int maxRetries);

    /// <summary>
    /// 返回现有项，如果该项不存在则将其添加到缓存。
    /// 该方法返回现有项的值或新添加的 <paramref name="value"/>。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="value">应添加的值。</param>
    /// <returns>已添加的值或现有值。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="key"/> 或 <paramref name="value"/> 为 null。
    /// </exception>
    TValue GetOrAdd(string key, TValue value);

    /// <summary>
    /// 返回现有项，如果该项不存在则将其添加到缓存。
    /// 该方法返回现有项的值或新添加的 <paramref name="value"/>。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存区域。</param>
    /// <param name="value">应添加的值。</param>
    /// <returns>已添加的值或现有值。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="key"/>、<paramref name="region"/> 或 <paramref name="value"/> 为 null。
    /// </exception>
    TValue GetOrAdd(string key, string region, TValue value);

    /// <summary>
    /// 返回现有项，如果该项不存在则将其添加到缓存。
    /// 仅当该项不存在时才会计算 <paramref name="valueFactory"/>。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="valueFactory">创建应添加的值的方法。</param>
    /// <returns>已添加的值或现有值。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="key"/> 或 <paramref name="valueFactory"/> 为 null。
    /// </exception>
    TValue GetOrAdd(string key, Func<string, TValue> valueFactory);

    /// <summary>
    /// 返回现有项，如果该项不存在则将其添加到缓存。
    /// 仅当该项不存在时才会计算 <paramref name="valueFactory"/>。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存区域。</param>
    /// <param name="valueFactory">创建应添加的值的方法。</param>
    /// <returns>已添加的值或现有值。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="key"/> 或 <paramref name="valueFactory"/> 为 null。
    /// </exception>
    TValue GetOrAdd(string key, string region, Func<string, string, TValue> valueFactory);

    /// <summary>
    /// 返回现有项，如果该项不存在则将其添加到缓存。
    /// 仅当该项不存在时才会计算 <paramref name="valueFactory"/>。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="valueFactory">创建应添加的值的方法。</param>
    /// <returns>已添加的值或现有值。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="key"/> 或 <paramref name="valueFactory"/> 为 null。
    /// </exception>
    CacheItem<TValue> GetOrAdd(string key, Func<string, CacheItem<TValue>> valueFactory);

    /// <summary>
    /// 返回现有项，如果该项不存在则将其添加到缓存。
    /// 仅当该项不存在时才会计算 <paramref name="valueFactory"/>。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存区域。</param>
    /// <param name="valueFactory">创建应添加的值的方法。</param>
    /// <returns>已添加的值或现有值。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="key"/> 或 <paramref name="valueFactory"/> 为 null。
    /// </exception>
    CacheItem<TValue> GetOrAdd(string key, string region, Func<string, string, CacheItem<TValue>> valueFactory);

    /// <summary>
    /// 尝试检索现有项，如果该项不存在则将其添加到缓存。
    /// 仅当该项不存在时才会计算 <paramref name="valueFactory"/>。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="valueFactory">创建应添加的值的方法。</param>
    /// <param name="value">缓存值。</param>
    /// <returns><c>True</c> 表示操作成功；如果重试次数过多或 <paramref name="valueFactory"/> 返回 null，则为 <c>False</c>。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="key"/> 或 <paramref name="valueFactory"/> 为 null。
    /// </exception>
    bool TryGetOrAdd(string key, Func<string, TValue> valueFactory, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// 尝试检索现有项，如果该项不存在则将其添加到缓存。
    /// 仅当该项不存在时才会计算 <paramref name="valueFactory"/>。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存区域。</param>
    /// <param name="valueFactory">创建应添加的值的方法。</param>
    /// <param name="value">缓存值。</param>
    /// <returns><c>True</c> 表示操作成功；如果重试次数过多或 <paramref name="valueFactory"/> 返回 null，则为 <c>False</c>。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="key"/> 或 <paramref name="valueFactory"/> 为 null。
    /// </exception>
    bool TryGetOrAdd(string key, string region, Func<string, string, TValue> valueFactory, [MaybeNullWhen(false)] out TValue value);

    /// <summary>
    /// 尝试检索现有项，如果该项不存在则将其添加到缓存。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="valueFactory">创建应添加的值的方法。</param>
    /// <param name="item">缓存项。</param>
    /// <returns><c>True</c> 表示操作成功；如果重试次数过多或 <paramref name="valueFactory"/> 返回 null，则为 <c>False</c>。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="key"/> 或 <paramref name="valueFactory"/> 为 null。
    /// </exception>
    bool TryGetOrAdd(string key, Func<string, CacheItem<TValue>> valueFactory, [MaybeNullWhen(false)] out CacheItem<TValue> item);

    /// <summary>
    /// 尝试检索现有项，如果该项不存在则将其添加到缓存。
    /// </summary>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存区域。</param>
    /// <param name="valueFactory">创建应添加的值的方法。</param>
    /// <param name="item">缓存项。</param>
    /// <returns><c>True</c> 表示操作成功；如果重试次数过多或 <paramref name="valueFactory"/> 返回 null，则为 <c>False</c>。</returns>
    /// <exception cref="ArgumentException">
    /// 如果 <paramref name="key"/> 或 <paramref name="valueFactory"/> 为 null。
    /// </exception>
    bool TryGetOrAdd(string key, string region, Func<string, string, CacheItem<TValue>> valueFactory, [MaybeNullWhen(false)] out CacheItem<TValue> item);

    /// <summary>
    /// 更新缓存中的现有键。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    /// <param name="key">要更新的键。</param>
    /// <param name="updateValue">执行更新的函数。</param>
    /// <returns>已更新的值；如果更新未成功，则为 null。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    TValue Update(string key, Func<TValue, TValue> updateValue);

    /// <summary>
    /// 更新缓存中的现有键。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    /// <param name="key">要更新的键。</param>
    /// <param name="region">要更新的键所属的区域。</param>
    /// <param name="updateValue">执行更新的函数。</param>
    /// <returns>已更新的值；如果更新未成功，则为 null。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/>、<paramref name="region"/> 或 <paramref name="updateValue"/>
    /// 为 null。
    /// </exception>
    TValue Update(string key, string region, Func<TValue, TValue> updateValue);

    /// <summary>
    /// 更新缓存中的现有键。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    /// <param name="key">要更新的键。</param>
    /// <param name="updateValue">执行更新的函数。</param>
    /// <param name="maxRetries">
    /// 发生版本冲突时应执行的尝试次数。
    /// 如果缓存在 <paramref name="maxRetries"/> 次尝试内无法完成更新，
    /// 此方法将返回 <c>Null</c>。
    /// </param>
    /// <returns>已更新的值；如果更新未成功，则为 null。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    TValue Update(string key, Func<TValue, TValue> updateValue, int maxRetries);

    /// <summary>
    /// 更新缓存中的现有键。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    /// <param name="key">要更新的键。</param>
    /// <param name="region">要更新的键所属的区域。</param>
    /// <param name="updateValue">执行更新的函数。</param>
    /// <param name="maxRetries">
    /// 发生版本冲突时应执行的尝试次数。
    /// 如果缓存在 <paramref name="maxRetries"/> 次尝试内无法完成更新，
    /// 此方法将返回 <c>Null</c>。
    /// </param>
    /// <returns>已更新的值；如果更新未成功，则为 null。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/>、<paramref name="region"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    TValue Update(string key, string region, Func<TValue, TValue> updateValue, int maxRetries);

    /// <summary>
    /// 尝试更新缓存中的现有键。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="key">要更新的键。</param>
    /// <param name="updateValue">执行更新的函数。</param>
    /// <param name="value">已更新的值；如果更新未成功，则为 null。</param>
    /// <returns><c>True</c> 表示更新操作成功，否则为 <c>False</c>。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    bool TryUpdate(string key, Func<TValue, TValue> updateValue, out TValue value);

    /// <summary>
    /// 尝试更新缓存中的现有键。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="key">要更新的键。</param>
    /// <param name="region">要更新的键所属的区域。</param>
    /// <param name="updateValue">执行更新的函数。</param>
    /// <param name="value">已更新的值；如果更新未成功，则为 null。</param>
    /// <returns><c>True</c> 表示更新操作成功，否则为 <c>False</c>。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/>、<paramref name="region"/> 或 <paramref name="updateValue"/>
    /// 为 null。
    /// </exception>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    bool TryUpdate(string key, string region, Func<TValue, TValue> updateValue, out TValue value);

    /// <summary>
    /// 尝试更新缓存中的现有键。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="key">要更新的键。</param>
    /// <param name="updateValue">执行更新的函数。</param>
    /// <param name="maxRetries">
    /// 发生版本冲突时应执行的尝试次数。
    /// 如果缓存在 <paramref name="maxRetries"/> 次尝试内无法完成更新，
    /// 此方法将返回 <c>False</c>。
    /// </param>
    /// <param name="value">已更新的值；如果更新未成功，则为 null。</param>
    /// <returns><c>True</c> 表示更新操作成功，否则为 <c>False</c>。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    bool TryUpdate(string key, Func<TValue, TValue> updateValue, int maxRetries, out TValue value);

    /// <summary>
    /// 尝试更新缓存中的现有键。
    /// <para>
    /// 缓存管理器将确保更新始终针对最新版本进行。
    /// </para>
    /// <para>
    /// 如果发生版本冲突，例如多个缓存客户端尝试写入同一个键，
    /// 而在更新过程中其他客户端更改了该键的值，
    /// 缓存管理器将重试该操作。
    /// </para>
    /// <para>
    /// 每次重试时，都会以缓存中存储的最新值调用 <paramref name="updateValue"/> 函数。
    /// </para>
    /// </summary>
    /// <param name="key">要更新的键。</param>
    /// <param name="region">要更新的键所属的区域。</param>
    /// <param name="updateValue">执行更新的函数。</param>
    /// <param name="maxRetries">
    /// 发生版本冲突时应执行的尝试次数。
    /// 如果缓存在 <paramref name="maxRetries"/> 次尝试内无法完成更新，
    /// 此方法将返回 <c>False</c>。
    /// </param>
    /// <param name="value">已更新的值；如果更新未成功，则为 null。</param>
    /// <returns><c>True</c> 表示更新操作成功，否则为 <c>False</c>。</returns>
    /// <exception cref="ArgumentNullException">
    /// 如果 <paramref name="key"/>、<paramref name="region"/> 或 <paramref name="updateValue"/> 为 null。
    /// </exception>
    /// <remarks>
    /// 如果缓存不使用分布式缓存系统，Update 的行为与 Get 加 Put 完全相同。
    /// </remarks>
    bool TryUpdate(string key, string region, Func<TValue, TValue> updateValue, int maxRetries, out TValue value);

    /// <summary>
    /// 为所有缓存层中的 <paramref name="key"/> 显式设置过期 <paramref name="mode"/> 和 <paramref name="timeout"/>。
    /// 此操作会覆盖为此特定项按缓存句柄配置的任何过期设置。
    /// </summary>
    /// <remarks>
    /// 如果在使用分布式缓存，请勿在对并发要求严格的场景中使用此方法，因为 <code>Expire</code> 不是原子操作；
    /// <code>Expire</code> 使用 <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> 存储带有新过期设置的项。
    /// </remarks>
    /// <param name="key">缓存键。</param>
    /// <param name="mode">过期模式。</param>
    /// <param name="timeout">过期超时时间。</param>
    void Expire(string key, CacheExpirationMode mode, TimeSpan timeout);

    /// <summary>
    /// 为所有缓存层中 <paramref name="region"/> 内的 <paramref name="key"/> 显式设置过期 <paramref name="mode"/> 和 <paramref name="timeout"/>。
    /// 此操作会覆盖为此特定项按缓存句柄配置的任何过期设置。
    /// </summary>
    /// <remarks>
    /// 如果在使用分布式缓存，请勿在对并发要求严格的场景中使用此方法，因为 <code>Expire</code> 不是原子操作；
    /// <code>Expire</code> 使用 <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> 存储带有新过期设置的项。
    /// </remarks>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存区域。</param>
    /// <param name="mode">过期模式。</param>
    /// <param name="timeout">过期超时时间。</param>
    void Expire(string key, string region, CacheExpirationMode mode, TimeSpan timeout);

    /// <summary>
    /// 为所有缓存层中的 <paramref name="key"/> 显式设置绝对过期日期。
    /// 此操作会覆盖为此特定项按缓存句柄配置的任何过期设置。
    /// </summary>
    /// <remarks>
    /// 如果在使用分布式缓存，请勿在对并发要求严格的场景中使用此方法，因为 <code>Expire</code> 不是原子操作；
    /// <code>Expire</code> 使用 <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> 存储带有新过期设置的项。
    /// </remarks>
    /// <param name="key">缓存键。</param>
    /// <param name="absoluteExpiration">
    /// 过期日期，该值必须大于零。
    /// </param>
    void Expire(string key, DateTimeOffset absoluteExpiration);

    /// <summary>
    /// 为所有缓存层中 <paramref name="region"/> 内的 <paramref name="key"/> 显式设置绝对过期日期。
    /// 此操作会覆盖为此特定项按缓存句柄配置的任何过期设置。
    /// </summary>
    /// <remarks>
    /// 如果在使用分布式缓存，请勿在对并发要求严格的场景中使用此方法，因为 <code>Expire</code> 不是原子操作；
    /// <code>Expire</code> 使用 <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> 存储带有新过期设置的项。
    /// </remarks>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存区域。</param>
    /// <param name="absoluteExpiration">
    /// 过期日期，该值必须大于零。
    /// </param>
    void Expire(string key, string region, DateTimeOffset absoluteExpiration);

    /// <summary>
    /// 为所有缓存层中的 <paramref name="key"/> 显式设置滑动过期时间。
    /// 此操作会覆盖为此特定项按缓存句柄配置的任何过期设置。
    /// </summary>
    /// <remarks>
    /// 如果在使用分布式缓存，请勿在对并发要求严格的场景中使用此方法，因为 <code>Expire</code> 不是原子操作；
    /// <code>Expire</code> 使用 <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> 存储带有新过期设置的项。
    /// </remarks>
    /// <param name="key">缓存键。</param>
    /// <param name="slidingExpiration">
    /// 过期超时时间，该值必须大于零。
    /// </param>
    void Expire(string key, TimeSpan slidingExpiration);

    /// <summary>
    /// 为所有缓存层中 <paramref name="region"/> 内的 <paramref name="key"/> 显式设置滑动过期时间。
    /// 此操作会覆盖为此特定项按缓存句柄配置的任何过期设置。
    /// </summary>
    /// <remarks>
    /// 如果在使用分布式缓存，请勿在对并发要求严格的场景中使用此方法，因为 <code>Expire</code> 不是原子操作；
    /// <code>Expire</code> 使用 <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> 存储带有新过期设置的项。
    /// </remarks>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存区域。</param>
    /// <param name="slidingExpiration">
    /// 过期超时时间，该值必须大于零。
    /// </param>
    void Expire(string key, string region, TimeSpan slidingExpiration);

    /// <summary>
    /// 显式移除之前为所有缓存层中的 <paramref name="key"/> 定义的任何过期设置，
    /// 并将过期模式设置为 <see cref="CacheExpirationMode.None"/>。
    /// 此操作会覆盖为此特定项按缓存句柄配置的任何过期设置。
    /// </summary>
    /// <remarks>
    /// 如果在使用分布式缓存，请勿在对并发要求严格的场景中使用此方法，因为 <code>Expire</code> 不是原子操作；
    /// <code>Expire</code> 使用 <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> 存储带有新过期设置的项。
    /// </remarks>
    /// <param name="key">缓存键。</param>
    void RemoveExpiration(string key);

    /// <summary>
    /// 显式移除之前为所有缓存层中 <paramref name="region"/> 内的 <paramref name="key"/> 定义的任何过期设置，
    /// 并将过期模式设置为 <see cref="CacheExpirationMode.None"/>。
    /// 此操作会覆盖为此特定项按缓存句柄配置的任何过期设置。
    /// </summary>
    /// <remarks>
    /// 如果在使用分布式缓存，请勿在对并发要求严格的场景中使用此方法，因为 <code>Expire</code> 不是原子操作；
    /// <code>Expire</code> 使用 <see cref="ICache{TCacheValue}.Put(CacheItem{TCacheValue})"/> 存储带有新过期设置的项。
    /// </remarks>
    /// <param name="key">缓存键。</param>
    /// <param name="region">缓存区域。</param>
    void RemoveExpiration(string key, string region);
}