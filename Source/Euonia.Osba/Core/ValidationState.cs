namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务对象的校验状态。
/// </summary>
/// <remarks>
/// <see cref="BusinessObject.IsValid"/> 是布尔值，首次校验之前恒为 <see langword="true"/>，
/// 无法区分“没校验过”和“校验通过”。本枚举补上这个语义：
/// <see cref="NotValidated"/> 表示结论未知，<see cref="Valid"/> / <see cref="Invalid"/> 才是结论。
/// </remarks>
public enum ValidationState
{
	/// <summary>尚未校验，或校验结论已因对象变更而失效。</summary>
	NotValidated,

	/// <summary>最近一次完整校验通过。</summary>
	Valid,

	/// <summary>最近一次校验存在 Error 级违规。</summary>
	Invalid
}
