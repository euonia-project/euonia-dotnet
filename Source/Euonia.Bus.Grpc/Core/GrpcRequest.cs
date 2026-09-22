using System.Text.Json;

namespace Google.Protobuf;

/// <summary>
/// To be added.
/// </summary>
public partial class GrpcRequest
{
    /// <summary>
    /// To be added.
    /// </summary>
    /// <param name="data"></param>
    public GrpcRequest(object data)
    {
        Data = data.GetType().IsClass ? JsonSerializer.Serialize(data) : data.ToString();
    }
}