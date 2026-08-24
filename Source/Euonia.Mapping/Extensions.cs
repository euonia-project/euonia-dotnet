using Nerosoft.Euonia.Mapping;

/// <summary>
/// 提供将对象投影（映射）到目标类型的扩展方法。
/// </summary>
/// <remarks>
/// 这些方法所使用的适配器由当前的 <see cref="TypeAdapterFactory"/> 创建，
/// 调用前需先通过 <see cref="TypeAdapterFactory.SetCurrent"/> 配置工厂。
/// </remarks>
// ReSharper disable once UnusedType.Global
#pragma warning disable CA1050
public static class Extensions
{
	/// <summary>
	/// 将 <paramref name="source"/> 投影为 <typeparamref name="TDestination"/> 类型的新实例。
	/// </summary>
	/// <typeparam name="TDestination">目标投影的类型。</typeparam>
	/// <param name="source">要投影的源对象。</param>
	/// <returns>根据 <paramref name="source"/> 创建的 <typeparamref name="TDestination"/> 实例。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
	public static TDestination ProjectedAs<TDestination>(this object source)
		where TDestination : class
	{
		var adapter = TypeAdapterFactory.CreateAdapter();
		return adapter.Adapt<TDestination>(source);
	}

	/// <summary>
	/// 将 <paramref name="source"/> 投影为 <paramref name="destinationType"/> 所表示类型的新实例。
	/// </summary>
	/// <param name="source">要投影的源对象。</param>
	/// <param name="destinationType">目标类型。</param>
	/// <returns>根据 <paramref name="source"/> 创建的目标类型实例。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
	public static object ProjectedAs(this object source, Type destinationType)
	{
		var adapter = TypeAdapterFactory.CreateAdapter();
		return adapter.Adapt(source, destinationType);
	}

	/// <summary>
	/// 将 <paramref name="items"/> 中的每一项投影为 <typeparamref name="TDestination"/> 类型的新实例。
	/// </summary>
	/// <typeparam name="TDestination">目标投影的类型。</typeparam>
	/// <param name="items">要投影的对象集合。</param>
	/// <returns>根据 <paramref name="items"/> 创建的 <typeparamref name="TDestination"/> 实例列表。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="items"/> 为 <see langword="null"/>。</exception>
	public static List<TDestination> ProjectedAsCollection<TDestination>(this IEnumerable<object> items)
		where TDestination : class
	{
		var adapter = TypeAdapterFactory.CreateAdapter();
		return adapter.Adapt<List<TDestination>>(items);
	}
}

#pragma warning restore CA1050