namespace Nerosoft.Euonia.Application;

/// <summary>
/// 表示用例输出为成功时的结构。
/// </summary>
/// <typeparam name="TOutput">输出结果的类型。</typeparam>
public interface IUseCaseOutputSuccess<in TOutput>
{
	/// <summary>
	/// 以成功结果完成用例。
	/// </summary>
	/// <param name="output">成功输出的结果。</param>
	void Ok(TOutput output);
}