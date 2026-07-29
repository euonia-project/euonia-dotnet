namespace System;

/// <summary>
/// 表示具有名称和值的枚举对象。
/// </summary>
/// <typeparam name="TValue">枚举值的类型。</typeparam>
public class EnumObject<TValue>
{
    /// <summary>
    /// 获取或设置枚举对象的名称。
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// 获取或设置枚举对象的值。
    /// </summary>
    public TValue Value { get; set; } = default!;
}