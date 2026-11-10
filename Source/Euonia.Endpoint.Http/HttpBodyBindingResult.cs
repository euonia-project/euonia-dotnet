namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 生成端点的请求体绑定结果。
/// </summary>
/// <typeparam name="T">请求体类型。</typeparam>
/// <param name="Succeeded">绑定是否成功。</param>
/// <param name="Value">绑定得到的请求体。</param>
public readonly record struct HttpBodyBindingResult<T>(bool Succeeded, T? Value);
