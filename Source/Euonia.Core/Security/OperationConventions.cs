using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 从特性名推导方法命名约定——各框架「用哪个方法名表示某个操作」的通用做法。
/// </summary>
/// <remarks>
/// <para>
/// 绝大多数框架的「操作入口」有两条并行约定：打特性或按名字匹配；本类把后者做成可复用的推导（见 README §3.3）。
/// </para>
/// <para>
/// <b>本类型位于 <c>Euonia.Core</c> 程序集</b>：宿主框架的工厂方法查找（「调用哪个方法」）与
/// 鉴权侧的入口规则声明（「看哪些方法上的权限声明」）必须用同一套推导——两处各写一份，
/// 一旦分叉就会出现「声明了权限却从不生效」的静默缺口。
/// </para>
/// <para>
/// 推导规则：以 <c>FetchAttribute</c>、可选前缀 <c>Factory</c> 为例，得到 <c>Fetch</c>、<c>FetchAsync</c>、
/// <c>FactoryFetch</c>、<c>FactoryFetchAsync</c>；许多框架的约定不含前缀。
/// </para>
/// <para>
/// 名字匹配<b>大小写敏感</b>，不做任何模糊化——拼写不符即不识别，宁可漏认（启动期可见）也不误认。
/// </para>
/// </remarks>
public static class OperationConventions
{
	private static readonly ConcurrentDictionary<(Type AttributeType, string Prefix), string[]> _cache = new();

	/// <summary>
	/// 获取指定特性类型对应的约定方法名。
	/// </summary>
	/// <param name="attributeType">操作入口特性类型。</param>
	/// <param name="prefix">特性名中需要剥离的前缀（例如 <c>Factory</c>）；为 <see langword="null"/> 或空时不去前缀。</param>
	/// <returns>约定方法名数组；已去重且保序。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="attributeType"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <remarks>
	/// 剥离 <c>Attribute</c> 后缀是必须的：特性名本身带该后缀，
	/// 不剥离会得到 <c>FactoryFactoryUpdate</c> 这类永不匹配的名称，
	/// 使「短名」那一半约定形同虚设。
	/// </remarks>
	public static string[] Names(Type attributeType, string prefix = null)
	{
		// netstandard2.1 没有 ArgumentNullException.ThrowIfNull
		ArgumentAssert.ThrowIfNull(attributeType, nameof(attributeType));

		return _cache.GetOrAdd((attributeType, prefix ?? string.Empty), static key =>
		{
			var (type, prefix) = key;

			var name = type.Name.EndsWith(nameof(Attribute), StringComparison.Ordinal)
				? type.Name[..^nameof(Attribute).Length]
				: type.Name;

			var operation = prefix.Length > 0
			                && name.StartsWith(prefix, StringComparison.Ordinal)
			                && name.Length > prefix.Length
				? name[prefix.Length..]
				: name;

			return operation == name
				? [name, name + "Async"]
				: [operation, operation + "Async", name, name + "Async"];
		});
	}
}
