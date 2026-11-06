using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// Redis 操作的同步重试辅助类，对连接中断、超时与服务器异常按固定间隔重试。
/// </summary>
internal static class RetryHelper
{
    // private const string ERROR_MESSAGE = "Maximum number of tries exceeded to perform the action: {0}.";
    // private const string WARNING_MESSAGE = "Exception occurred performing an action. Retrying... {0}/{1}";

    /// <summary>
    /// 按指定重试次数执行带返回值的操作，可重试的异常会等待固定间隔后重试。
    /// </summary>
    /// <typeparam name="T">操作的返回类型。</typeparam>
    /// <param name="action">要执行的操作。</param>
    /// <param name="timeOut">每次重试前的等待毫秒数。</param>
    /// <param name="retries">最大尝试次数，包含首次执行。</param>
    /// <returns>操作成功时的返回值；重试耗尽时返回 <typeparamref name="T"/> 的默认值。</returns>
    /// <remarks>“unknown command” 服务器异常不可重试，会立即抛出。</remarks>
    public static T Retry<T>(Func<T> action, int timeOut, int retries)
    {
        var tries = 0;
        do
        {
            tries++;

            try
            {
                return action();
            }

            // 可能在只读从库执行 lua 脚本时发生，因为主库刚刚宕机。
            // 应通过故障转移恢复。
            catch (RedisServerException ex)
            {
                if (ex.Message.Contains("unknown command"))
                {
                    throw;
                }

                if (tries >= retries)
                {
                    throw;
                }

                Task.Delay(timeOut).Wait();
            }
            catch (RedisConnectionException)
            {
                if (tries >= retries)
                {
                    throw;
                }

                Task.Delay(timeOut).Wait();
            }
            catch (TimeoutException)
            {
                if (tries >= retries)
                {
                    throw;
                }

                Task.Delay(timeOut).Wait();
            }
            catch (AggregateException aggregateException)
            {
                if (tries >= retries)
                {
                    throw;
                }

                aggregateException.Handle(e =>
                {
                    switch (e)
                    {
                        case RedisServerException serverEx when serverEx.Message.Contains("unknown command"):
                            return false;
                        case RedisConnectionException:
                        case TimeoutException:
                        case RedisServerException:
                            Task.Delay(timeOut).Wait();
                            return true;
                        default:
                            return false;
                    }
                });
            }
        }
        while (tries < retries);

        return default!;
    }

    /// <summary>
    /// 按指定重试次数执行无返回值的操作，重试策略与泛型重载一致。
    /// </summary>
    /// <param name="action">要执行的操作。</param>
    /// <param name="timeOut">每次重试前的等待毫秒数。</param>
    /// <param name="retries">最大尝试次数，包含首次执行。</param>
    public static void Retry(Action action, int timeOut, int retries)
    {
        Retry(() =>
            {
                action();
                return true;
            },
            timeOut,
            retries);
    }
}