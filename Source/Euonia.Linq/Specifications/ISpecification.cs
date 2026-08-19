using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 规约（Specification）模式的基础契约，更多信息参见
/// http://martinfowler.com/apsupp/spec.pdf 或 http://en.wikipedia.org/wiki/Specification_pattern。
/// 这是该模式的一个变体实现，将 Linq 和 lambda 表达式引入到模式中。
/// </summary>
/// <typeparam name="TEntity">实体类型。</typeparam>
public interface ISpecification<TEntity>
    where TEntity : class
{
    /// <summary>
    /// 判断此规约是否被指定的表达式满足。
    /// </summary>
    /// <returns>表示此规约的谓词表达式。</returns>
    Expression<Func<TEntity, bool>> Satisfy();
}