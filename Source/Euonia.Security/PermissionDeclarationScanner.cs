using System.Collections.Concurrent;
using System.Reflection;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 启动期声明扫描：一次读完被扫描程序集里的 <see cref="PermissionAttribute"/>，并报告三类违规声明。
/// </summary>
/// <remarks>
/// <para>
/// 扫描对象是<b>声明</b>：遍历每个类型与方法上的 <see cref="PermissionAttribute"/>，
/// 因此新增权限码或新增操作都不需要修改配置。
/// </para>
/// <para>
/// 结果按程序集缓存（<see cref="ConcurrentDictionary{TKey,TValue}"/>）：同一程序集整进程只反射一次，
/// 反复调用 <c>AddPermission</c> 是零成本。诊断一次报全（逐行拼接），不是修一个再跑一次。
/// </para>
/// <para>
/// <b>不</b>报错的情形：同一权限码出现在不同成员或不同类型上（常态，例如 <c>order:read</c> 出现在多个聚合上）；
/// 同一成员上同码但角色不同（<c>[Permission("x", "a")]</c> 与 <c>[Permission("x", "b")]</c> 是两个合法要求）。
/// </para>
/// </remarks>
internal static class PermissionDeclarationScanner
{
	/// <summary>
	/// 方法可见性范围：只读当前类型自己声明的方法（基类的声明由基类自己负责，避免重复诊断）。
	/// </summary>
	private const BindingFlags DeclaredMethodFlags =
		BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

	/// <summary>
	/// 单个程序集的扫描结果缓存。
	/// </summary>
	private static readonly ConcurrentDictionary<Assembly, DeclarationSet> Cache = new();

	/// <summary>
	/// 判断给定程序集中是否存在任何 <see cref="PermissionAttribute"/> 声明（类型级或方法级）。
	/// </summary>
	/// <param name="assemblies">要检查的程序集。</param>
	/// <returns>存在声明则返回 <see langword="true"/>。</returns>
	internal static bool HasDeclarations(IEnumerable<Assembly> assemblies)
	{
		foreach (var assembly in assemblies)
		{
			if (assembly != null && Load(assembly).HasDeclarations)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 校验给定程序集中的权限声明；存在违规时，把全部诊断逐行拼接后一次性抛出。
	/// </summary>
	/// <param name="assemblies">要校验的程序集。</param>
	/// <exception cref="InvalidOperationException">存在空码、保留前缀或同成员重复码时抛出。</exception>
	internal static void EnsureValid(IEnumerable<Assembly> assemblies)
	{
		var diagnostics = new List<string>();

		foreach (var assembly in assemblies)
		{
			if (assembly != null)
			{
				diagnostics.AddRange(Load(assembly).Diagnostics);
			}
		}

		if (diagnostics.Count == 0)
		{
			return;
		}

		// 每行本身已本地化：直接逐行拼接，一次报全
		throw new InvalidOperationException(string.Join(Environment.NewLine, diagnostics));
	}

	/// <summary>
	/// 读取（或缓存命中）单个程序集的扫描结果。
	/// </summary>
	/// <param name="assembly">要扫描的程序集。</param>
	/// <returns>扫描结果。</returns>
	private static DeclarationSet Load(Assembly assembly)
	{
		return Cache.GetOrAdd(assembly, Scan);
	}

	/// <summary>
	/// 扫描单个程序集：逐类型读类型级声明、逐方法读方法级声明，并收集全部诊断。
	/// </summary>
	/// <param name="assembly">要扫描的程序集。</param>
	/// <returns>扫描结果。</returns>
	/// <remarks>工厂必须是纯反射（可能被并发执行两次），不得有副作用。</remarks>
	private static DeclarationSet Scan(Assembly assembly)
	{
		var diagnostics = new List<string>();
		var hasDeclarations = false;

		foreach (var type in AssemblyHelper.LoadTypes(assembly))
		{
			var typeDeclarations = type.GetCustomAttributes<PermissionAttribute>(false).ToArray();

			if (typeDeclarations.Length > 0)
			{
				hasDeclarations = true;
				Validate(diagnostics, type.FullName ?? type.Name, typeDeclarations);
			}

			foreach (var method in type.GetMethods(DeclaredMethodFlags))
			{
				var declarations = method.GetCustomAttributes<PermissionAttribute>(false).ToArray();

				if (declarations.Length == 0)
				{
					continue;
				}

				hasDeclarations = true;
				Validate(diagnostics, $"{type.FullName ?? type.Name}.{method.Name}", declarations);
			}
		}

		return new DeclarationSet(hasDeclarations, [.. diagnostics]);
	}

	/// <summary>
	/// 校验同一成员上的全部声明：空权限码、保留前缀、同成员重复码。
	/// </summary>
	/// <param name="diagnostics">诊断收集器。</param>
	/// <param name="member">成员显示名（类型全名，或「类型全名.方法名」）。</param>
	/// <param name="declarations">该成员上声明的全部 <see cref="PermissionAttribute"/>。</param>
	private static void Validate(List<string> diagnostics, string member, PermissionAttribute[] declarations)
	{
		// 去重键与判定口径一致：码忽略大小写、角色按序数；同码不同角色是合法要求，不报重复
		var seen = new HashSet<string>(StringComparer.Ordinal);

		foreach (var declaration in declarations)
		{
			var code = declaration.Permission?.Trim();

			if (string.IsNullOrWhiteSpace(code))
			{
				// 只说角色是合法的；什么都没有才是复制粘贴留下的空声明
				if (declaration.Roles.Length == 0)
				{
					diagnostics.Add(string.Format(Resources.IDS_PERMISSION_CODE_EMPTY, member));
				}

				continue;
			}

			try
			{
				// 唯一的权限码校验器：与数据权限共用同一套规则与文案（空码已在上面先行分类）
				ScopeKeys.Validate(code);
			}
			catch (ArgumentException exception)
			{
				diagnostics.Add(exception.Message);
				continue;
			}
			catch (InvalidOperationException exception)
			{
				diagnostics.Add(exception.Message);
				continue;
			}

			if (!seen.Add($"{code.ToUpperInvariant()}\u001f{string.Join("\u001e", declaration.Roles)}"))
			{
				diagnostics.Add(string.Format(Resources.IDS_PERMISSION_CODE_DUPLICATE, member, code));
			}
		}
	}

	/// <summary>
	/// 单个程序集的扫描结果。
	/// </summary>
	/// <param name="HasDeclarations">是否存在任何声明。</param>
	/// <param name="Diagnostics">全部违规诊断（每行一条，已本地化）。</param>
	private sealed record DeclarationSet(bool HasDeclarations, IReadOnlyList<string> Diagnostics);
}
