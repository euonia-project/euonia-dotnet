// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace System;

/// <summary>
/// 指示属性或参数将从服务容器解析。
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class InjectAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="InjectAttribute"/> 的新实例。
    /// </summary>
    /// <param name="serviceKey">一个对象，指定要获取的服务对象的键。</param>
    public InjectAttribute(object? serviceKey = null)
    {
        ServiceKey = serviceKey;
    }

    /// <summary>
    /// 一个对象，指定要获取的服务对象的键。
    /// </summary>
    public object? ServiceKey { get; }
}