#if NET8_0_OR_GREATER
namespace System;

/// <summary>
/// 提供辅助方法，用于查找由带优先级的工厂函数生成的值。
/// </summary>
/// <remarks>
/// 查找方法接受工厂函数（同步或异步）的优先级队列，并按照队列的出队顺序评估工厂函数，
/// 直到生成的值满足给定的谓词条件。
/// 如果没有任何值满足谓词条件，则返回提供的默认值（对于异步变体，包装在 Task 中）。
/// </remarks>
public static class PriorityValueFinder
{
    /// <summary>
    /// 查找由带优先级的工厂函数生成的第一个满足指定谓词条件的值。
    /// </summary>
    /// <typeparam name="TValue">工厂函数生成的值的类型。</typeparam>
    /// <param name="queue">包含返回 <typeparamref name="TValue"/> 的工厂函数的 <see cref="PriorityQueue{TElement,TPriority}"/>。
    /// 工厂函数按队列的出队顺序评估。</param>
    /// <param name="assert">用于测试每个生成值的函数。第一个返回 <c>true</c> 的值将被返回。</param>
    /// <param name="defaultValue">如果没有任何生成值满足 <paramref name="assert"/>，则返回此值。默认为 <typeparamref name="TValue"/> 的默认值。</param>
    /// <returns>满足 <paramref name="assert"/> 的第一个值，如果没有则返回 <paramref name="defaultValue"/>。</returns>
    public static TValue Find<TValue>(PriorityQueue<Func<TValue>, int> queue, Func<TValue, bool> assert, TValue defaultValue = default)
    {
        while (queue.Count > 0)
        {
            if (!queue.TryDequeue(out var factory, out _))
            {
                continue;
            }

            var value = factory();
            if (assert(value))
            {
                return value;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// 使用提供的 <paramref name="factory"/> 创建优先级队列，然后查找满足 <paramref name="assert"/> 的第一个值。
    /// </summary>
    /// <typeparam name="TValue">工厂函数生成的值的类型。</typeparam>
    /// <param name="factory">用返回 <typeparamref name="TValue"/> 的工厂函数填充 <see cref="PriorityQueue{TElement,TPriority}"/> 的操作。</param>
    /// <param name="assert">用于测试每个生成值的函数。</param>
    /// <param name="defaultValue">如果没有任何生成值满足 <paramref name="assert"/>，则返回此值。</param>
    /// <returns>满足 <paramref name="assert"/> 的第一个值，如果没有则返回 <paramref name="defaultValue"/>。</returns>
    public static TValue Find<TValue>(Action<PriorityQueue<Func<TValue>, int>> factory, Func<TValue, bool> assert, TValue defaultValue = default)
    {
        var queue = new PriorityQueue<Func<TValue>, int>();
        factory(queue);
        var value = Find(queue, assert, defaultValue);
        queue.Clear();
        return value;
    }

    /// <summary>
    /// 查找由带优先级的异步工厂函数生成的第一个满足指定谓词条件的值。
    /// </summary>
    /// <typeparam name="TValue">工厂函数生成的值的类型。</typeparam>
    /// <param name="queue">包含返回 <see cref="Task{TResult}"/>（包装 <typeparamref name="TValue"/>）的工厂函数的 <see cref="PriorityQueue{TElement,TPriority}"/>。</param>
    /// <param name="assert">用于测试每个生成值的函数。第一个返回 <c>true</c> 的值将被返回。</param>
    /// <param name="defaultValue">如果没有任何生成值满足 <paramref name="assert"/>，则返回此值。默认为 <typeparamref name="TValue"/> 的默认值。</param>
    /// <returns>包含满足 <paramref name="assert"/> 的第一个值或 <paramref name="defaultValue"/>（如果没有）的已完成的 <see cref="Task{TResult}"/>。</returns>
    /// <remarks>
    /// 此方法通过 <c>GetAwaiter().GetResult()</c> 同步地执行异步工厂函数。调用者应注意这可能会阻塞调用线程。
    /// </remarks>
    public static async Task<TValue> FindAsync<TValue>(PriorityQueue<Func<Task<TValue>>, int> queue, Func<TValue, bool> assert, TValue defaultValue = default)
    {
        while (queue.Count > 0)
        {
            if (!queue.TryDequeue(out var factory, out _))
            {
                continue;
            }

            var value = await factory();
            if (assert(value))
            {
                return value;
            }
        }

        return await Task.FromResult(defaultValue);
    }

    /// <summary>
    /// 使用提供的 <paramref name="factory"/> 创建异步工厂的优先级队列，然后查找满足 <paramref name="assert"/> 的第一个值。
    /// </summary>
    /// <typeparam name="TValue">工厂函数生成的值的类型。</typeparam>
    /// <param name="factory">用返回 <see cref="Task{TResult}"/>（包装 <typeparamref name="TValue"/>）的工厂函数填充 <see cref="PriorityQueue{TElement,TPriority}"/> 的操作。</param>
    /// <param name="assert">用于测试每个生成值的函数。</param>
    /// <param name="defaultValue">如果没有任何生成值满足 <paramref name="assert"/>，则返回此值。</param>
    /// <returns>包含满足 <paramref name="assert"/> 的第一个值或 <paramref name="defaultValue"/>（如果没有）的已完成的 <see cref="Task{TResult}"/>。</returns>
    public static async Task<TValue> FindAsync<TValue>(Action<PriorityQueue<Func<Task<TValue>>, int>> factory, Func<TValue, bool> assert, TValue defaultValue = default)
    {
        var queue = new PriorityQueue<Func<Task<TValue>>, int>();
        factory(queue);
        var value = await FindAsync(queue, assert, defaultValue);
        queue.Clear();
        return value;
    }
}
#endif
