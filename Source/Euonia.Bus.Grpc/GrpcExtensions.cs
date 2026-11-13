using System.ComponentModel;
using System.Text.Json;

namespace Google.Protobuf;

/// <summary>
/// <see cref="GrpcRequest"/> 的扩展方法。
/// </summary>
public static class GrpcExtensions
{
    /// <summary>
    /// 将 <see cref="GrpcResponse"/> 的 <see cref="GrpcResponse.Data"/> 转换为指定类型的结果。
    /// </summary>
    /// <typeparam name="TValue">结果的目标类型，支持类、枚举和基元类型。</typeparam>
    /// <param name="response">要读取结果的响应实例。</param>
    /// <returns>转换后的结果；当 <see cref="GrpcResponse.Data"/> 为空字符串时，返回 <typeparamref name="TValue"/> 的默认值（引用类型为 <see langword="null"/>）。</returns>
    public static TValue GetResult<TValue>(this GrpcResponse response)
    {
        return Convert<TValue>(response.Data);
    }

    /// <summary>
    /// 将指定值写入 <see cref="GrpcResponse"/> 的 <see cref="GrpcResponse.Data"/>。
    /// </summary>
    /// <typeparam name="TValue">结果值的类型。</typeparam>
    /// <param name="response">要设置结果的响应实例。</param>
    /// <param name="value">要写入的结果值；引用类型序列化为 JSON，值类型直接转换为字符串。</param>
    /// <returns>已设置结果的 <paramref name="response"/> 实例。</returns>
    /// <remarks>当 <paramref name="value"/> 为 <see langword="null"/> 时不修改响应，直接返回原实例。</remarks>
    public static GrpcResponse SetResult<TValue>(this GrpcResponse response, TValue value)
    {
        if (value == null)
        {
            return response;
        }

        if (typeof(TValue).IsClass)
        {
            response.Data = JsonSerializer.Serialize(value);
        }
        else
        {
            response.Data = value.ToString();
        }

        return response;
    }

    /// <summary>
    /// 按键从 <see cref="GrpcRequest"/> 的属性集合中读取属性值并转换为指定类型。
    /// </summary>
    /// <typeparam name="TValue">属性值的目标类型。</typeparam>
    /// <param name="request">包含属性集合的请求实例。</param>
    /// <param name="key">要读取的属性键。</param>
    /// <returns>转换后的属性值；当属性集合中不存在 <paramref name="key"/> 时，返回 <typeparamref name="TValue"/> 的默认值。</returns>
    /// <exception cref="NullReferenceException">当 <paramref name="request"/> 为 <see langword="null"/>，或其属性集合为 <see langword="null"/> 或不含任何项时抛出。</exception>
    public static TValue GetProperty<TValue>(this GrpcRequest request, string key)
    {
        if (request == null)
        {
            throw new NullReferenceException();
        }

        if (request.Property == null || request.Property.Count == 0)
        {
            throw new NullReferenceException();
        }

        return request.Property.TryGetValue(key, out var value) ? Convert<TValue>(value) : default!;
    }

    /// <summary>
    /// 将指定键和值添加到 <see cref="GrpcRequest"/> 的属性集合。
    /// </summary>
    /// <typeparam name="TValue">属性值的类型。</typeparam>
    /// <param name="request">要写入属性的请求实例。</param>
    /// <param name="key">属性键。</param>
    /// <param name="value">属性值；引用类型序列化为 JSON，值类型直接转换为字符串，<see langword="null"/> 转换为空字符串。</param>
    /// <returns>已写入属性的 <paramref name="request"/> 实例。</returns>
    /// <exception cref="NullReferenceException">当 <paramref name="request"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <remarks>若属性集合中已存在相同键，则保留原值，不会覆盖。</remarks>
    public static GrpcRequest SetProperty<TValue>(this GrpcRequest request, string key, TValue value)
    {
        request.Property.TryAdd(key, Convert(value));
        return request;
    }

    /// <summary>
    /// 将 <see cref="GrpcRequest"/> 的 <see cref="GrpcRequest.Data"/> 转换为指定类型的数据。
    /// </summary>
    /// <typeparam name="TValue">数据的目标类型。</typeparam>
    /// <param name="request">包含数据的请求实例。</param>
    /// <returns>转换后的数据；当 <see cref="GrpcRequest.Data"/> 为空字符串时，返回 <typeparamref name="TValue"/> 的默认值。</returns>
    /// <exception cref="NullReferenceException">当 <paramref name="request"/> 为 <see langword="null"/>，或其 <see cref="GrpcRequest.Data"/> 为 <see langword="null"/> 时抛出。</exception>
    public static TValue GetData<TValue>(this GrpcRequest request)
    {
        if (request?.Data == null)
        {
            throw new NullReferenceException();
        }

        return Convert<TValue>(request.Data);
    }

    /// <summary>
    /// 将指定值写入 <see cref="GrpcRequest"/> 的 <see cref="GrpcRequest.Data"/>。
    /// </summary>
    /// <typeparam name="TValue">数据值的类型。</typeparam>
    /// <param name="request">要设置数据的请求实例。</param>
    /// <param name="value">要写入的数据；引用类型序列化为 JSON，值类型直接转换为字符串，<see langword="null"/> 转换为空字符串。</param>
    /// <returns>已设置数据的 <paramref name="request"/> 实例。</returns>
    /// <exception cref="NullReferenceException">当 <paramref name="request"/> 为 <see langword="null"/> 时抛出。</exception>
    public static GrpcRequest SetData<TValue>(this GrpcRequest request, TValue value)
    {
        request.Data = Convert(value);
        return request;
    }

    private static string Convert<TValue>(TValue content)
    {
        if (content == null)
        {
            return string.Empty;
        }

        return typeof(TValue).IsClass ? JsonSerializer.Serialize(content) : content.ToString()!;
    }

    private static TValue Convert<TValue>(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return default!;
        }

        if (typeof(TValue).IsEnum)
        {
            return (TValue)Enum.Parse(typeof(TValue), content);
        }

        if (!typeof(TValue).IsClass)
        {
            return (TValue)TypeDescriptor.GetConverter(typeof(TValue))
                                         .ConvertFrom(content)!;
        }

        return JsonSerializer.Deserialize<TValue>(content)!;
    }
}