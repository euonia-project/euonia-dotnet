namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 用于创建正确实例的辅助类。
/// </summary>
public static class CacheItemUpdateResult
{
	/// <summary>
	/// 创建 <see cref="CacheItemUpdateResult{TCacheValue}"/> 类的新实例，其属性对应更新操作中缓存项不存在的情况。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <returns>项结果。</returns>
	public static CacheItemUpdateResult<TValue> ForItemDidNotExist<TValue>() =>
		new(null, CacheItemUpdateResultState.ItemDidNotExist, false, 1);

	/// <summary>
	/// 创建 <see cref="CacheItemUpdateResult{TCacheValue}"/> 的新实例，指示缓存值工厂返回了 <c>null</c> 而非有效值。
	/// </summary>
	/// <typeparam name="TCacheValue">缓存值的类型。</typeparam>
	/// <returns>项结果。</returns>
	public static CacheItemUpdateResult<TCacheValue> ForFactoryReturnedNull<TCacheValue>() =>
		new(null, CacheItemUpdateResultState.FactoryReturnedNull, false, 1);

	/// <summary>
	/// 创建 <see cref="CacheItemUpdateResult{TCacheValue}"/> 类的新实例，其属性对应成功的更新操作。
	/// </summary>
	/// <typeparam name="TCacheValue">缓存值的类型。</typeparam>
	/// <param name="value">值。</param>
	/// <param name="conflictOccurred">如果发生冲突，则设为 <c>true</c>。</param>
	/// <param name="triesNeeded">所需尝试次数。</param>
	/// <returns>项结果。</returns>
	public static CacheItemUpdateResult<TCacheValue> ForSuccess<TCacheValue>(CacheItem<TCacheValue> value, bool conflictOccurred = false, int triesNeeded = 1) =>
		new(value, CacheItemUpdateResultState.Success, conflictOccurred, triesNeeded);

	/// <summary>
	/// 创建 <see cref="CacheItemUpdateResult{TCacheValue}"/> 类的新实例，其属性对应因超过尝试次数上限而失败的更新操作。
	/// </summary>
	/// <typeparam name="TCacheValue">缓存值的类型。</typeparam>
	/// <param name="triesNeeded">所需尝试次数。</param>
	/// <returns>项结果。</returns>
	public static CacheItemUpdateResult<TCacheValue> ForTooManyRetries<TCacheValue>(int triesNeeded) =>
		new(null, CacheItemUpdateResultState.TooManyRetries, true, triesNeeded);
}

/// <summary>
/// 由缓存句柄实现使用，用于让缓存管理器了解更新操作期间发生的情况。
/// </summary>
/// <typeparam name="TValue">缓存值的类型。</typeparam>
public class CacheItemUpdateResult<TValue>
{
	internal CacheItemUpdateResult(CacheItem<TValue> value, CacheItemUpdateResultState state, bool conflictOccurred, int triesNeeded)
	{
		if (triesNeeded == 0)
		{
			throw new ArgumentOutOfRangeException(nameof(triesNeeded), string.Format(Resources.IDS_VALUE_MUST_BE_GREATER_THAN, 0));
		}

		VersionConflictOccurred = conflictOccurred;
		UpdateState = state;
		NumberOfTriesNeeded = triesNeeded;
		Value = value;
	}

	/// <summary>
	/// 获取缓存更新该项所需的尝试次数。
	/// </summary>
	/// <value>所需重试次数。</value>
	public int NumberOfTriesNeeded { get; }

	/// <summary>
	/// 获取一个值，指示更新操作是否成功。
	/// </summary>
	/// <value>当前的 <see cref="CacheItemUpdateResultState"/>。</value>
	public CacheItemUpdateResultState UpdateState { get; }

	/// <summary>
	/// 获取更新后的值。
	/// </summary>
	/// <value>更新后的值。</value>
	public CacheItem<TValue> Value { get; }

	/// <summary>
	/// 获取一个值，指示更新操作期间是否发生了版本冲突。
	/// </summary>
	/// <value>如果发生了版本冲突，则为 <c>true</c>；否则为 <c>false</c>。</value>
	public bool VersionConflictOccurred { get; }
}