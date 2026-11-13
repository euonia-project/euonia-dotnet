namespace Nerosoft.Euonia.Bus.Tests.Requests;

/// <summary>
/// 示例请求消息，其处理器以 <see cref="NotFoundException"/> 失败，用于验证调用异常传播。
/// </summary>
public class UserExceptionRequest : IRequest<int>
{
}

