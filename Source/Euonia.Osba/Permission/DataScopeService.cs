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
		return IsGrantedCore(required, ResolveScopes());
	}

	/// <inheritdoc />
	public bool CanAccess(IDataScoped resource)
	{
		if (resource == null)
		{
			return false;
		}

		var user = _context.User;
		if (user == null || !user.IsAuthenticated)
		{
			return true;
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

		var scopes = ResolveScopes();
		return tags.GroupBy(tag => tag.Dimension)
		           .All(group => group.Any(tag => IsGrantedCore(tag, scopes)));
	}

	/// <inheritdoc />
	public Func<T, bool> CreateScopePredicate<T>()
		where T : IDataScoped
	{
		return item => CanAccess(item);
	}

	/// <inheritdoc />
	public IEnumerable<T> Filter<T>(IEnumerable<T> source)
		where T : IDataScoped
	{
		ArgumentNullException.ThrowIfNull(source);
		return source.Where(CreateScopePredicate<T>());
	}

	private static bool IsGrantedCore(ScopeTag required, IReadOnlyList<ScopeTag> scopes)
	{
		if (scopes.Count == 0)
		{
			return false;
		}

		foreach (var granted in scopes)
		{
			if (Equals(granted.Dimension, "*") && Equals(granted.Value, "*"))
			{
				return true;
			}

			if (!Equals(granted.Dimension, required.Dimension))
			{
				continue;
			}

			if (Equals(granted.Value, "*") || Equals(granted.Value, required.Value))
			{
				return true;
			}
		}

		return false;
	}

	private static bool Equals(string left, string right)
	{
		return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
	}
}