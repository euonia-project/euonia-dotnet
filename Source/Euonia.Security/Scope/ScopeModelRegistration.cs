namespace Nerosoft.Euonia.Security;

/// <summary>
/// 一个资源类型的权限模型注册项：模型描述、默认策略与逐条声明的行级策略，
/// 并负责把判定入口给出的<b>授权标识</b>解析成「策略 + 授予键」。
/// </summary>
/// <remarks>
/// <para>
/// 策略的类型参数只有在运行期扫描后才能确定，因此这里以 <see cref="object"/> 持有，
/// 由 <see cref="ScopeGuard"/> 在按类型解析时完成强类型转换。
/// </para>
/// <para>
/// 「标识 → 策略 + 键」是本类唯一的职责，三个来源按固定次序尝试（见 <see cref="TryResolve"/>）：
/// 模型声明的标识、模型声明的<b>授予键</b>、以及「谁都没声明」的回落。
/// </para>
/// </remarks>
public sealed class ScopeModelRegistration
{
	/// <summary>授权标识 → 策略；实际类型是 <see cref="ScopePolicy{T}"/>。</summary>
	private readonly IReadOnlyDictionary<string, object> _policies;

	/// <summary>授权标识 → 授予键。</summary>
	private readonly IReadOnlyDictionary<string, string> _keys;

	/// <summary>授予键 → 授权标识（键与标识共用命名空间，因此这是一一对应的反向索引）。</summary>
	private readonly Dictionary<string, string> _byKey;

	/// <summary>
	/// 初始化 <see cref="ScopeModelRegistration"/> 的新实例。
	/// </summary>
	/// <param name="descriptor">模型描述。</param>
	/// <param name="model">模型实例，用于取默认策略与逐条声明的策略。</param>
	internal ScopeModelRegistration(ScopeModelDescriptor descriptor, IScopeModel model)
	{
		Descriptor = descriptor;
		Model = model;

		_policies = model.DeclaredPolicies;
		_keys = model.DeclaredKeys;
		_byKey = new Dictionary<string, string>(_keys.Count, StringComparer.OrdinalIgnoreCase);

		foreach (var (identifier, key) in _keys)
		{
			_byKey[key] = identifier;
		}
	}

	/// <summary>
	/// 模型描述：资源类型、维度映射与分类属性，由 <see cref="ScopeModelDescriptor.Create"/> 从模型实例构建。
	/// </summary>
	public ScopeModelDescriptor Descriptor { get; }

	/// <summary>
	/// 模型实例：默认策略与逐条声明的行级策略都由它提供，本注册项只做类型擦除后的转发。
	/// </summary>
	internal IScopeModel Model { get; }

	/// <summary>
	/// 默认策略：未单独声明策略的标识都回落到它；实际类型是 <see cref="ScopePolicy{T}"/>（T 为资源类型），
	/// 因类型参数在运行期才确定而以 <see cref="object"/> 持有，由 <see cref="ScopeGuard"/> 解析时完成强类型转换。
	/// </summary>
	internal object DefaultPolicy => Model.PolicyObject;

	/// <summary>
	/// 解析一个授权标识。
	/// </summary>
	/// <param name="identifier">业务操作名或权限码。</param>
	/// <param name="policy">解析出的策略；未声明时返回 <see langword="null"/>（调用方回落到 <see cref="DefaultPolicy"/>）。</param>
	/// <param name="key">解析出的授予键。</param>
	/// <returns>命中了一条声明则返回 <see langword="true"/>。</returns>
	/// <remarks>
	/// <para>
	/// 三个来源按次序尝试：
	/// </para>
	/// <list type="number">
	/// <item><description><b>标识</b>——模型声明过的那个名字（<c>For</c> 声明的码、<c>ForOperation</c> 声明的操作）。</description></item>
	/// <item><description><b>授予键</b>——键与标识共用命名空间，因此「为 <c>read</c> 声明策略、授予键写 <c>repository:view</c>」
	/// 之后，用 <c>repository:view</c> 来寻址同样命中那一条。少了这一步，按权限码调用一个按操作声明的策略会
	/// <b>静默回落到默认策略</b>——调用方以为拿到了窄化后的行范围，实际拿到的是最宽的那条。</description></item>
	/// <item><description><b>回落</b>——谁都没声明：策略为 <see langword="null"/>，键取标识自身
	/// （见 <see cref="ScopeKeys.KeyFor"/>）。以自身为键意味着调用方在这个名字下写的授予仍然生效，
	/// 没写则与从前一样回落到 <see cref="ScopeKeys.Default"/>。</description></item>
	/// </list>
	/// </remarks>
	internal bool TryResolve(string identifier, out object policy, out string key)
	{
		var name = Normalize(identifier);

		if (name == null)
		{
			policy = null;
			key = ScopeKeys.Default;
			return false;
		}

		if (_policies.TryGetValue(name, out policy))
		{
			key = _keys[name];
			return true;
		}

		if (_byKey.TryGetValue(name, out var owner))
		{
			policy = _policies[owner];
			key = _keys[owner];
			return true;
		}

		policy = null;
		key = ScopeKeys.KeyFor(name);
		return false;
	}

	/// <summary>
	/// 规范化标识：空白一律视为「未指定」，其余去掉首尾空白。
	/// </summary>
	/// <remarks>
	/// 不规范化的话，<c>"delete"</c> 与 <c>"delete "</c> 会是两条不同的判定路径，
	/// 而后者会连授予键都取错。
	/// </remarks>
	private static string Normalize(string identifier)
	{
		return string.IsNullOrWhiteSpace(identifier) ? null : identifier.Trim();
	}
}
