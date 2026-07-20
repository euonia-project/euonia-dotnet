using System.Collections.Concurrent;

namespace System;

/// <summary>
/// 任意类的单例设计模式实现。
/// </summary>
/// <typeparam name="T">单例类型。</typeparam>
public class Singleton<T> where T : class
{
    private static readonly ConcurrentDictionary<Type, T> _container = new();

    /// <summary>
    /// 获取或设置单例实例。
    /// </summary>
    public static T Instance
    {
        get => _container[typeof(T)];
        set => _container.AddOrUpdate(typeof(T), _ => value, (_, _) => value);
    }

    /// <summary>
    /// 获取指定类型 <typeparamref name="T"/> 的单例实例。
    /// </summary>
    /// <param name="factory">如果实例不存在，则用于创建新实例的工厂函数。</param>
    /// <returns>类型 <typeparamref name="T"/> 的单例实例。</returns>
    public static T Get(Func<T> factory)
    {
        return _container.GetOrAdd(typeof(T), factory);
    }
}