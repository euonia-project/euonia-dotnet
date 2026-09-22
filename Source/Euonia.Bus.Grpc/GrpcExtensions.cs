using System.ComponentModel;
using System.Text.Json;

namespace Google.Protobuf;

/// <summary>
/// Extension methods for <see cref="GrpcRequest"/>.
/// </summary>
public static class GrpcExtensions
{
    /// <summary>
    /// Gets the result of the <see cref="GrpcResponse"/>.
    /// </summary>
    /// <param name="response"></param>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public static TValue GetResult<TValue>(this GrpcResponse response)
    {
        return Convert<TValue>(response.Data);
    }

    /// <summary>
    /// Sets the result of the <see cref="GrpcResponse"/>.
    /// </summary>
    /// <param name="response"></param>
    /// <param name="value"></param>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
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
    /// Gets the property of the <see cref="GrpcRequest"/>.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="key"></param>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
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

        return request.Property.TryGetValue(key, out var value) ? Convert<TValue>(value) : default;
    }

    /// <summary>
    /// Sets the property of the <see cref="GrpcRequest"/>.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="request"></param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static GrpcRequest SetProperty<TValue>(this GrpcRequest request, string key, TValue value)
    {
        request.Property.TryAdd(key, Convert(value));
        return request;
    }

    /// <summary>
    /// Gets the data of the <see cref="GrpcRequest"/>.
    /// </summary>
    /// <param name="request"></param>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public static TValue GetData<TValue>(this GrpcRequest request)
    {
        if (request?.Data == null)
        {
            throw new NullReferenceException();
        }

        return Convert<TValue>(request.Data);
    }

    /// <summary>
    /// Sets the data of the <see cref="GrpcRequest"/>.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="value"></param>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
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

        return typeof(TValue).IsClass ? JsonSerializer.Serialize(content) : content.ToString();
    }

    private static TValue Convert<TValue>(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return default;
        }

        if (typeof(TValue).IsEnum)
        {
            return (TValue)Enum.Parse(typeof(TValue), content);
        }

        if (!typeof(TValue).IsClass)
        {
            return (TValue)TypeDescriptor.GetConverter(typeof(TValue))
                                         .ConvertFrom(content);
        }

        return JsonSerializer.Deserialize<TValue>(content);
    }
}