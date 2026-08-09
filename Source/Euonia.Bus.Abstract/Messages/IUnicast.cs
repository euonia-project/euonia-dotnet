namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 表示消息只能投递给单个订阅者。
/// </summary>
public interface IUnicast : ITransportable
{
}