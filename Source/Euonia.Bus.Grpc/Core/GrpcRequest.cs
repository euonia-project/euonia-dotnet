using System.Text.Json;

namespace Google.Protobuf;

/// <summary>
/// gRPC 请求消息的部分类扩展，提供从任意对象构造请求的构造函数。
/// </summary>
/// <remarks>
/// 该类与 protobuf 生成的 <c>GrpcRequest</c> 合并，构造时把传入对象写入生成的 <c>Data</c> 字段。
/// </remarks>
public partial class GrpcRequest
{
    /// <summary>
    /// 使用指定对象初始化 <see cref="GrpcRequest"/> 的新实例。
    /// </summary>
    /// <param name="data">要写入 <c>Data</c> 字段的请求数据；引用类型按 JSON 序列化，值类型使用 <see cref="object.ToString"/> 转换结果。</param>
    /// <exception cref="NullReferenceException">当 <paramref name="data"/> 为 <see langword="null"/> 时抛出。</exception>
    public GrpcRequest(object data)
    {
        Data = data.GetType().IsClass ? JsonSerializer.Serialize(data) : data.ToString();
    }
}