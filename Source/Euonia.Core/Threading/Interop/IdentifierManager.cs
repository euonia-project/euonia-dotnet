namespace Nerosoft.Euonia.Threading.Interop;

/// <summary>
/// 按需为实例分配 Id。0 是无效/未分配的 Id。在长时间运行的系统中，Id 可能不唯一。这类似于 <see cref="System.Threading.Tasks.Task"/> 和 <see cref="System.Threading.Tasks.TaskScheduler"/> 使用的 Id 系统。
/// </summary>
/// <typeparam name="T">为其生成 Id 的类型。</typeparam>
// ReSharper disable UnusedTypeParameter
internal static class IdentifierManager<T>
// ReSharper restore UnusedTypeParameter
{
    /// <summary>
    /// 为此类型生成的最后一个 Id。如果尚未生成任何 Id，则为 0。
    /// </summary>
// ReSharper disable StaticFieldInGenericType
    private static int _lastId;
// ReSharper restore StaticFieldInGenericType

    /// <summary>
    /// 返回 Id，如果尚未分配则分配一个新的 Id。
    /// </summary>
    /// <param name="id">对包含 Id 的字段的引用。</param>
    public static int GetId(ref int id)
    {
        // 如果 Id 已经被分配，直接使用它。
        // If the Id has already been assigned, just use it.
        if (id != 0)
        {
            return id;
        }

        // 在不修改 "id" 的情况下确定新的 Id，因为其他线程也可能同时正在确定新的 Id。
        // Determine the new Id without modifying "id", since other threads may also be determining the new Id at the same time.
        int newId;

        // 递增操作在 while 循环中以确保获得非零 Id：
        // 如果我们正在递增 -1，则需要跳过 0。
        // 如果有大量的 Id 分配正在进行中，无论我们获得多少次 0，都需要跳过它。
        // The Increment is in a while loop to ensure we get a non-zero Id:
        //  If we are incrementing -1, then we want to skip over 0.
        //  If there are tons of Id allocations going on, we want to skip over 0 no matter how many times we get it.
        do
        {
            newId = Interlocked.Increment(ref _lastId);
        }
        while (newId == 0);

        // 更新 Id，除非其他线程已经更新了它。
        // Update the Id unless another thread already updated it.
        Interlocked.CompareExchange(ref id, newId, 0);

        // 返回当前的 Id，无论它是我们的新 Id 还是来自其他线程的新 Id。
        // Return the current Id, regardless of whether it's our new Id or a new Id from another thread.
        return id;
    }
}
