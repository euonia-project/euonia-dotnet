using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 模型构建器的非泛型视图，供框架在不了解资源类型的前提下读取已声明的映射。
/// </summary>
/// <remarks>
/// 使用方不直接实现本接口，而是通过 <see cref="ScopeModel{T}.Define"/> 接收强类型的
/// <see cref="ScopeModelBuilder{T}"/>。
/// </remarks>
public interface IScopeModelBuilder
{
	/// <summary>
	/// 获取已声明的维度映射：维度名 → 取值表达式。
	/// </summary>
	IReadOnlyDictionary<string, LambdaExpression> Dimensions { get; }

	/// <summary>
	/// 获取已声明的分类属性映射：分类名 → 取值表达式。
	/// </summary>
	IReadOnlyDictionary<string, LambdaExpression> Classifications { get; }
}
