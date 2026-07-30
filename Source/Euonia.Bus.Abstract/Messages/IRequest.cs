namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示消息是一个请求，可以期望收到响应。
/// </summary>
/// <typeparam name="TResponse">响应的类型。</typeparam>
public interface IRequest<out TResponse> : ITransportable
{
}