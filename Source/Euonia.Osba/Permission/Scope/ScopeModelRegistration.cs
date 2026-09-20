namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 一个资源类型的权限模型注册项：模型描述、默认策略与按码声明的行级策略。
/// </summary>
/// <remarks>
/// 策略的类型参数只有在运行期扫描后才能确定，因此这里以 <see cref="object"/> 持有，
/// 由 <see cref="ScopeGuard"/> 在按类型解析时完成强类型转换。
/// </remarks>
public sealed class ScopeModelRegistration
{
	/// <summary>
	/// 初始化 <see cref="ScopeModelRegistration"/> 的新实例。
	/// </summary>
	/// <param name="descriptor">模型描述。</param>
	/// <param name="model">模型实例，用于按权限码取行级策略。</param>
	internal ScopeModelRegistration(ScopeModelDescriptor descriptor, IScopeModel model)
	{
		Descriptor = descriptor;
		Model = model;
	}

	/// <summary>
	/// 获取模型描述。
	/// </summary>
	public ScopeModelDescriptor Descriptor { get; }

	/// <summary>
	/// 获取模型实例。
	/// </summary>
	internal IScopeModel Model { get; }

	/// <summary>
	/// 获取默认策略（<see cref="ScopePolicy{T}"/> 实例，T 为资源类型）。
	/// </summary>
	internal object DefaultPolicy => Model.PolicyObject;

	/// <summary>
	/// 获取指定权限码上的行级策略；未声明时返回 <see langword="null"/>。
	/// </summary>
	/// <param name="code">权限码。</param>
	/// <returns>策略；未声明时返回 <see langword="null"/>。</returns>
	internal object PolicyFor(string code)
	{
		return Model.PolicyFor(code);
	}

	/// <summary>
	/// 获取已显式声明行级策略的权限码集合。
	/// </summary>
	internal IReadOnlyCollection<string> DeclaredCodes => Model.DeclaredCodes;
}
