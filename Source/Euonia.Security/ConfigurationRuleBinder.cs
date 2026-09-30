using System.Reflection;
using Microsoft.Extensions.Configuration;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 把配置节里的「操作入口规则」读成 <see cref="OperationCodeSourceBuilder"/> 的规则。
/// </summary>
/// <remarks>
/// <para>
/// 配置只是规则的一种<b>载体</b>：读出来的规则与代码里声明的规则走同一套编译、同一份启动期校验，
/// 不存在第二条判定路径。因此本类只做「读 + 解析 + 报错」，不引入任何新语义。
/// </para>
/// <para>
/// <b>读到的每一个字符都要么生效、要么报错</b>：配置写错时的危险方向是「规则比作者以为的更窄」——
/// 少一条入口规则意味着对应方法上的 <c>[Permission]</c> 不被收集，闸门悄悄落到默认键上。
/// 因此标量写成数组、空字符串、节点名拼错、类型名解析不了或有歧义，一律在注册期失败。
/// </para>
/// </remarks>
internal static class ConfigurationRuleBinder
{
	/// <summary>操作定义表所在的节点名。</summary>
	internal const string OperationsKey = "Operations";

	/// <summary>入口特性类型列表所在的节点名。</summary>
	internal const string AttributesKey = "Attributes";

	/// <summary>入口方法名列表所在的节点名。</summary>
	internal const string NamesKey = "Names";

	/// <summary>
	/// 读取配置节并写入规则。
	/// </summary>
	/// <param name="configuration">规则所在的配置节（其下应有 <see cref="OperationsKey"/> 节点）。</param>
	/// <param name="builder">规则构造器。</param>
	/// <param name="assemblies">已知的扫描范围，用于解析类型名。</param>
	/// <exception cref="InvalidOperationException">配置缺节点、含未知节点、操作无规则、类型名不可用时抛出。</exception>
	internal static void Bind(IConfigurationSection configuration, OperationCodeSourceBuilder builder, IReadOnlyCollection<Assembly> assemblies)
	{
		var operations = configuration.GetSection(OperationsKey);

		Check.Ensure(
			operations.Exists(),
			Resources.IDS_CONFIG_OPERATIONS_NODE_MISSING,
			OperationsKey,
			configuration.Path);

		var declared = 0;
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var operation in operations.GetChildren())
		{
			// 本表刻意用 OrdinalIgnoreCase：它要收拢的是<b>写法差异</b>。
			// 下游操作表按 Ordinal 比较（OperationCodeSource._rules），「read」与「Read」会成为两个不同操作，
			// 预期的那一个会静默失去规则，故在这里直接拒绝仅大小写不同的重复声明。
			Check.Ensure(
				seen.Add(operation.Key),
				Resources.IDS_CONFIG_OPERATION_DUPLICATE_CASE,
				operation.Key);

			RejectUnknownKeys(operation);

			var attributes = ReadValues(operation, AttributesKey)
			                 .Select(name => ResolveAttribute(name, operation, assemblies))
			                 .ToArray();

			var names = ReadValues(operation, NamesKey);

			Check.Ensure(
				attributes.Length > 0 || names.Length > 0,
				Resources.IDS_CONFIG_OPERATION_NO_RULES,
				operation.Path,
				AttributesKey,
				NamesKey);

			// 两者并存即「特性或命名」的或语义：任何一条命中都算该操作的入口
			if (attributes.Length > 0)
			{
				builder.OnAttribute(operation.Key, attributes);
			}

			if (names.Length > 0)
			{
				builder.OnMethodName(operation.Key, names);
			}

			declared++;
		}

		Check.Ensure(
			declared > 0,
			Resources.IDS_CONFIG_NO_OPERATIONS,
			operations.Path);
	}

	/// <summary>拒绝操作节点下的未知子节点（拼错的键名会让整条规则静默丢失）。</summary>
	private static void RejectUnknownKeys(IConfigurationSection operation)
	{
		foreach (var child in operation.GetChildren())
		{
			Check.Ensure(
				child.Key is AttributesKey or NamesKey,
				Resources.IDS_CONFIG_UNKNOWN_NODE,
				operation.Path,
				child.Key,
				AttributesKey,
				NamesKey);
		}
	}

	/// <summary>读取某个键下的全部取值；标量写法与空项都直接报错。</summary>
	/// <remarks>
	/// 静默跳过是最坏的处置：漏掉一条规则会让对应方法的权限声明不再被收集，
	/// 表现为「闸门比配置写的更宽松」，而且没有任何迹象。
	/// </remarks>
	private static string[] ReadValues(IConfigurationSection operation, string key)
	{
		var node = operation.GetSection(key);

		Check.Ensure(
			node.Value == null,
			Resources.IDS_CONFIG_VALUE_MUST_BE_ARRAY,
			node.Path,
			key);

		var values = new List<string>();

		foreach (var item in node.GetChildren())
		{
			Check.Ensure(
				!string.IsNullOrWhiteSpace(item.Value),
				Resources.IDS_CONFIG_VALUE_EMPTY,
				item.Path);

			values.Add(item.Value);
		}

		return [.. values.Distinct(StringComparer.Ordinal)];
	}

	/// <summary>
	/// 把配置里的类型名解析为入口特性类型。
	/// </summary>
	/// <remarks>
	/// 两种写法路由到两条互不重叠的路径，避免「同一个名字有时按扫描范围解析、有时按调用方程序集解析」：
	/// 带逗号的程序集限定名交给运行时（显式逃生舱），其余一律**只在扫描范围内**解析——
	/// 配置里的名字不应悄悄绑定到框架自己的类型上。
	/// </remarks>
	private static Type ResolveAttribute(string name, IConfigurationSection operation, IReadOnlyCollection<Assembly> assemblies)
	{
		var resolved = name.Contains(',', StringComparison.Ordinal)
			? ResolveAssemblyQualified(name, operation)
			: ResolveInScope(name, operation, assemblies);

		Check.Ensure(
			resolved != null,
			Resources.IDS_CONFIG_TYPE_UNRESOLVED,
			name,
			operation.GetSection(AttributesKey).Path,
			NamesKey);

		Check.Ensure(
			!resolved.IsGenericType,
			Resources.IDS_CONFIG_TYPE_GENERIC,
			name);

		Check.Ensure(
			typeof(Attribute).IsAssignableFrom(resolved),
			Resources.IDS_CONFIG_TYPE_NOT_ATTRIBUTE,
			name);

		return resolved;
	}

	/// <summary>解析程序集限定名（<c>"类型, 程序集"</c>）；解析过程中的任何失败都转成注册期错误。</summary>
	private static Type ResolveAssemblyQualified(string name, IConfigurationSection operation)
	{
		try
		{
			return Type.GetType(name, throwOnError: false);
		}
		catch (Exception exception) when (exception is ArgumentException or FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException)
		{
			// Type.GetType 对格式错误 / 加载失败的限定名抛的是框架异常，不能让它盖过这里的报错口径
			throw new InvalidOperationException(
				string.Format(Resources.IDS_CONFIG_TYPE_LOAD_FAILED, name, operation.GetSection(AttributesKey).Path, exception.Message),
				exception);
		}
	}

	/// <summary>在扫描范围内解析类型名：完整名优先，短名仅在唯一时兜底。</summary>
	private static Type ResolveInScope(string name, IConfigurationSection operation, IReadOnlyCollection<Assembly> assemblies)
	{
		var types = AssemblyHelper.LoadTypes(assemblies);
		var path = operation.GetSection(AttributesKey).Path;

		var exact = types.Where(type => type.FullName == name).ToArray();

		Check.Ensure(
			exact.Length <= 1,
			Resources.IDS_CONFIG_TYPE_AMBIGUOUS,
			name,
			path,
			Describe(exact));

		if (exact.Length == 1)
		{
			return exact[0];
		}

		var byShortName = types.Where(type => type.Name == name).ToArray();

		Check.Ensure(
			byShortName.Length <= 1,
			Resources.IDS_CONFIG_TYPE_SHORT_NAME_AMBIGUOUS,
			name,
			path,
			Describe(byShortName));

		return byShortName.FirstOrDefault();
	}

	private static string Describe(IEnumerable<Type> types)
	{
		return string.Join(", ", types.Select(type => $"{type.FullName}（{type.Assembly.GetName().Name}）"));
	}
}
