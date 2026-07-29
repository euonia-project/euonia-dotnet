namespace System;

/// <summary>
/// 表示包含指定泛型类型值的引用盒。
/// 此类对其内容强制执行以下规则：
/// - 包含的值始终非 null
/// - 创建后，包含的值永远不会改变
/// - 包含的值永远不会被装箱
/// </summary>
public sealed class RefBox<T> where T : struct
{
    private readonly T _value;

    internal RefBox(T value)
    {
        _value = value;
    }

    /// <summary>
    /// 获取值。
    /// </summary>
    public ref readonly T Value => ref _value;
}

/// <summary>
/// 表示包含指定泛型类型值的引用盒。
/// 此类对其内容强制执行以下规则：
/// - 包含的值始终非 null
/// - 创建后，包含的值永远不会改变
/// - 包含的值永远不会被装箱
/// </summary>
public sealed class RefBox
{
    /// <summary>
    /// 创建 <see cref="RefBox{T}"/> 的新实例。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <param name="value">要包装的值。</param>
    /// <returns>新的 <see cref="RefBox{T}"/> 实例。</returns>
    public static RefBox<T> Create<T>(T value) where T : struct => new(value);

    /// <summary>
    /// 以线程安全的方式检查 <paramref name="boxRef"/> 是否为非 null，如果是则将其设为 null 并输出值为 <paramref name="value"/>。
    /// </summary>
    /// <typeparam name="T">值的类型。</typeparam>
    /// <param name="boxRef">要消费的引用盒引用。</param>
    /// <param name="value">输出的值。</param>
    /// <returns>如果成功消费则为 true；否则为 false。</returns>
    public static bool TryConsume<T>(ref RefBox<T> boxRef, out T value)
        where T : struct
    {
        var box = Interlocked.Exchange(ref boxRef, null);
        if (box != null)
        {
            value = box.Value;
            return true;
        }

        value = default;
        return false;
    }
}
