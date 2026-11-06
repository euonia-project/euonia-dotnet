using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Extensibility;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 基于依赖注入的 Quartz 作业工厂。
/// </summary>
public class DefaultJobFactory : IJobFactory
{
    private readonly IServiceProvider _provider;

    /// <summary>
    /// 初始化 <see cref="DefaultJobFactory"/> 的新实例。
    /// </summary>
    /// <param name="provider">用于创建或解析作业实例的服务提供程序。</param>
    public DefaultJobFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public ValueTask<JobScope> CreateJob(TriggerFiredBundle bundle, IScheduler scheduler, CancellationToken cancellationToken)
    {
        var type = bundle.JobDetail.JobType.Type;
        if (type == null)
        {
            throw new NullReferenceException();
        }

        var job = (IJob)ActivatorUtilities.GetServiceOrCreateInstance(_provider, type);
        return ValueTask.FromResult(new JobScope(job, state: null));
    }

    /// <inheritdoc />
    public ValueTask ReturnJob(JobScope scope, CancellationToken cancellationToken)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        if (scope.Job is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
