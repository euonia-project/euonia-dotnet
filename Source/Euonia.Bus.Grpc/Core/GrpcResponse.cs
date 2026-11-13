using System.Text.Json;

namespace Google.Protobuf;

/// <summary>
/// gRPC 响应消息的部分类扩展，提供从任意对象构造响应的构造函数。
/// </summary>
/// <remarks>
/// 该类与 protobuf 生成的 <c>GrpcResponse</c> 合并，构造时把传入对象写入生成的 <c>Data</c> 字段。
/// </remarks>
public partial class GrpcResponse
{
    /// <summary>
    /// 使用指定对象初始化 <see cref="GrpcResponse"/> 的新实例。
    /// </summary>
    /// <param name="value">要写入 <c>Data</c> 字段的响应数据；引用类型按 JSON 序列化，值类型使用 <see cref="object.ToString"/> 转换结果。</param>
    /// <exception cref="NullReferenceException">当 <paramref name="value"/> 为 <see langword="null"/> 时抛出。</exception>
    public GrpcResponse(object value)
    {
        Data = value.GetType().IsClass ? JsonSerializer.Serialize(value) : value.ToString();
    }
}