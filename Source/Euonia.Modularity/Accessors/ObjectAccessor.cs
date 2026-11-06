namespace System;

/// <inheritdoc />
public class ObjectAccessor<TValue> : IObjectAccessor<TValue>
{
    /// <inheritdoc />
    public TValue Value { get; set; } = default!;

    /// <summary>
    /// Initialize a new instance of <see cref="ObjectAccessor{TValue}"/>.
    /// </summary>
    public ObjectAccessor()
    {
    }

    /// <summary>
    /// Initialize a new instance of <see cref="ObjectAccessor{TValue}"/> with given value.
    /// </summary>
    /// <param name="obj"></param>
    public ObjectAccessor(TValue obj)
    {
        Value = obj;
    }
}