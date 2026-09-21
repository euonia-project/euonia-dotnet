using Grpc.Health.V1;
using Grpc.HealthCheck;

namespace Nerosoft.Euonia.Grpc;

/// <summary>
/// Grpc health check service.
/// </summary>
public class HealthService : HealthServiceImpl
{
    private const string SERVICE_NAME = "HealthCheck";

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthService"/> class.
    /// </summary>
    public HealthService()
    {
        SetStatus(SERVICE_NAME, HealthCheckResponse.Types.ServingStatus.Serving);
    }
}