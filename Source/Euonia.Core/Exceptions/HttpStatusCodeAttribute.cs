using System.Net;

namespace System;

/// <summary>
/// 用于标记异常类对应的 HTTP 状态码的特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class HttpStatusCodeAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="HttpStatusCodeAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="statusCode">HTTP 状态码。</param>
    public HttpStatusCodeAttribute(HttpStatusCode statusCode)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// 初始化 <see cref="HttpStatusCodeAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="statusCode">HTTP 状态码的整数值。</param>
    public HttpStatusCodeAttribute(int statusCode)
        : this((HttpStatusCode)statusCode)
    {
    }

    /// <summary>
    /// 获取 HTTP 状态码。
    /// </summary>
    public HttpStatusCode StatusCode { get; }
}