using System.Diagnostics;

namespace System;

/// <summary>
/// 帮助强制执行不变条件的方法。
/// </summary>
public static class Invariant
{
    /// <summary>
    /// 检查条件是否为 true，如果不为 true，则抛出带有指定消息的 <see cref="InvalidOperationException"/>。
    /// </summary>
    /// <param name="condition">要检查的条件。</param>
    /// <param name="message">如果条件为 false，则包含在 <see cref="InvalidOperationException"/> 中的消息。如果为 null，则使用默认消息。</param>
    [Conditional("DEBUG")]
    public static void Require(bool condition, string message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "invariant violated");
        }
    }
}