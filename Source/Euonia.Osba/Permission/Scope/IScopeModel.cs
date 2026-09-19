namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据权限模型的非泛型视图，用于程序集扫描与启动期校验。
/// </summary>
/// <remarks>
/// 使用方通常派生 <see cref="ScopeModel{T}"/>，而不是直接实现本接口。
/// </remarks>
public interface IScopeModel
{
	/// <summary>
	/// 获取本模型所描述的资源类型。
	/// </summary>
	Type ResourceType { get; }

	/// <summary>
	/// 定义资源在各维度上的取值来源，以及不参与授权的分类属性。
	/// </summary>
	/// <param name="builder">模型构建器。</param>
	void Define(IScopeModelBuilder builder);

	/// <summary>
	/// 获取访问策略（实际类型为 <see cref="ScopePolicy{T}"/>，T 为 <see cref="ResourceType"/>）。
	/// </summary>
	object PolicyObject { get; }
}

/// <summary>
/// 描述一种资源在数据权限中的形态：它在各维度上的取值，以及它的访问策略。
/// </summary>
/// <typeparam name="T">资源类型。</typeparam>
/// <remarks>
/// <para>
/// <b>模型与策略写在同一个类型里</b>，从而结构上不可能出现「声明了模型却忘了写策略」的情况——
/// 缺少任何一个都无法完成本接口的实现。
/// </para>
/// <para>
/// 实现应派生 <see cref="ScopeModel{T}"/>。
/// </para>
/// </remarks>
public interface IScopeModel<T> : IScopeModel
	where T : class
{
	/// <summary>
	/// 获取本资源的访问策略。
	/// </summary>
	ScopePolicy<T> Policy { get; }
}
