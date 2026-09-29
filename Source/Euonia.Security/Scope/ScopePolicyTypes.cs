using System.Linq.Expressions;

namespace Nerosoft.Euonia.Security;

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
		// 集合维度（子表维度）带 "[]" 标记：审计时要能一眼看出授予的是「资源标识」还是「子表里的值」
		var marker = context.Descriptor.IsCollectionDimension(_dimension) ? "[]" : string.Empty;

		traces.Add(new ScopePolicyLeaf<T>($"Grant({_dimension}{marker})", context.Lambda(context.GrantCondition(_dimension)), negated));
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
/// 逻辑「与」/「或」共用的组合策略：收集各子策略的允许与拒绝条件，差别只在允许条件的折叠方式。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// <para>
/// 收集口径对两者完全一致：只带拒绝条件的分支不参与允许折叠，也不计入允许分支数——
/// 「没有允许条件」不等于「允许条件为真」，混入会让本节点被当成有允许分支（静默提权）。
/// </para>
/// <para>
/// 语义差异只落在 <see cref="FoldAllows"/>：「与」把空集折叠为 <c>True</c>（全部分支都没有允许条件时不施加限制，
/// 例如全部是 Deny 分支 → 拒绝清单语义），「或」把空集折叠为 <c>False</c>（没有任何分支提供允许条件时一律拒绝，fail-closed）。
/// 归约逻辑只此一份，将来加规则不会漏改其中一侧——那正是「与/或语义漂移」。
/// </para>
/// </remarks>
internal abstract class CompositeScopePolicy<T> : ScopePolicy<T>
	where T : class
{
	private readonly ScopePolicy<T>[] _policies;

	private protected CompositeScopePolicy(ScopePolicy<T>[] policies)
	{
		_policies = policies;
	}

	/// <summary>审计输出用的算子名：<c>All</c> 或 <c>Any</c>。</summary>
	private protected abstract string OperatorName { get; }

	/// <summary>把收集到的允许条件折叠成一个表达式；空集的取值决定本节点的默认语义。</summary>
	private protected abstract Expression FoldAllows(List<Expression> allows);

	/// <inheritdoc />
	internal sealed override ScopePolicyNode<T> Reduce(ScopeCompileContext<T> context)
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

		return new ScopePolicyNode<T>(
			FoldAllows(allows),
			ScopePolicyNode<T>.Fold(denies, Expression.OrElse, ScopePolicyNode<T>.False),
			allows.Count > 0);
	}

	/// <inheritdoc />
	internal sealed override void CollectLeaves(ScopeCompileContext<T> context, bool negated, List<ScopePolicyLeaf<T>> traces)
	{
		foreach (var policy in _policies)
		{
			policy.CollectLeaves(context, negated, traces);
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"{OperatorName}({string.Join(", ", _policies.AsEnumerable())})";
	}
}

/// <summary>
/// 逻辑「与」策略节点。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
internal sealed class AllScopePolicy<T> : CompositeScopePolicy<T>
	where T : class
{
	internal AllScopePolicy(ScopePolicy<T>[] policies)
		: base(policies)
	{
	}

	private protected override string OperatorName => "All";

	private protected override Expression FoldAllows(List<Expression> allows)
	{
		// 空集折叠为 True：全部分支都没有允许条件时「与」不施加任何限制
		// （例如全部是 Deny 分支 → 拒绝清单语义）
		return ScopePolicyNode<T>.Fold(allows, Expression.AndAlso, ScopePolicyNode<T>.True);
	}
}

/// <summary>
/// 逻辑「或」策略节点。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
internal sealed class AnyScopePolicy<T> : CompositeScopePolicy<T>
	where T : class
{
	internal AnyScopePolicy(ScopePolicy<T>[] policies)
		: base(policies)
	{
	}

	private protected override string OperatorName => "Any";

	private protected override Expression FoldAllows(List<Expression> allows)
	{
		// 空集折叠为 False：没有任何分支提供允许条件时一律拒绝（fail-closed）
		return ScopePolicyNode<T>.Fold(allows, Expression.OrElse, ScopePolicyNode<T>.False);
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
