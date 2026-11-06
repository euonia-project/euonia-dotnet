using Grpc.Health.V1;
using Grpc.HealthCheck;

namespace Nerosoft.Euonia.Grpc;

/// <summary>
/// gRPC 健康检查服务。
/// </summary>
public class HealthService : HealthServiceImpl
{
    private const string SERVICE_NAME = "HealthCheck";

    /// <summary>
    /// 初始化 <see cref="HealthService"/> 类的新实例。
    /// </summary>
    public HealthService()
    {
        SetStatus(SERVICE_NAME, HealthCheckResponse.Types.ServingStatus.Serving);
    }
}