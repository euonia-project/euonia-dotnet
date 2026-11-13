namespace Nerosoft.Euonia.Bus.Tests.Commands;

//[Channel("foo.create")]
/// <summary>
/// 示例单播命令，配合处理器上的通道订阅验证带响应的命令发送。
/// </summary>
public class FooCreateCommand : IUnicast
{
}
