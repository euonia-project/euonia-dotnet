using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis;

internal static class RetryHelper
{
    // private const string ERROR_MESSAGE = "Maximum number of tries exceeded to perform the action: {0}.";
    // private const string WARNING_MESSAGE = "Exception occurred performing an action. Retrying... {0}/{1}";

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

            // might occur on lua script execution on a readonly slave because the master just died.
            // Should recover via fail over
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