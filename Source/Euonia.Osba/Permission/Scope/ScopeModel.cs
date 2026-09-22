namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据权限模型的基类，使用方应派生本类来声明一种资源的访问控制。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// <para>
/// 一个完整的声明同时给出「资源在各维度上的取值」与「访问策略」，例如：
/// </para>
/// <code>
/// public sealed class OrderScope : ScopeModel&lt;Order&gt;
/// {
///     public override void Define(ScopeModelBuilder&lt;Order&gt; builder)
///     {
///         builder.Map(ScopeDimensions.Owner, x =&gt; x.OwnerId)
///                .Map(ScopeDimensions.Dept, x =&gt; x.DeptId)
///                .Classify("classification", x =&gt; x.Level);
///     }
///
///     public override ScopePolicy&lt;Order&gt; Policy =&gt;
///         ScopePolicy&lt;Order&gt;.All(
///             ScopePolicy&lt;Order&gt;.Any(
///                 ScopePolicy&lt;Order&gt;.Self(),
///                 ScopePolicy&lt;Order&gt;.Grant(ScopeDimensions.Dept)),
///             ScopePolicy&lt;Order&gt;.Deny(
///                 ScopePolicy&lt;Order&gt;.Where(x =&gt; x.Level == "secret")));
/// }
/// </code>
/// <para>
/// 模型由 <c>AddBusinessObject</c> 在程序集扫描时自动发现，并在启动期校验；
/// 同一资源类型存在多个模型、或策略引用了未映射的维度，都会导致启动失败。
/// </para>
/// </remarks>
public abstract class ScopeModel<T> : IScopeModel<T>
	where T : class
{
	/// <summary>
	/// 按权限码声明的策略集合，首次访问时构建。
	/// </summary>
	private ScopePolicySet<T> _policies;

	/// <inheritdoc />
	public Type ResourceType => typeof(T);

	/// <inheritdoc />
	object IScopeModel.PolicyObject => Policy;

	/// <inheritdoc />
	void IScopeModel.Define(IScopeModelBuilder builder)
	{
		Define((ScopeModelBuilder<T>)builder);
	}

	/// <summary>
	/// 定义资源在各维度上的取值来源，以及不参与授权的分类属性。
	/// </summary>
	/// <param name="builder">模型构建器。</param>
	public abstract void Define(ScopeModelBuilder<T> builder);

	/// <inheritdoc />
	public abstract ScopePolicy<T> Policy { get; }

	/// <summary>
	/// 按权限码声明行级策略（可选）。未声明的码一律使用 <see cref="Policy"/>。
	/// </summary>
	/// <param name="policies">策略集合。</param>
	/// <remarks>
	/// 用于表达「同一用户、同一类型、不同行权限不同」：把资源标识也作为一个维度映射，
	/// 再为不同权限码声明不同的行范围。
	/// </remarks>
	public virtual void Declare(ScopePolicySet<T> policies)
	{
	}

	#region IScopeModel 显式实现

	/// <inheritdoc />
	object IScopeModel.PolicyFor(string code)
	{
		return GetPolicySet().TryGet(code, out var policy) ? policy : null;
	}

	/// <inheritdoc />
	IReadOnlyCollection<string> IScopeModel.DeclaredCodes => GetPolicySet().Codes;

	/// <summary>
	/// 延迟构建并缓存按码声明的策略集合。
	/// </summary>
	private ScopePolicySet<T> GetPolicySet()
	{
		if (_policies == null)
		{
			_policies = new ScopePolicySet<T>();
			Declare(_policies);
		}

		return _policies;
	}

	#endregion
}
