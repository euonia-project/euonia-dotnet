using System.Reflection;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nerosoft.Euonia.Grpc;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 用于向 <see cref="IApplicationBuilder"/> 添加 gRPC 服务器功能的扩展方法。
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 将 gRPC 服务映射到应用程序终结点，并为根路径添加调用提示。
    /// </summary>
    /// <param name="builder">要配置的应用程序构建器。</param>
    /// <param name="configure">用于追加配置终结点路由的可选委托。</param>
    /// <returns>原始应用程序构建器，以支持链式调用。</returns>
    public static IApplicationBuilder UseGrpcEndpoints(this IApplicationBuilder builder, Action<IEndpointRouteBuilder>? configure = null)
    {
        builder.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcServices();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Communication with gRPC endpoints must be called through a gRPC client");
            });
            configure?.Invoke(endpoints);
        });
        return builder;
    }

    /// <summary>
    /// 扫描入口程序集中的 gRPC 服务实现并映射对应终结点。
    /// </summary>
    /// <param name="builder">要映射终结点的路由构建器。</param>
    /// <param name="useHealthCheck">为 <see langword="true"/> 时同时启用 gRPC 健康检查。</param>
    public static void MapGrpcServices(this IEndpointRouteBuilder builder, bool useHealthCheck = true)
    {
        if (useHealthCheck)
        {
            builder.UseGrpcHealthCheck();
        }

        var method = typeof(GrpcEndpointRouteBuilderExtensions).GetMethod(nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService));
        if (method == null)
        {
            return;
        }

        var definedTypes = Assembly.GetEntryAssembly()?.DefinedTypes;

        if (definedTypes == null)
        {
            return;
        }

        var types = definedTypes.Where(t => t.IsClass)
                                .Where(t => t.IsAbstract == false)
                                .Where(t => t.BaseType != null && t.BaseType.IsAbstract)
                                .Where(t => t.BaseType!.GetCustomAttributes<BindServiceMethodAttribute>().Any());

        foreach (var type in types)
        {
            method.MakeGenericMethod(type.AsType()).Invoke(null, new object[] { builder });
        }
    }

    /// <summary>
    /// 启用 gRPC 健康检查终结点。
    /// </summary>
    /// <param name="builder">要映射健康检查终结点的路由构建器，不能为 <see langword="null"/>。</param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> 为 <see langword="null"/>。</exception>
    public static void UseGrpcHealthCheck(this IEndpointRouteBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.MapGrpcService<HealthService>();
    }
}