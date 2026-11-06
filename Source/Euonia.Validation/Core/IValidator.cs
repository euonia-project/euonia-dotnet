namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 对象验证器的契约，用于同步或异步验证给定类型的实例。
/// </summary>
public interface IValidator
{
	/// <summary>
	/// 如果实体状态无效，则返回错误集合。
	/// </summary>
	/// <typeparam name="TObject">实体类型</typeparam>
	/// <param name="item">包含验证错误的实例</param>
	void Validate<TObject>(TObject item)
		where TObject : class;

	/// <summary>
	/// 如果实体状态无效，则返回错误集合。
	/// </summary>
	/// <typeparam name="TObject">要验证的对象类型。</typeparam>
	/// <param name="item">要验证的实例。</param>
	/// <returns>表示异步验证过程的任务。</returns>
	Task ValidateAsync<TObject>(TObject item)
		where TObject : class;
}