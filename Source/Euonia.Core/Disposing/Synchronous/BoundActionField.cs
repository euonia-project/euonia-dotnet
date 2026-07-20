namespace Nerosoft.Euonia.Disposing;

/// <summary>
/// 包含绑定的操作的字段。
/// </summary>
/// <typeparam name="T">操作的上下文类型。</typeparam>
public sealed class BoundActionField<T>
{
    private BoundAction _field;

    /// <summary>
    /// 使用指定的操作和上下文初始化字段。
    /// </summary>
    /// <param name="action">操作委托。</param>
    /// <param name="context">上下文。</param>
    public BoundActionField(Action<T> action, T context)
    {
        _field = new BoundAction(action, context);
    }

    /// <summary>
    /// 获取字段是否为空。
    /// </summary>
    public bool IsEmpty => Interlocked.CompareExchange(ref _field, null, null) == null;

    /// <summary>
    /// 原子地从字段中获取绑定的操作并将字段设置为 <c>null</c>。可能返回 <c>null</c>。
    /// </summary>
    public IBoundAction TryGetAndUnset()
    {
        return Interlocked.Exchange(ref _field, null);
    }

    /// <summary>
    /// 尝试更新存储在字段中的绑定操作的上下文。如果字段为 <c>null</c>，则返回 <c>false</c>。
    /// </summary>
    /// <param name="contextUpdater">用于更新现有上下文的函数。如果多个线程同时尝试更新上下文，此函数可能会被调用多次。</param>
    public bool TryUpdateContext(Func<T, T> contextUpdater)
    {
        while (true)
        {
            var original = Interlocked.CompareExchange(ref _field, _field, _field);
            if (original == null)
                return false;
            var updatedContext = new BoundAction(original, contextUpdater);
            var result = Interlocked.CompareExchange(ref _field, updatedContext, original);
            if (ReferenceEquals(original, result))
                return true;
        }
    }

    /// <summary>
    /// 与其上下文绑定的操作委托。
    /// </summary>
    public interface IBoundAction
    {
        /// <summary>
        /// 执行操作。仅应在通过 <see cref="TryGetAndUnset"/> 从字段中获取绑定操作后调用。
        /// </summary>
        void Invoke();
    }

    private sealed class BoundAction : IBoundAction
    {
        private readonly Action<T> _action;
        private readonly T _context;

        public BoundAction(Action<T> action, T context)
        {
            _action = action;
            _context = context;
        }

        public BoundAction(BoundAction originalBoundAction, Func<T, T> contextUpdater)
        {
            _action = originalBoundAction._action;
            _context = contextUpdater(originalBoundAction._context);
        }

        public void Invoke() => _action?.Invoke(_context);
    }
}