namespace System;

/// <inheritdoc />
public class ObjectAccessor<TValue> : IObjectAccessor<TValue>
{
    /// <inheritdoc />
    public TValue Value { get; set; } = default!;

    /// <summary>
    /// 初始化 <see cref="ObjectAccessor{TValue}"/> 的新实例。
    /// </summary>
    public ObjectAccessor()
    {
    }

    /// <summary>
    /// 使用指定值初始化 <see cref="ObjectAccessor{TValue}"/> 的新实例。
    /// </summary>
    /// <param name="obj">访问器要保留的初始值。</param>
    public ObjectAccessor(TValue obj)
    {
        Value = obj;
    }
}