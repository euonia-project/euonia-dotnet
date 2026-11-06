namespace System;

/// <summary>
/// 定义按类型访问对象的只读访问器契约。
/// </summary>
/// <typeparam name="TValue">所访问值的类型。</typeparam>
public interface IObjectAccessor<out TValue>
{
    /// <summary>
    /// 获取访问器当前持有的值。
    /// </summary>
    TValue Value { get; }
}