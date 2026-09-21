using System.Text.Json;

namespace Google.Protobuf;

/// <summary>
/// To be added.
/// </summary>
public partial class GrpcResponse
{
    /// <summary>
    /// To be added.
    /// </summary>
    /// <param name="value"></param>
    public GrpcResponse(object value)
    {
        Data = value.GetType().IsClass ? JsonSerializer.Serialize(value) : value.ToString();
    }
}