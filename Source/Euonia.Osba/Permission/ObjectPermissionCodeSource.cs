using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 权限码来源：把「哪个方法是哪个操作的入口」这条<b>对象模型知识</b>交给
/// <see cref="OperationCodeSource"/>，而扫描本身由权限库完成。
/// </summary>
/// <remarks>
/// <para>
/// 这是「对象模型 → 权限」这条依赖方向的唯一落点，Osba 侧只剩<b>一张对照表</b>
/// （操作名 → 工厂方法特性），没有反射扫描代码。
/// </para>
/// <para>
/// 扫描口径必须与工厂方法查找（<c>ObjectReflector.IsFactoryMethod</c>）完全一致：
/// 「特性 <b>或</b> 约定名」都算入口，否则以命名约定声明的工厂方法上的
/// <see cref="PermissionAttribute"/> 会被静默忽略，权限形同虚设。
/// 这里用 <see cref="OperationCodeSourceBuilder.OnAttributeOrName(string, string, Type[])"/>
/// 一次性表达这条规则，正是为了让两个口径不可能分叉。
/// </para>
/// </remarks>
internal sealed class ObjectPermissionCodeSource : IPermissionCodeSource
{
	/// <summary>工厂方法特性的公共前缀，参与约定名推导时需要剥离。</summary>
	private const string FactoryPrefix = "Factory";

	internal static ObjectPermissionCodeSource Instance { get; } = new();

	private readonly OperationCodeSource _source = Build();

	private static OperationCodeSource Build()
	{
		return OperationCodeSource.Create()
		                            .OnAttributeOrName(BusinessOperation.Read, FactoryPrefix, typeof(FactoryFetchAttribute))
		                            .OnAttributeOrName(BusinessOperation.Create, FactoryPrefix, typeof(FactoryCreateAttribute), typeof(FactoryInsertAttribute))
		                            .OnAttributeOrName(BusinessOperation.Update, FactoryPrefix, typeof(FactoryUpdateAttribute))
		                            .OnAttributeOrName(BusinessOperation.Delete, FactoryPrefix, typeof(FactoryDeleteAttribute))
		                            .OnAttributeOrName(BusinessOperation.Execute, FactoryPrefix, typeof(FactoryExecuteAttribute))
		                            .Build();
	}

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation) => _source.CodesFor(type, operation);

	/// <inheritdoc />
	public IReadOnlyList<string> AllOperations => _source.AllOperations;

	/// <summary>
	/// 收集类型级与执行指定操作的工厂方法上的权限要求（含角色等原始信息）。
	/// </summary>
	/// <param name="type">业务对象类型。</param>
	/// <param name="operation">业务操作名。</param>
	/// <returns>权限要求列表；结果按（类型，操作）缓存。</returns>
	/// <remarks>
	/// 运行期判定需要 <see cref="PermissionAttribute.Roles"/>，因此不能用只给权限码的
	/// <see cref="IPermissionCodeSource.CodesFor"/>。本方法与 <see cref="CodesFor"/> 共用同一个来源实例，
	/// 确保启动期校验与运行期判定对「某操作声明了哪些权限码」不会得出不同答案。
	/// </remarks>
	internal static IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
		=> Instance._source.RequirementsFor(type, operation);
}
