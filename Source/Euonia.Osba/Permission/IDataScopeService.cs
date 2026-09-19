namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 提供基于当前用户的数据范围判定与过滤能力（数据权限）。
/// </summary>
/// <remarks>
/// <para>
/// 数据权限回答"当前用户能看到哪些数据行"，作用于数据访问层：
/// 对任意实现 <see cref="IDataScoped"/> 的数据行，根据行声明的范围标签
/// 与当前用户从授权数据实时解析出来的范围进行计算；越权行应当被排除，而非抛出异常。
/// 用户的授权范围值来源为 <see cref="IUserScopeProvider"/>，必须在判定时从应用数据解析——
/// 授权关系随时可能调整，值不能固化在声明、Token 或代码里。
/// </para>
/// </remarks>
public interface IDataScopeService
{
	/// <summary>
	/// 判断当前用户是否被授予指定的单个标签。
	/// </summary>
	/// <param name="required">要求的范围标签，值通常为数据库标识；值为 "*" 时匹配该维度任意值。</param>
	/// <returns>被授予则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool IsGranted(ScopeTag required);

	/// <summary>
	/// 判断当前用户是否可访问指定的数据行。
	/// </summary>
	/// <param name="resource">待判定的数据行，不能为 <see langword="null"/>。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool CanAccess(IDataScoped resource);

	/// <summary>
	/// 解析当前用户此刻被授予的全部范围标签。
	/// </summary>
	/// <returns>当前用户的范围标签序列；未认证或未授予任何范围时返回空序列。</returns>
	IReadOnlyList<ScopeTag> ResolveScopes();

	/// <summary>
	/// 创建用于过滤数据行是否为当前用户可访问范围的谓词，供查询层直接用于 <c>Where</c>。
	/// </summary>
	/// <typeparam name="T">数据项的类型。</typeparam>
	/// <returns>当前用户的数据范围谓词。</returns>
	Func<T, bool> CreateScopePredicate<T>()
		where T : IDataScoped;

	/// <summary>
	/// 从数据源中过滤出当前用户可访问的数据项。
	/// </summary>
	/// <typeparam name="T">数据项的类型。</typeparam>
	/// <param name="source">数据源。</param>
	/// <returns>仅包含可访问数据项的延迟执行序列。</returns>
	IEnumerable<T> Filter<T>(IEnumerable<T> source)
		where T : IDataScoped;
}