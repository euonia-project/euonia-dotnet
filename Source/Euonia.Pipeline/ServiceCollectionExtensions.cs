using Nerosoft.Euonia.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于向服务集合注册管道服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 向服务集合注册管道相关服务。
    /// 注册泛型 <see cref="IPipeline{TRequest, TResponse}"/> 服务。
    /// </summary>
    /// <param name="services">要注册服务的 <see cref="IServiceCollection"/> 实例。</param>
    /// <returns>返回当前的 <see cref="IServiceCollection"/> 实例，以便进行链式调用。</returns>
    public static IServiceCollection AddPipeline(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipeline<,>), typeof(DefaultPipelineProvider<,>));

        return services;
    }
}