// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace System;

/// <summary>
/// Indicate that the property or parameter would resolve from service container.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class InjectAttribute : Attribute
{
    /// <summary>
    /// Initialize a new instance of <see cref="InjectAttribute"/>.
    /// </summary>
    /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
    public InjectAttribute(object? serviceKey = null)
    {
        ServiceKey = serviceKey;
    }

    /// <summary>
    /// An object that specifies the key of service object to get.
    /// </summary>
    public object? ServiceKey { get; }
}