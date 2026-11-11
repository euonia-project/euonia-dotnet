using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Tenancy;
using Quartz;
using Quartz.Extensibility;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 基于依赖注入的 Quartz 作业工厂；按作业数据中的租户标识安装租户作用域。
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
    public async ValueTask<JobScope> CreateJob(TriggerFiredBundle bundle, IScheduler scheduler, CancellationToken cancellationToken)
    {
        var type = bundle.JobDetail.JobType.Type;
        if (type == null)
        {
            throw new NullReferenceException();
        }

        var job = (IJob)ActivatorUtilities.GetServiceOrCreateInstance(_provider, type);

        // 作业数据里的租户标识来自调度方；租户在工厂阶段解析，但作用域必须在作业执行体内安装：
        // Quartz 在独立的异步流中调用 Execute，工厂方法里设置的 AsyncLocal 不会流向执行阶段。
        var tenant = await TenantJobScope.ResolveAsync(bundle.JobDetail, _provider, cancellationToken).ConfigureAwait(false);
        if (tenant != null)
        {
            var scopeFactory = _provider.GetRequiredService<ITenantScopeFactory>();
            job = new TenantScopedJob(job, tenant, scopeFactory);
        }

        return new JobScope(job);
    }

    /// <inheritdoc />
    public ValueTask ReturnJob(JobScope scope, CancellationToken cancellationToken)
    {
        if (scope.State is IAsyncDisposable asyncDisposable)
        {
            return DisposeStateAsync(scope, asyncDisposable);
        }

        (scope.State as IDisposable)?.Dispose();

        // ReSharper disable once SuspiciousTypeConversion.Global
        if (scope.Job is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private static async ValueTask DisposeStateAsync(JobScope scope, IAsyncDisposable disposable)
    {
        await disposable.DisposeAsync().ConfigureAwait(false);

        // ReSharper disable once SuspiciousTypeConversion.Global
        if (scope.Job is IDisposable job)
        {
            job.Dispose();
        }
    }
}

/// <summary>
/// 作业租户解析：把作业数据中的租户标识转换为租户信息。
/// </summary>
internal static class TenantJobScope
{
    /// <summary>
    /// 租户标识的作业数据键。
    /// </summary>
    public const string TenantIdKey = "tenantId";

    /// <summary>
    /// 解析作业数据中的租户；没有租户标识时返回 <c>null</c>。
    /// </summary>
    /// <param name="jobDetail">作业详情。</param>
    /// <param name="provider">服务提供程序。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>租户信息。</returns>
    /// <exception cref="TenantNotFoundException">租户标识存在但查不到租户。</exception>
    public static async ValueTask<ITenantInfo?> ResolveAsync(IJobDetail jobDetail, IServiceProvider provider, CancellationToken cancellationToken)
    {
        if (!jobDetail.JobDataMap.TryGetString(TenantIdKey, out var tenantId) || string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        var store = provider.GetService<ITenantStore>();
        var tenant = store == null
                         ? null
                         : await store.FindByIdAsync(tenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null)
        {
            throw new TenantNotFoundException(tenantId);
        }

        return tenant;
    }
}

/// <summary>
/// 在作业执行体内安装租户作用域；工厂阶段设置的 AsyncLocal 不会流向 Quartz 的执行异步流。
/// </summary>
internal sealed class TenantScopedJob : IJob
{
    private readonly IJob _inner;
    private readonly ITenantInfo _tenant;
    private readonly ITenantScopeFactory _scopeFactory;

    public TenantScopedJob(IJob inner, ITenantInfo tenant, ITenantScopeFactory scopeFactory)
    {
        _inner = inner;
        _tenant = tenant;
        _scopeFactory = scopeFactory;
    }

    /// <inheritdoc />
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.Begin(_tenant);
        await _inner.Execute(context, cancellationToken).ConfigureAwait(false);
    }
}
