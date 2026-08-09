using System.Diagnostics.CodeAnalysis;

namespace System;

/// <summary>
/// 一个可能包含或不包含非 null 值的容器对象。如果值存在，<see cref="HasValue"/> 返回 <see langword="true"/>，
/// 否则返回 <see langword="false"/>。提供依赖于值存在与否的其他方法，例如 <see cref="Or(T)"/> 和 <see cref="IfPresent(Action{T})"/>。
/// </summary>
/// <typeparam name="T">包含值的类型。</typeparam>
public sealed class Optional<T>
{
	private readonly T _value;
    private readonly bool _hasValue;

    /// <summary>
    /// 初始化一个空的 <see cref="Optional{T}"/> 实例。
    /// </summary>
    private Optional()
    {
        _value = default!;
        _hasValue = false;
    }

    /// <summary>
    /// 使用指定值初始化 <see cref="Optional{T}"/> 实例。
    /// </summary>
    /// <param name="value">要包装的非 null 值。</param>
    private Optional(T value)
    {
        _value = value;
        _hasValue = true;
    }

    /// <summary>
    /// 获取一个空的 <see cref="Optional{T}"/> 实例。
    /// </summary>
    public static Optional<T> Empty { get; } = new();

    /// <summary>
    /// 获取值（如果存在）。访问空 Optional 的值将抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    /// <exception cref="InvalidOperationException">当 <see cref="HasValue"/> 为 <see langword="false"/> 时抛出。</exception>
    public T Value
    {
        get
        {
            if (!_hasValue)
            {
                ThrowNoValueException();
            }

            return _value;
        }
    }

    /// <summary>
    /// 如果值存在，返回 <see langword="true"/>；否则返回 <see langword="false"/>。
    /// </summary>
    public bool HasValue => _hasValue;

    /// <summary>
    /// 如果值不存在，返回 <see langword="true"/>；否则返回 <see langword="false"/>。
    /// </summary>
    public bool IsEmpty => !_hasValue;

    /// <summary>
    /// 返回包含指定非 null 值的 <see cref="Optional{T}"/>。
    /// </summary>
    /// <param name="value">要包装的值，不能为 <see langword="null"/>。</param>
    /// <returns>包含指定值的 <see cref="Optional{T}"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> 为 <see langword="null"/> 时抛出。</exception>
    public static Optional<T> Of(T value)
    {
        ArgumentAssert.ThrowIfNull(value);
        return new Optional<T>(value);
    }

    /// <summary>
    /// 返回包含指定值的 <see cref="Optional{T}"/>，如果值为 <see langword="null"/> 则返回空 <see cref="Optional{T}"/>。
    /// </summary>
    /// <param name="value">要包装的值，可以为 <see langword="null"/>。</param>
    /// <returns>如果值非 null 则返回包含值的 <see cref="Optional{T}"/>，否则返回 <see cref="Empty"/>。</returns>
    public static Optional<T> OfNullable(T value)
    {
        return value is null ? Empty : new Optional<T>(value);
    }

    /// <summary>
    /// 如果值存在且满足指定谓词，则返回包含该值的 <see cref="Optional{T}"/>；否则返回空 <see cref="Optional{T}"/>。
    /// </summary>
    /// <param name="predicate">用于测试值的谓词。</param>
    /// <returns>如果值存在且满足谓词则返回当前实例，否则返回 <see cref="Empty"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> 为 <see langword="null"/> 时抛出。</exception>
    public Optional<T> Where(Func<T, bool> predicate)
    {
        ArgumentAssert.ThrowIfNull(predicate);
        if (!_hasValue)
        {
            return this;
        }

        return predicate(_value) ? this : Empty;
    }

    /// <summary>
    /// 如果值存在，则将提供的映射函数应用于该值，并返回包含映射结果的 <see cref="Optional{TResult}"/>；
    /// 否则返回空 <see cref="Optional{TResult}"/>。
    /// </summary>
    /// <typeparam name="TResult">映射结果的类型。</typeparam>
    /// <param name="selector">应用于值的映射函数。</param>
    /// <returns>包含映射结果的 <see cref="Optional{TResult}"/>，如果值不存在则返回空。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> 为 <see langword="null"/> 时抛出。</exception>
    public Optional<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        ArgumentAssert.ThrowIfNull(selector);
        if (!_hasValue)
        {
            return Optional<TResult>.Empty;
        }

        return Optional<TResult>.OfNullable(selector(_value));
    }

    /// <summary>
    /// 如果值存在，则将提供的映射函数应用于该值并直接返回其 <see cref="Optional{TResult}"/> 结果；
    /// 否则返回空 <see cref="Optional{TResult}"/>。
    /// </summary>
    /// <typeparam name="TResult">映射结果的类型。</typeparam>
    /// <param name="selector">应用于值并返回 <see cref="Optional{TResult}"/> 的映射函数。</param>
    /// <returns>映射函数的结果，如果值不存在则返回空。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> 为 <see langword="null"/> 时抛出。</exception>
    public Optional<TResult> SelectMany<TResult>(Func<T, Optional<TResult>> selector)
    {
        ArgumentAssert.ThrowIfNull(selector);
        if (!_hasValue)
        {
            return Optional<TResult>.Empty;
        }

        return selector(_value);
    }

    /// <summary>
    /// 如果值存在，则返回该值；否则返回 <paramref name="other"/>。
    /// </summary>
    /// <param name="other">值不存在时返回的默认值。</param>
    /// <returns>如果存在则返回实际值，否则返回 <paramref name="other"/>。</returns>
    public T Or(T other)
    {
        return _hasValue ? _value : other;
    }

    /// <summary>
    /// 如果值存在，则返回该值；否则返回由 <paramref name="supplier"/> 提供的值。
    /// </summary>
    /// <param name="supplier">值不存在时提供默认值的函数。</param>
    /// <returns>如果存在则返回实际值，否则返回由 <paramref name="supplier"/> 提供的结果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="supplier"/> 为 <see langword="null"/> 时抛出。</exception>
    public T Or(Func<T> supplier)
    {
        ArgumentAssert.ThrowIfNull(supplier);
        return _hasValue ? _value : supplier();
    }

    /// <summary>
    /// 如果值存在，则返回该值；否则抛出 <see cref="InvalidOperationException"/>。
    /// </summary>
    /// <returns>实际值。</returns>
    /// <exception cref="InvalidOperationException">值不存在时抛出。</exception>
    public T GetOrThrow()
    {
        if (!_hasValue)
        {
            ThrowNoValueException();
        }

        return _value;
    }

    /// <summary>
    /// 如果值存在，则返回该值；否则抛出由 <paramref name="exceptionSupplier"/> 提供的异常。
    /// </summary>
    /// <typeparam name="TException">要抛出的异常类型。</typeparam>
    /// <param name="exceptionSupplier">提供异常的工厂函数。</param>
    /// <returns>实际值。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exceptionSupplier"/> 为 <see langword="null"/> 时抛出。</exception>
    public T GetOrThrow<TException>(Func<TException> exceptionSupplier) where TException : Exception
    {
        ArgumentAssert.ThrowIfNull(exceptionSupplier);
        if (!_hasValue)
        {
            throw exceptionSupplier();
        }

        return _value;
    }

    /// <summary>
    /// 如果值存在，则使用该值执行指定操作；否则不执行任何操作。
    /// </summary>
    /// <param name="action">值存在时要执行的操作。</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> 为 <see langword="null"/> 时抛出。</exception>
    public void IfPresent(Action<T> action)
    {
        ArgumentAssert.ThrowIfNull(action);
        if (_hasValue)
        {
            action(_value);
        }
    }

    /// <summary>
    /// 如果值存在，则使用该值执行指定操作；否则执行空的回退操作。
    /// </summary>
    /// <param name="action">值存在时要执行的操作。</param>
    /// <param name="emptyAction">值不存在时要执行的操作。</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentNullException"><paramref name="emptyAction"/> 为 <see langword="null"/> 时抛出。</exception>
    public void IfPresent(Action<T> action, Action emptyAction)
    {
        ArgumentAssert.ThrowIfNull(action);
        ArgumentAssert.ThrowIfNull(emptyAction);
        if (_hasValue)
        {
            action(_value);
        }
        else
        {
            emptyAction();
        }
    }

    /// <summary>
    /// 返回当前对象的值（如果存在），否则返回 <typeparamref name="T"/> 的默认值。
    /// </summary>
    /// <returns>值或默认值。</returns>
    [return: MaybeNull]
    public T GetValueOrDefault()
    {
        return _hasValue ? _value : default;
    }

    /// <summary>
    /// 返回当前对象的值（如果存在），否则返回 <paramref name="defaultValue"/>。
    /// </summary>
    /// <param name="defaultValue">值不存在时返回的默认值。</param>
    /// <returns>值或默认值。</returns>
    public T GetValueOrDefault(T defaultValue)
    {
        return _hasValue ? _value : defaultValue;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (obj is not Optional<T> other)
        {
            return false;
        }

        if (!_hasValue && !other._hasValue)
        {
            return true;
        }

        if (_hasValue && other._hasValue)
        {
            return EqualityComparer<T>.Default.Equals(_value, other._value);
        }

        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _hasValue ? HashCode.Combine(_value) : 0;
    }

    /// <summary>
    /// 判断两个 <see cref="Optional{T}"/> 实例是否相等。
    /// </summary>
    public static bool operator ==(Optional<T> left, Optional<T> right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// 判断两个 <see cref="Optional{T}"/> 实例是否不相等。
    /// </summary>
    public static bool operator !=(Optional<T> left, Optional<T> right)
    {
        return !(left == right);
    }

    /// <summary>
    /// 返回当前实例的字符串表示形式。
    /// </summary>
    /// <returns>如果存在则返回 <c>Optional[value]</c>，否则返回 <c>Optional.empty</c>。</returns>
    public override string ToString()
    {
        return _hasValue ? $"Optional[{_value}]" : "Optional.empty";
    }

    [DoesNotReturn]
    private static void ThrowNoValueException()
    {
        throw new InvalidOperationException("Optional 不包含任何值。");
    }
}