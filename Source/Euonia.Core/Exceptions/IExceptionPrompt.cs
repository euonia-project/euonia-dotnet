namespace System;

/// <summary>
/// 提供异常提示信息的接口。
/// </summary>
public interface IExceptionPrompt
{
    /// <summary>
    /// 获取指定异常的提示信息。
    /// </summary>
    /// <param name="exception">要获取提示的异常。</param>
    /// <returns>异常的提示信息字符串。</returns>
    string GetPrompt(Exception exception);
}