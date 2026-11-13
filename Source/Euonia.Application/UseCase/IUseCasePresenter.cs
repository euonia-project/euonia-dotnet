namespace Nerosoft.Euonia.Application;

/// <summary>
/// 定义用例执行响应呈现器的结构。
/// </summary>
/// <typeparam name="TOutput">用例执行结果（输出）的类型。</typeparam>
public interface IUseCasePresenter<TOutput> : IUseCaseOutputFailure,
											  IUseCaseOutputSuccess<TOutput>,
											  IDisposable
{
	/// <summary>
	/// 在用例成功执行时发生。
	/// </summary>
	event EventHandler<TOutput>? OnSucceed;

	/// <summary>
	/// 在用例执行失败时发生。
	/// </summary>
	event EventHandler<Exception>? OnFailed;

	/// <summary>
	/// 在用例被取消时发生。
	/// </summary>
	event EventHandler? OnCanceled;
}