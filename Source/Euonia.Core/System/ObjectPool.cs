namespace System;

/// <summary>
/// <see cref="ObjectPool{T}"/> 使用的约定，用于定义如何创建实例并将其归还到池中。
/// </summary>
/// <typeparam name="T">池中对象的类型。</typeparam>
public interface IObjectPoolPolicy<T>
{
    /// <summary>
    /// 创建 <typeparamref name="T"/> 的新实例。
    /// </summary>
    /// <returns>新创建的实例。</returns>
    T CreateNew();

    /// <summary>
    /// 检查实例是否可以归还，并可能将实例重置为可重用状态。
    /// </summary>
    /// <param name="value">要归还的实例。</param>
    /// <returns>如果可以归还实例则为 <c>True</c>，否则为 <c>False</c>。</returns>
    bool Return(T value);
}

/// <summary>
/// 基于策略的简单对象池。
/// </summary>
/// <typeparam name="T">要池化的对象类型。</typeparam>
public class ObjectPool<T>
    where T : class
{
    private readonly T[] _items;
    private readonly IObjectPoolPolicy<T> _policy;

    /// <summary>
    /// 初始化 <see cref="ObjectPool{T}"/> 类的新实例。
    /// </summary>
    /// <param name="policy">对象池策略。</param>
    /// <param name="maxItems">保留的项目数，默认为处理器数量 * 2。</param>
    public ObjectPool(IObjectPoolPolicy<T> policy, int? maxItems = null)
    {
        if (maxItems == null || maxItems <= 0)
        {
            maxItems = Environment.ProcessorCount * 2;
        }

        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _items = new T[maxItems.Value];
    }

    /// <summary>
    /// 返回 <typeparamref name="T"/> 的池化实例或新实例。
    /// </summary>
    /// <returns>池化实例或新创建的实例。</returns>
    public T Lease()
    {
        for (var i = 0; i < _items.Length; i++)
        {
            var item = _items[i];
            if (item != null && Interlocked.CompareExchange(ref _items[i], null, item) == item)
            {
                return item;
            }
        }

        return _policy.CreateNew();
    }

    /// <summary>
    /// 将实例归还到池中（如果可能）。
    /// </summary>
    /// <param name="value">要归还到池中的实例。</param>
    public void Return(T value)
    {
        if (!_policy.Return(value))
        {
            return;
        }

        for (var i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                _items[i] = value;
                return;
            }
        }
    }
}
