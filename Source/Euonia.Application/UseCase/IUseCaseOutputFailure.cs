namespace Nerosoft.Euonia.Application;

/// <summary>
/// 表示用例输出为失败时的结构。
/// </summary>
public interface IUseCaseOutputFailure
{
	/// <summary>
	/// 报告用例执行失败。
	/// </summary>
	/// <param name="exception">导致用例失败的异常。</param>
	void Error(Exception exception);
}