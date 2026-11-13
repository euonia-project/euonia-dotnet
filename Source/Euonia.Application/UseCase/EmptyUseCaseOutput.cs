namespace Nerosoft.Euonia.Application;

/// <summary>
/// 表示不应返回任何输出的用例。
/// </summary>
public sealed record EmptyUseCaseOutput : IUseCaseOutput
{
	/// <summary>
	/// 获取 <see cref="EmptyUseCaseOutput"/> 的新实例。
	/// </summary>
	public static EmptyUseCaseOutput Instance { get; } = new();
}