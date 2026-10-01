using System.Linq.Expressions;

namespace Nerosoft.Euonia.Security;

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
	/// 已声明的维度映射：维度名 → 取值表达式，表达式以资源类型为参数并返回 <see cref="string"/>
	/// （单值维度，通常是实体的列值）或 <see cref="IEnumerable{T}"/>（集合维度，取值来自子表/关系表）。
	/// 名称大小写不敏感，一个模型至少要映射一个维度。
	/// </summary>
	IReadOnlyDictionary<string, LambdaExpression> Dimensions { get; }

	/// <summary>
	/// 已声明的分类属性映射：分类名 → 取值表达式，表达式以资源类型为参数（可返回任意类型）。
	/// 分类不参与授权，只供 <see cref="ScopePolicy{T}.Where"/> 之类的谓词引用。
	/// </summary>
	IReadOnlyDictionary<string, LambdaExpression> Classifications { get; }
}
