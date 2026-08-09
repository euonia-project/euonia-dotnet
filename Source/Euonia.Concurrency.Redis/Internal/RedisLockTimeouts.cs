using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

internal readonly struct RedisLockTimeouts
{
    public RedisLockTimeouts(TimeoutValue expiry, TimeoutValue minValidityTime)
    {
        Expiry = expiry;
        MinValidityTime = minValidityTime;
    }

    public TimeoutValue Expiry { get; }
    public TimeoutValue MinValidityTime { get; }
    public TimeoutValue AcquireTimeout => Expiry.TimeSpan - MinValidityTime.TimeSpan;
}