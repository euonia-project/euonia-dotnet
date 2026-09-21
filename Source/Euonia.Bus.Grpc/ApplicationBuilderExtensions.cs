using System.Reflection;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nerosoft.Euonia.Grpc;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for <see cref="IApplicationBuilder"/> to add gRPC server features.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseGrpcEndpoints(this IApplicationBuilder builder, Action<IEndpointRouteBuilder> configure = null)
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
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="useHealthCheck"></param>
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
                                .Where(t => t.BaseType.GetCustomAttributes<BindServiceMethodAttribute>().Any());

        foreach (var type in types)
        {
            method.MakeGenericMethod(type.AsType()).Invoke(null, new object[] { builder });
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void UseGrpcHealthCheck(this IEndpointRouteBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.MapGrpcService<HealthService>();
    }
}