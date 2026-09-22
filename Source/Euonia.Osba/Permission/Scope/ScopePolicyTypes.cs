using System.Linq.Expressions;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 「资源在指定维度上的值属于用户被授予的集合」对应的策略节点。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
internal sealed class GrantScopePolicy<T> : ScopePolicy<T>
	where T : class
{
	private readonly string _dimension;

	internal GrantScopePolicy(string dimension)
	{
		_dimension = dimension;
	}

	internal override ScopePolicyNode<T> Reduce(ScopeCompileContext<T> context)
	{
		return ScopePolicyNode<T>.AllowOnly(context.GrantCondition(_dimension));
	}

	internal override void CollectLeaves(ScopeCompileContext<T> context, bool negated, List<ScopePolicyLeaf<T>> traces)
	{
		traces.Add(new ScopePolicyLeaf<T>($"Grant({_dimension})", context.Lambda(context.GrantCondition(_dimension)), negated));
	}

	public override string ToString()
	{
		return $"Grant({_dimension})";
	}
}

/// <summary>
/// 直接谓词条件对应的策略节点。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
internal sealed class WhereScopePolicy<T> : ScopePolicy<T>
	where T : class
{
	private readonly Expression<Func<T, bool>> _predicate;

	internal WhereScopePolicy(Expression<Func<T, bool>> predicate)
	{
		_predicate = predicate;
	}

	internal override ScopePolicyNode<T> Reduce(ScopeCompileContext<T> context)
	{
		return ScopePolicyNode<T>.AllowOnly(context.Rebind(_predicate));
	}

	internal override void CollectLeaves(ScopeCompileContext<T> context, bool negated, List<ScopePolicyLeaf<T>> traces)
	{
		traces.Add(new ScopePolicyLeaf<T>($"Where({_predicate})", context.Lambda(context.Rebind(_predicate)), negated));
	}

	public override string ToString()
	{
		return $"Where({_predicate})";
	}
}

/// <summary>
/// 逻辑「与」策略节点。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
internal sealed class AllScopePolicy<T> : ScopePolicy<T>
	where T : class
{
	private readonly ScopePolicy<T>[] _policies;

	internal AllScopePolicy(ScopePolicy<T>[] policies)
	{
		_policies = policies;
	}

	internal override ScopePolicyNode<T> Reduce(ScopeCompileContext<T> context)
	{
		var allows = new List<Expression>();
		var denies = new List<Expression>();

		foreach (var policy in _policies)
		{
			var node = policy.Reduce(context);

			if (node.HasAllow)
			{
				allows.Add(node.Allow);
			}

			if (!ScopePolicyNode<T>.IsConstantFalse(node.Deny))
			{
				denies.Add(node.Deny);
			}
		}

		// 全部分支都没有允许条件时，「与」不施加任何限制（例如全部是 Deny 分支 → 拒绝清单语义）
		return new ScopePolicyNode<T>(
			ScopePolicyNode<T>.Fold(allows, Expression.AndAlso, ScopePolicyNode<T>.True),
			ScopePolicyNode<T>.Fold(denies, Expression.OrElse, ScopePolicyNode<T>.False),
			allows.Count > 0);
	}

	internal override void CollectLeaves(ScopeCompileContext<T> context, bool negated, List<ScopePolicyLeaf<T>> traces)
	{
		foreach (var policy in _policies)
		{
			policy.CollectLeaves(context, negated, traces);
		}
	}

	public override string ToString()
	{
		return $"All({string.Join(", ", _policies.AsEnumerable())})";
	}
}

/// <summary>
/// 逻辑「或」策略节点。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
internal sealed class AnyScopePolicy<T> : ScopePolicy<T>
	where T : class
{
	private readonly ScopePolicy<T>[] _policies;

	internal AnyScopePolicy(ScopePolicy<T>[] policies)
	{
		_policies = policies;
	}

	internal override ScopePolicyNode<T> Reduce(ScopeCompileContext<T> context)
	{
		var allows = new List<Expression>();
		var denies = new List<Expression>();

		foreach (var policy in _policies)
		{
			var node = policy.Reduce(context);

			// 只带拒绝条件的分支不参与「或」，否则 Allow 会变成恒真（静默提权）
			if (node.HasAllow)
			{
				allows.Add(node.Allow);
			}

			if (!ScopePolicyNode<T>.IsConstantFalse(node.Deny))
			{
				denies.Add(node.Deny);
			}
		}

		// 没有任何分支提供允许条件时一律拒绝（fail-closed）
		return new ScopePolicyNode<T>(
			ScopePolicyNode<T>.Fold(allows, Expression.OrElse, ScopePolicyNode<T>.False),
			ScopePolicyNode<T>.Fold(denies, Expression.OrElse, ScopePolicyNode<T>.False),
			allows.Count > 0);
	}

	internal override void CollectLeaves(ScopeCompileContext<T> context, bool negated, List<ScopePolicyLeaf<T>> traces)
	{
		foreach (var policy in _policies)
		{
			policy.CollectLeaves(context, negated, traces);
		}
	}

	public override string ToString()
	{
		return $"Any({string.Join(", ", _policies.AsEnumerable())})";
	}
}

/// <summary>
/// 「拒绝」策略节点：子策略的允许条件成为拒绝条件，并压过所有允许条件。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
internal sealed class DenyScopePolicy<T> : ScopePolicy<T>
	where T : class
{
	private readonly ScopePolicy<T> _policy;

	internal DenyScopePolicy(ScopePolicy<T> policy)
	{
		_policy = policy;
	}

	internal override ScopePolicyNode<T> Reduce(ScopeCompileContext<T> context)
	{
		var inner = _policy.Reduce(context);

		// 子策略「成立」的完整条件 = 其允许条件 ∨ 其自身的拒绝条件
		var conditions = new List<Expression>();
		if (!ScopePolicyNode<T>.IsConstantFalse(inner.Allow))
		{
			conditions.Add(inner.Allow);
		}

		if (!ScopePolicyNode<T>.IsConstantFalse(inner.Deny))
		{
			conditions.Add(inner.Deny);
		}

		return ScopePolicyNode<T>.DenyOnly(ScopePolicyNode<T>.Fold(conditions, Expression.OrElse, ScopePolicyNode<T>.False));
	}

	internal override void CollectLeaves(ScopeCompileContext<T> context, bool negated, List<ScopePolicyLeaf<T>> traces)
	{
		// 进入 Deny 即翻转一次语义：Deny 之下的叶子成为拒绝条件
		_policy.CollectLeaves(context, !negated, traces);
	}

	public override string ToString()
	{
		return $"Deny({_policy})";
	}
}
