namespace Nerosoft.Euonia.Security;

/// <summary>
/// 数据权限模型的基类，使用方应派生本类来声明一种资源的访问控制。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// 一个完整的声明同时给出「资源在各维度上的取值」与「访问策略」，例如：
/// <code>
/// public sealed class OrderScope : ScopeModel&lt;Order&gt;
/// {
///     public override void Define(ScopeModelBuilder&lt;Order&gt; builder)
///         =&gt; builder.Map(ScopeDimensions.Dept, x =&gt; x.DeptId);
///
///     public override ScopePolicy&lt;Order&gt; Policy =&gt; ScopePolicy&lt;Order&gt;.Grant(ScopeDimensions.Dept);
///
///     public override void Declare(ScopePolicySet&lt;Order&gt; policies)
///         =&gt; policies.For("order:delete", ScopePolicy&lt;Order&gt;.Grant("id"));
/// }
/// </code>
/// 模型在程序集扫描时自动发现并在启动期校验；同一资源类型存在多个模型、策略引用了未映射的维度、
/// 或两条声明抢同一个标识/授予键都会导致启动失败（更多示例见 README §5.2）。
/// </remarks>
public abstract class ScopeModel<T> : IScopeModel<T>
	where T : class
{
	/// <summary>
	/// 按授权标识声明的策略集合，首次访问时构建。
	/// </summary>
	private ScopePolicySet<T> _policies;

	/// <summary>
	/// 类型擦除后的只读视图，首次访问时构建。
	/// </summary>
	private IReadOnlyDictionary<string, object> _declaredPolicies;

	private IReadOnlyDictionary<string, string> _declaredKeys;

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
	/// 逐条声明行级策略（可选）。未声明的标识一律使用 <see cref="Policy"/>。
	/// </summary>
	/// <param name="policies">策略集合。</param>
	/// <remarks>
	/// 用于表达「同一用户、同一类型、不同操作行权限不同」：把资源标识也作为一个维度映射，
	/// 再为不同标识声明不同的行范围。<see cref="ScopePolicySet{T}.For"/> 按权限码声明（标识与授予键都是该码），
	/// <see cref="ScopePolicySet{T}.ForOperation"/> 按操作名声明并可指定该操作的授予键。
	/// </remarks>
	public virtual void Declare(ScopePolicySet<T> policies)
	{
	}

	#region IScopeModel 显式实现

	/// <inheritdoc />
	IReadOnlyDictionary<string, object> IScopeModel.DeclaredPolicies => GetDeclaredPolicies();

	/// <inheritdoc />
	IReadOnlyDictionary<string, string> IScopeModel.DeclaredKeys => GetDeclaredKeys();

	/// <summary>
	/// 延迟构建并缓存按标识声明的策略集合。
	/// </summary>
	/// <remarks>
	/// <b>构建成功后才发布</b>：<see cref="Declare"/> 在声明互相冲突时会抛出，
	/// 若先把空集合赋给字段再调用它，失败后字段会留下一个<b>半成品</b>——
	/// 下次访问直接返回它而不再重试 <see cref="Declare"/>，冲突就此变成静默的缺失声明。
	/// </remarks>
	private ScopePolicySet<T> GetPolicySet()
	{
		if (_policies == null)
		{
			var policies = new ScopePolicySet<T>();
			Declare(policies);
			_policies = policies;
		}

		return _policies;
	}

	private IReadOnlyDictionary<string, object> GetDeclaredPolicies()
	{
		if (_declaredPolicies == null)
		{
			var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			foreach (var (identifier, _, policy) in GetPolicySet().Declarations)
			{
				map[identifier] = policy;
			}

			_declaredPolicies = map;
		}

		return _declaredPolicies;
	}

	private IReadOnlyDictionary<string, string> GetDeclaredKeys()
	{
		if (_declaredKeys == null)
		{
			var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach (var (identifier, key, _) in GetPolicySet().Declarations)
			{
				map[identifier] = key;
			}

			_declaredKeys = map;
		}

		return _declaredKeys;
	}

	#endregion
}
