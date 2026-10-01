namespace Nerosoft.Euonia.Security;

/// <summary>
/// 按<b>授权标识</b>声明资源行级策略的集合，是 <see cref="ScopeModel{T}.Declare"/> 的入参。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// <para>
/// <b>授权标识</b>是判定入口收的那个字符串，它要么是<b>操作名</b>（<c>read</c>、<c>approve</c>），
/// 要么是<b>权限码</b>（<c>repository:view</c>）。两种写法各对应一种宿主形态，见
/// <see cref="ForOperation"/> 与 <see cref="For"/>。
/// </para>
/// <para>
/// <b>一个标识一个声明位</b>：模型声明过的标识直接命中；没声明的回落到模型必填的默认策略
/// <see cref="ScopeModel{T}.Policy"/>。因为不存在「一个标识解析出多个候选项」，也就没有歧义，
/// 更没有「声明了策略却没有任何标识会用到它」的死配置。
/// </para>
/// <para>
/// 每条声明还带一个<b>授予键</b>：它只决定<b>授予值从哪来</b>（见 <see cref="ScopeSubjectSet.ValuesOf"/>）——
/// 用户在该键下被授予的维度值参与判定，没有授予时覆盖式回落到 <see cref="ScopeKeys.Default"/>。
/// 「同一资源、同一操作，不同用户看到不同行」由此表达——策略描述<b>用哪个维度</b>，
/// 授予描述<b>该维度的哪些值</b>；不同标识各写各的键，行范围因此互不干扰。
/// </para>
/// <para>
/// 授予键与标识共用同一个命名空间：一条声明的键也可以拿来当标识用（见
/// <c>ScopeModelRegistration.TryResolve</c>），因此键撞键、标识撞标识、键撞标识都会被拒绝——
/// 两个东西抢同一个键，要么是笔误，要么就是把两条策略的行权限悄悄绑在了一起。
/// </para>
/// </remarks>
public sealed class ScopePolicySet<T>
	where T : class
{
	/// <summary>标识 → (键, 策略)。</summary>
	private readonly Dictionary<string, (string Key, ScopePolicy<T> Policy)> _byIdentifier = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>键 → 标识：让「拿权限码当标识传」也能命中为同名操作声明的那条策略。</summary>
	private readonly Dictionary<string, string> _byKey = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 为指定<b>操作</b>声明行级策略。
	/// </summary>
	/// <param name="operation">业务操作名；可以是 <see cref="BusinessOperation"/> 的常量，也可以是宿主自定义的操作。</param>
	/// <param name="policy">策略。</param>
	/// <param name="scopeKey">
	/// 该操作的授予键；为 <see langword="null"/> 时取操作名自身。
	/// </param>
	/// <returns>当前集合，便于链式声明。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="policy"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="ArgumentException">当 <paramref name="operation"/> 为 <see langword="null"/>、空或仅由空白字符组成时抛出。</exception>
	/// <exception cref="InvalidOperationException">当操作使用了框架保留前缀，或标识/键与既有声明冲突时抛出。</exception>
	/// <remarks>
	/// <para>
	/// 适用形态：宿主框架能把「目标当前代表哪个操作」说出来（工厂边界、命令对象），
	/// 于是判定入口传操作名，行级授予写在 <paramref name="scopeKey"/> 给出的权限码下。
	/// </para>
	/// <para>
	/// <b>键默认取操作名自身</b>（而不是派生成 <c>@&lt;operation&gt;</c>）：保留前缀下写不进授予，
	/// 派生只会让这条策略的授予永远取不到。以自身为键则没有这个缺口。
	/// </para>
	/// </remarks>
	public ScopePolicySet<T> ForOperation(string operation, ScopePolicy<T> policy, string scopeKey = null)
	{
		ScopeKeys.ValidateOperation(operation);

		if (scopeKey != null)
		{
			ScopeKeys.Validate(scopeKey);
		}

		return Declare(operation, scopeKey ?? operation, policy);
	}

	/// <summary>
	/// 为指定<b>权限码</b>声明行级策略：标识与授予键都是这个码。
	/// </summary>
	/// <param name="code">应用声明的权限码。</param>
	/// <param name="policy">策略。</param>
	/// <returns>当前集合，便于链式声明。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="policy"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="ArgumentException">当 <paramref name="code"/> 为 <see langword="null"/>、空或仅由空白字符组成时抛出。</exception>
	/// <exception cref="InvalidOperationException">当权限码落在框架保留命名空间内，或与既有声明冲突时抛出。</exception>
	/// <remarks>
	/// <para>
	/// 适用形态：判定入口本来就只认权限码（读侧下推 <c>Apply(query, RepositoryPermissions.View)</c>、
	/// 命令体内的单行判定 <c>AllowsObject(row, RepositoryPermissions.Delete)</c>），
	/// 或者领域动作没有对应的宿主操作名。<b>它等价于 <c>ForOperation(code, policy, code)</c></b>，
	/// 只是不必把同一个字符串写两遍。
	/// </para>
	/// <para>
	/// 与 <see cref="ForOperation"/> 的取舍：宿主框架若会在调用方没给标识时按对象状态自行推断操作
	/// （<c>IObjectOperationResolver</c>），推断出来的是框架的操作名而不是应用的权限码，
	/// 这时按码声明的策略不会自动命中——要用 <see cref="ForOperation"/> 把操作与码连起来。
	/// </para>
	/// </remarks>
	public ScopePolicySet<T> For(string code, ScopePolicy<T> policy)
	{
		ScopeKeys.Validate(code);

		return Declare(code, code, policy);
	}

	/// <summary>
	/// 登记一条声明并校验命名空间占用。
	/// </summary>
	/// <param name="identifier">授权标识。</param>
	/// <param name="key">授予键。</param>
	/// <param name="policy">策略。</param>
	/// <returns>当前集合，便于链式声明。</returns>
	private ScopePolicySet<T> Declare(string identifier, string key, ScopePolicy<T> policy)
	{
		Check.EnsureNotNull(policy, nameof(policy));

		// 标识与键共用一个命名空间，因此两个名字各查两个方向：谁已经占了这块地方。
		// 少查一个方向，被撞的那条声明的行权限就会被另一条悄悄接管（拿它的键去取别人的授予）。
		Check.Ensure(
			!_byIdentifier.ContainsKey(identifier),
			Resources.IDS_SCOPE_POLICY_DUPLICATE_DECLARED,
			identifier);

		Check.Ensure(
			!IsTaken(key),
			Resources.IDS_SCOPE_KEY_DUPLICATE_DECLARED,
			key,
			OwnerOf(key));

		Check.Ensure(
			!IsTaken(identifier),
			Resources.IDS_SCOPE_KEY_DUPLICATE_DECLARED,
			identifier,
			OwnerOf(identifier));

		_byIdentifier[identifier] = (key, policy);
		_byKey[key] = identifier;

		return this;
	}

	/// <summary>
	/// 判断一个名字是否已被某个标识或某个键占用。
	/// </summary>
	private bool IsTaken(string name)
	{
		return _byIdentifier.ContainsKey(name) || _byKey.ContainsKey(name);
	}

	/// <summary>
	/// 取当前占用指定名字的标识。
	/// </summary>
	/// <remarks>
	/// 名字没被占用时返回它自身：<c>Check.Ensure</c> 的实参是<b>先求值</b>的，
	/// 本方法因此总会被调用一次，必须对「谁都没占」也给出确定答案。
	/// </remarks>
	private string OwnerOf(string name)
	{
		return _byKey.TryGetValue(name, out var owner) ? owner : name;
	}

	/// <summary>
	/// 获取已显式声明策略的授权标识集合。
	/// </summary>
	/// <remarks>
	/// 只管「声明过什么」，不回答「某个名字该怎么解析」——那件事只有一个出口，
	/// 即 <c>ScopeModelRegistration.TryResolve</c>。两处各写一份解析迟早会分叉，
	/// 而分叉的后果是判定按一条策略、审计按另一条。
	/// </remarks>
	public IReadOnlyCollection<string> Identifiers => _byIdentifier.Keys;

	/// <summary>
	/// 获取全部声明，供模型构建类型擦除后的视图。
	/// </summary>
	internal IEnumerable<(string Identifier, string Key, ScopePolicy<T> Policy)> Declarations
	{
		get
		{
			foreach (var (identifier, declared) in _byIdentifier)
			{
				yield return (identifier, declared.Key, declared.Policy);
			}
		}
	}
}
