namespace Nerosoft.Euonia.Bus.Tests.Commands;

/// <summary>
/// 示例命令，声明仅经内存传输收发并要求返回 <see cref="int"/> 结果。
/// </summary>
[DispatchIn("inmemory"), ReceiveIn("inmemory")]
[Request(typeof(int))]
public class UserCreateCommand
{
}