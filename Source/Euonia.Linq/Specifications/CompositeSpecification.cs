using System.Linq.Expressions;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 复合规约的基类。
/// </summary>
/// <typeparam name="TEntity">检查此规约的实体类型。</typeparam>
public sealed class CompositeSpecification<TEntity> : Specification<TEntity>
    where TEntity : class
{
    private readonly List<ISpecification<TEntity>> _specifications = new();

    private readonly PredicateOperator _composeType;

    /// <summary>
    /// 初始化 <see cref="CompositeSpecification{T}"/> 类的新实例。
    /// </summary>
    /// <param name="composeType">组合方式。</param>
    public CompositeSpecification(PredicateOperator composeType)
    {
        _composeType = composeType;
    }

    /// <summary>
    /// 向复合规约添加一个或多个规约。
    /// </summary>
    /// <param name="specifications">要添加的规约。</param>
    /// <returns>复合规约本身。</returns>
    /// <exception cref="ArgumentException">未提供任何规约时抛出。</exception>
    public CompositeSpecification<TEntity> With(params ISpecification<TEntity>[] specifications)
	{
		if (specifications == null || specifications.Length == 0)
		{
			throw new ArgumentException("At least 1 specification.");
		}
		_specifications.AddRange(specifications);
		return this;
	}

	/// <summary>
    /// 向复合规约添加一个规约。
    /// </summary>
    /// <param name="specification">要添加的规约。</param>
    /// <returns>复合规约本身。</returns>
    public CompositeSpecification<TEntity> Add(ISpecification<TEntity> specification)
    {
        _specifications.Add(specification);
        return this;
    }

    /// <summary>
    /// 添加新的规约。
    /// </summary>
    /// <param name="specification">用于获取要添加规约的委托。</param>
    /// <returns>复合规约本身。</returns>
    public CompositeSpecification<TEntity> Add(Func<ISpecification<TEntity>> specification)
    {
        _specifications.Add(specification());
        return this;
    }

    /// <summary>
    /// 当条件为 <see langword="true"/> 时添加新的规约。
    /// </summary>
    /// <param name="condition">是否添加的条件。</param>
    /// <param name="specification">要添加的规约。</param>
    /// <returns>复合规约本身。</returns>
    public CompositeSpecification<TEntity> AddIf(bool condition, ISpecification<TEntity> specification)
    {
        if (condition)
        {
            _specifications.Add(specification);
        }
        return this;
    }

    /// <summary>
    /// 当条件为 <see langword="true"/> 时添加新的规约。
    /// </summary>
    /// <param name="condition">是否添加的条件。</param>
    /// <param name="specification">用于获取要添加规约的委托。</param>
    /// <returns>复合规约本身。</returns>
    public CompositeSpecification<TEntity> AddIf(bool condition, Func<ISpecification<TEntity>> specification)
    {
        if (condition)
        {
            _specifications.Add(specification());
        }
        return this;
    }

    /// <summary>
    /// 当条件委托返回 <see langword="true"/> 时添加新的规约。
    /// </summary>
    /// <param name="condition">返回是否添加的条件委托。</param>
    /// <param name="specification">要添加的规约。</param>
    /// <returns>复合规约本身。</returns>
    public CompositeSpecification<TEntity> AddIf(Func<bool> condition, ISpecification<TEntity> specification)
    {
        if (condition())
        {
            _specifications.Add(specification);
        }
        return this;
    }

    /// <summary>
    /// 当条件委托返回 <see langword="true"/> 时添加新的规约。
    /// </summary>
    /// <param name="condition">返回是否添加的条件委托。</param>
    /// <param name="specification">用于获取要添加规约的委托。</param>
    /// <returns>复合规约本身。</returns>
    public CompositeSpecification<TEntity> AddIf(Func<bool> condition, Func<ISpecification<TEntity>> specification)
    {
        if (condition())
        {
            _specifications.Add(specification());
        }
        return this;
    }

    /// <inheritdoc />
    /// <returns>按组合方式合并所有子规约后的谓词表达式。</returns>
    public override Expression<Func<TEntity, bool>> Satisfy()
    {
        var expressions = _specifications.Select(t => t.Satisfy());
        return expressions.Compose(_composeType);
    }
}
