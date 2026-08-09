using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Concurrency.Azure;

/// <summary>
/// <see cref="AzureLockProvider"/> 的配置选项。
/// </summary>
/// <param name="Duration">租约的持续时间。</param>
/// <param name="RenewalCadence">续约节奏，即两次租约续约之间的间隔。</param>
/// <param name="MinBusyWaitSleepTime">忙等待轮询的最小休眠时间。</param>
/// <param name="MaxBusyWaitSleepTime">忙等待轮询的最大休眠时间。</param>
public sealed record AzureSynchronizationOptions(TimeoutValue Duration, TimeoutValue RenewalCadence, TimeoutValue MinBusyWaitSleepTime, TimeoutValue MaxBusyWaitSleepTime);