using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据范围判定引擎：把"代码可预定义的部分"（维度、判定语义）与
/// "必须运行期解析的部分"（用户被授予的范围值）分开处理。
/// </summary>
/// <remarks>
/// <para>
/// 数据权限回答"当前用户此刻能看到哪些数据行"，作用于数据访问层（查询过滤），
/// 不参与业务规则的校验流水线，也不对业务对象做任何介入。
/// 匹配失败即排除该数据行，不抛异常。
/// </para>
/// <para>
/// 用户的授权范围值完全来自 <see cref="IUserScopeProvider"/> 在每次判定时从应用数据
/// 实时解析的结果：团队、资源及授权随时调整都立即生效，不固化在声明或代码里。
/// 框架不预设任何维度，也不对值做任何格式假设（值通常为数据库标识，按字符串精确匹配；
/// 值 "*" 表示该维度通配，维度与值均为 "*" 即 <see cref="ScopeTag.Any"/> 表示全局通配）。
/// </para>
/// <para>
/// 匹配语义：数据行声明的各维度按交集（AND）判定（每个维度都要被满足），
/// 同一维度内的多个标签或用户的多个值按并集（OR）判定（满足任意一个即可）。
/// 维度名按大小写不敏感比较（维度是代码约定的概念），标签值为不透明标识，按大小写敏感精确比较。
/// </para>
/// <para>
/// 用户与数据行的三种关系：
/// <list type="bullet">
/// <item><description>未接入用户上下文（<see cref="BusinessContext.User"/> 为 <c>null</c>，如后台任务）：无从判定，不做限制；</description></item>
/// <item><description>匿名用户（已接入 <see cref="UserPrincipal"/> 但未通过认证）：默认拒绝，仅放行实现了
/// <see cref="IAnonymousAccessible"/> 的数据行（注册、密码重置等场景）；</description></item>
/// <item><description>已认证用户：按所有者快速路径与范围标签判定。</description></item>
/// </list>
/// </para>
/// </remarks>
public class DataScopeService : IDataScopeService
{
	private readonly BusinessContext _context;
	private readonly IUserScopeProvider _provider;

	/// <summary>
	/// 初始化 <see cref="DataScopeService"/> 的新实例。
	/// </summary>
	/// <param name="context">当前业务上下文，用于解析当前用户。</param>
	/// <param name="provider">用户的授权范围值来源，由应用基于授权数据实现。</param>
	public DataScopeService(BusinessContext context, IUserScopeProvider provider)
	{
		_context = context;
		_provider = provider;
	}

	/// <inheritdoc />
	public IReadOnlyList<ScopeTag> ResolveScopes()
	{
		var user = _context.User;
		if (user == null || !user.IsAuthenticated)
		{
			return Array.Empty<ScopeTag>();
		}

		return _provider.ResolveScopes(user);
	}

	/// <inheritdoc />
	public bool IsGranted(ScopeTag required)
	{
		if (required == null)
		{
			return false;
		}

		return IsGrantedCore(required, ResolveScopes());
	}

	/// <inheritdoc />
	public bool CanAccess(IDataScoped resource)
	{
		if (resource == null)
		{
			return false;
		}

		return CanAccessCore(resource, _context.User, ResolveScopes());
	}

	/// <inheritdoc />
	/// <remarks>
	/// 用户范围在创建谓词时解析一次并在后续所有行上复用。谓词会被查询层对每一行调用，
	/// 若逐行解析，一次列表过滤就会对授权数据发起与行数相同次数的查询。
	/// 因此谓词持有的是创建时刻的范围快照：同一批过滤使用同一份授权数据。
	/// </remarks>
	public Func<T, bool> CreateScopePredicate<T>()
		where T : IDataScoped
	{
		var user = _context.User;
		var scopes = ResolveScopes();

		return item => CanAccessCore(item, user, scopes);
	}

	/// <inheritdoc />
	public IEnumerable<T> Filter<T>(IEnumerable<T> source)
		where T : IDataScoped
	{
		ArgumentNullException.ThrowIfNull(source);

		// CreateScopePredicate 在调用时即解析用户范围，故整个序列共用一份快照。
		return source.Where(CreateScopePredicate<T>());
	}

	/// <summary>
	/// 依据已解析的用户范围判断是否可访问指定数据行。
	/// </summary>
	/// <param name="resource">待判定的数据行。</param>
	/// <param name="user">当前用户；未接入用户上下文时为 <see langword="null"/>。</param>
	/// <param name="scopes">当前用户已解析的范围快照。</param>
	/// <returns>可访问则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	private static bool CanAccessCore(IDataScoped resource, UserPrincipal user, IReadOnlyList<ScopeTag> scopes)
	{
		if (resource == null)
		{
			return false;
		}

		if (user == null)
		{
			// 未接入用户上下文（后台任务、系统上下文等）：数据权限无从判定，不做限制。
			return true;
		}

		if (!user.IsAuthenticated)
		{
			// 已接入认证但请求未通过认证（匿名用户）：默认拒绝，
			// 仅放行显式声明可匿名访问的数据（注册、密码重置等场景）。
			return resource is IAnonymousAccessible;
		}

		var ownerId = resource.OwnerId;
		if (!string.IsNullOrWhiteSpace(ownerId)
		    && string.Equals(ownerId, user.UserId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		var tags = resource.ScopeTags;
		if (tags == null || tags.Count == 0)
		{
			// 无维度约束：仅当无所有者约束时才放行，否则只有所有者本人可访问
			return string.IsNullOrWhiteSpace(ownerId);
		}

		return tags.Where(tag => tag != null)
		           .GroupBy(tag => tag.Dimension, StringComparer.OrdinalIgnoreCase)
		           .All(group => group.Any(tag => IsGrantedCore(tag, scopes)));
	}

	private static bool IsGrantedCore(ScopeTag required, IReadOnlyList<ScopeTag> scopes)
	{
		if (required == null || scopes == null || scopes.Count == 0)
		{
			return false;
		}

		foreach (var granted in scopes)
		{
			if (granted == null)
			{
				continue;
			}

			if (IsWildcard(granted.Dimension) && IsWildcard(granted.Value))
			{
				return true;
			}

			if (!Matches(granted.Dimension, required.Dimension, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (IsWildcard(granted.Value) || Matches(granted.Value, required.Value, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsWildcard(string value)
	{
		return value == "*";
	}

	private static bool Matches(string left, string right, StringComparison comparison)
	{
		return string.Equals(left ?? string.Empty, right ?? string.Empty, comparison);
	}
}