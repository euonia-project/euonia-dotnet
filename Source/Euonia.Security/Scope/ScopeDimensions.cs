namespace Nerosoft.Euonia.Security;

/// <summary>
/// 定义数据权限中常用的维度名，并提供维度名的校验入口。
/// </summary>
/// <remarks>
/// <para>
/// 维度是<em>代码概念</em>：应当预定义，而不是由运行期数据产生。本类给出常用维度常量，
/// 使用方也可以定义自己的维度名（建议经 <see cref="Register"/> 校验）。
/// </para>
/// <para>
/// 维度名在比较时<b>大小写不敏感</b>；维度值是不透明标识（通常是数据库标识），
/// 按<b>大小写敏感</b>精确比较。框架不对值的格式做任何假设，
/// 也<b>不使用 <c>"*"</c> 之类的保留值</b>——「全部」应当由策略显式表达。
/// </para>
/// </remarks>
public static class ScopeDimensions
{
	/// <summary>
	/// 所有者维度：值为所属用户的标识。
	/// </summary>
	/// <remarks>
	/// 本维度并不特殊——是否拥有「本人的数据」取决于 <see cref="IScopeSubjectResolver"/>
	/// 是否在解析结果中包含本维度，因此可以撤销。
	/// </remarks>
	public const string Owner = "owner";

	/// <summary>
	/// 部门（组织）维度。层级关系应在解析期展开为扁平集合。
	/// </summary>
	public const string Dept = "dept";

	/// <summary>
	/// 团队维度：值为所属团队的标识。
	/// </summary>
	public const string Team = "team";

	/// <summary>
	/// 成员（参与者）维度：值为该资源的<b>成员标识</b>。
	/// </summary>
	/// <remarks>
	/// 与其余维度不同，本维度的取值通常不在资源行上，而在<b>子表</b>（成员表/关系表）里，
	/// 因此用 <see cref="ScopeModelBuilder{T}.MapMany"/> 声明为集合维度：
	/// 「我加入了哪些团队」即 <c>Grant(Member)</c> + 解析器授予本人的用户标识，
	/// 下推为 <c>EXISTS</c> 子查询。同一个用户在团队上的角色（管理员/普通成员）若也要参与判定，
	/// 写在选择器里（<c>x =&gt; x.Members.Where(m =&gt; m.Role == "admin").Select(m =&gt; m.UserId)</c>），
	/// 或另立一个维度。
	/// </remarks>
	public const string Member = "member";

	/// <summary>
	/// 区域维度。
	/// </summary>
	public const string Region = "region";

	/// <summary>
	/// 项目维度。
	/// </summary>
	public const string Project = "project";

	/// <summary>
	/// 校验并返回一个维度名。
	/// </summary>
	/// <param name="name">维度名。</param>
	/// <returns>校验通过的维度名。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="name"/> 为 <see langword="null"/>、空或仅由空白字符组成时抛出。</exception>
	public static string Register(string name)
	{
		return Check.EnsureNotNullOrWhiteSpace(name, nameof(name));
	}
}
