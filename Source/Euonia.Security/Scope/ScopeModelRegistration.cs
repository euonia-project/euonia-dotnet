namespace Nerosoft.Euonia.Security;

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
	/// 模型描述：资源类型、维度映射与分类属性，由 <see cref="ScopeModelDescriptor.Create"/> 从模型实例构建。
	/// </summary>
	public ScopeModelDescriptor Descriptor { get; }

	/// <summary>
	/// 模型实例：默认策略与按权限码声明的行级策略都由它提供，本注册项只做类型擦除后的转发。
	/// </summary>
	internal IScopeModel Model { get; }

	/// <summary>
	/// 默认策略：未单独声明行级策略的权限码都回落到它；实际类型是 <see cref="ScopePolicy{T}"/>（T 为资源类型），
	/// 因类型参数在运行期才确定而以 <see cref="object"/> 持有，由 <see cref="ScopeGuard"/> 解析时完成强类型转换。
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
	/// 已显式声明行级策略的权限码；<see cref="PolicyFor"/> 对集合外的码一律返回 <see langword="null"/>，调用方回落到默认策略。
	/// </summary>
	internal IReadOnlyCollection<string> DeclaredCodes => Model.DeclaredCodes;
}
