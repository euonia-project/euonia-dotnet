namespace System;

/// <summary>
/// 负责存储和返回异常提示信息。
/// </summary>
public static class ExceptionPrompt
{
    private static readonly List<IExceptionPrompt> _prompts = new();

    /// <summary>
    /// 向提示列表中添加一个提示提供程序。
    /// </summary>
    /// <param name="prompt">要添加的异常提示提供程序。</param>
    /// <exception cref="ArgumentNullException"><paramref name="prompt"/> 为 null。</exception>
    public static void AddPrompt(IExceptionPrompt prompt)
    {
        if (prompt == null)
        {
            throw new ArgumentNullException(nameof(prompt));
        }

        if (_prompts.Contains(prompt))
        {
            return;
        }

        _prompts.Add(prompt);
    }

    /// <summary>
    /// 获取指定异常的提示信息。
    /// </summary>
    /// <param name="exception">要获取提示的异常。</param>
    /// <returns>异常的提示信息。</returns>
    public static string GetPrompt(Exception exception)
    {
        exception = exception.GetBaseException();
        var prompt = GetExceptionPrompt(exception);
        if (string.IsNullOrWhiteSpace(prompt) == false)
        {
            return prompt;
        }

        if (exception is ApplicationException applicationException)
        {
            return applicationException.Message;
        }

        return "Application error";
    }

    private static string GetExceptionPrompt(Exception exception)
    {
        foreach (var prompt in _prompts)
        {
            var result = prompt.GetPrompt(exception);
            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }

        return string.Empty;
    }
}