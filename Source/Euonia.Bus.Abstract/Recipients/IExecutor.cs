namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息执行器接口
/// </summary>
/// <remarks>用于处理请求-响应模式的消息，并返回执行结果。</remarks>
public interface IExecutor : IRecipient
{
}