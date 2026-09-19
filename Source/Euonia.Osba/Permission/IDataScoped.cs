namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 定义受数据权限约束的数据行必须声明的归属信息。
/// </summary>
/// <remarks>
/// <para>
/// 维度由使用方自行定义，框架不预设任何维度或值（例如 region、team、family、organization 等
/// 均可作为维度名，值通常为数据库标识）。
/// </para>
/// <para>
/// 行的范围值直接来自该行自身的数据列（例如 <c>new ScopeTag("team", TeamId)</c>），
/// 是运行期数据而非固化标签：行数据一变，其归属范围立即变化。
/// 用户的授权范围值则由 <see cref="IUserScopeProvider"/> 从应用数据实时解析。
/// </para>
/// <para>
/// 实现本接口的数据行默认不对匿名用户开放；确需匿名访问的场景（注册、密码重置等），
/// 额外实现 <see cref="IAnonymousAccessible"/> 显式声明。
/// </para>
/// </remarks>
public interface IDataScoped
{
	/// <summary>
	/// 获取数据所有者的用户标识，用于"仅本人"的快速判断。
	/// 可为 <see langword="null"/> 或空字符串，表示无所有者约束。
	/// </summary>
	string OwnerId { get; }

	/// <summary>
	/// 获取该数据行所属的全部范围标签（值来自行自身的数据列）。
	/// </summary>
	/// <remarks>
	/// 空集合表示无维度约束。跨维度按交集（AND）判定：每个维度都必须满足；
	/// 同一维度内的多个标签按并集（OR）判定：满足其中任意一个即可。
	/// </remarks>
	IReadOnlyList<ScopeTag> ScopeTags { get; }
}