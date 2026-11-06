namespace Nerosoft.Euonia.Bus.Tests.Requests;

/// <summary>
/// 示例请求消息，期望返回 <see cref="int"/> 类型的用户数量。
/// </summary>
public class UserCountRequest : IRequest<int>
{
}