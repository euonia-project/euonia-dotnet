namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 一个资源类型的权限模型注册项：模型描述与访问策略。
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
	/// <param name="policy">策略（<see cref="ScopePolicy{T}"/> 实例，T 为资源类型）。</param>
	internal ScopeModelRegistration(ScopeModelDescriptor descriptor, object policy)
	{
		Descriptor = descriptor;
		Policy = policy;
	}

	/// <summary>
	/// 获取模型描述。
	/// </summary>
	public ScopeModelDescriptor Descriptor { get; }

	/// <summary>
	/// 获取策略。
	/// </summary>
	internal object Policy { get; }
}
