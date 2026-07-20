using System.Runtime.ExceptionServices;

public static partial class Extensions
{
    /// <summary>
    /// 获取异常链的完整消息，包括内部异常。
    /// </summary>
    /// <param name="exception">异常。</param>
    /// <param name="maxDepths">最大遍历深度。</param>
    /// <returns>完整的异常消息字符串。</returns>
    public static string GetFullMessage(this Exception exception, int maxDepths = 3)
    {
        if (exception == null)
        {
            return null;
        }

        var message = new StringBuilder();
        while (exception != null && maxDepths > 0)
        {
            message.AppendLine(exception.Message);
            message.AppendLine("====================");
            exception = exception.InnerException;
            maxDepths--;
        }

        return message.ToString();
    }

    /// <summary>
    /// 获取异常链中最底层异常的消息。
    /// </summary>
    /// <param name="exception">捕获的异常。</param>
    /// <returns>根异常的消息。</returns>
    public static string GetRootMessage(this Exception exception)
    {
        while (true)
        {
            if (exception == null)
            {
                return string.Empty;
            }

            if (exception.InnerException == null)
            {
                return exception.Message;
            }

            exception = exception.InnerException;
        }
    }

    /// <summary>
    /// 准备重新抛出异常，保留堆栈跟踪。返回的异常应立即抛出。
    /// </summary>
    /// <param name="exception">异常，不能为 <c>null</c>。</param>
    /// <returns>传入此方法的 <see cref="Exception"/>。</returns>
    public static Exception PrepareForRethrow(this Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();

        // 代码永远不会执行到这里。返回值只是为了绕过设计不佳的 API。
		//  https://connect.microsoft.com/VisualStudio/feedback/details/689516/exceptiondispatchinfo-api-modifications (http://www.webcitation.org/6XQ7RoJmO)
        return exception;
    }
}