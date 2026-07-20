namespace System;

/// <summary>
/// 表示对对象的弱引用。
/// </summary>
/// <typeparam name="T">目标对象的类型。</typeparam>
public class Weak<T>
{
    /// <summary>
    /// 弱引用目标。
    /// </summary>
    private readonly WeakReference _target;

    /// <summary>
    /// 初始化 <see cref="Weak{T}" /> 类的新实例。
    /// </summary>
    /// <param name="target">目标对象。</param>
    public Weak(T target)
    {
        _target = new WeakReference(target);
    }

    /// <summary>
    /// 初始化 <see cref="Weak{T}" /> 类的新实例。
    /// </summary>
    /// <param name="target">目标对象。</param>
    /// <param name="trackResurrection">是否跟踪对象复活。</param>
    public Weak(T target, bool trackResurrection)
    {
        _target = new WeakReference(target, trackResurrection);
    }

    /// <summary>
    /// 获取或设置弱引用目标。
    /// </summary>
    public T Target
    {
        get => (T)_target.Target;
        set => _target.Target = value;
    }
}