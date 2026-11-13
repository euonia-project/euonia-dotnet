namespace Nerosoft.Euonia.Bus.Tests.Commands;

/// <summary>
/// 示例单播命令，其处理器抛出 <see cref="NotFoundException"/>，用于验证异常传播。
/// </summary>
[Unicast]
public class FooDeleteCommand
{
		
}