using Nerosoft.Euonia.Pipeline;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于向服务集合注册管道服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 向服务集合注册管道相关服务。
    /// 注册 <see cref="IPipeline"/>、泛型 <see cref="IPipeline{TRequest, TResponse}"/> 及构建后的 <see cref="PipelineDelegate"/> 服务。
    /// </summary>
    /// <param name="services">要注册服务的 <see cref="IServiceCollection"/> 实例。</param>
    /// <returns>返回当前的 <see cref="IServiceCollection"/> 实例，以便进行链式调用。</returns>
    /// <exception cref="NullReferenceException">当无法从服务提供程序解析 <see cref="IPipeline"/> 服务时抛出。</exception>
    public static IServiceCollection AddPipeline(this IServiceCollection services)
    {
        services.AddTransient<IPipeline, DefaultPipelineProvider>();
        services.AddTransient(typeof(IPipeline<,>), typeof(DefaultPipelineProvider<,>));

        services.AddTransient(provider =>
        {
            var pipeline = provider.GetService<IPipeline>();
            if (pipeline == null)
            {
                throw new NullReferenceException($"Can not resolve service {nameof(IPipeline)}");
            }

            var @delegate = pipeline.Build();
            return @delegate;
        });

        return services;
    }
}