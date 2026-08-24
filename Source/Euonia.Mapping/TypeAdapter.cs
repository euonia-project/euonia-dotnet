namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 提供将对象投影（映射）到目标类型的静态辅助方法。
/// </summary>
/// <remarks>
/// 这些方法所使用的适配器由当前的 <see cref="TypeAdapterFactory"/> 创建，
/// 调用前需先通过 <see cref="TypeAdapterFactory.SetCurrent"/> 配置工厂。
/// </remarks>
public class TypeAdapter
{
	/// <summary>
	/// 将 <paramref name="source"/> 投影为 <typeparamref name="TDestination"/> 类型的新实例。
	/// </summary>
	/// <typeparam name="TSource">源项的类型。</typeparam>
	/// <typeparam name="TDestination">目标项的类型。</typeparam>
	/// <param name="source">要投影的实例。</param>
	/// <returns>根据 <paramref name="source"/> 创建的 <typeparamref name="TDestination"/> 新实例。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
	public static TDestination ProjectedAs<TSource, TDestination>(TSource source)
		where TSource : class
		where TDestination : class
	{
		var adapter = TypeAdapterFactory.CreateAdapter();
		return adapter.Adapt<TSource, TDestination>(source);
	}

	/// <summary>
	/// 将 <paramref name="source"/> 投影到已有的 <paramref name="destination"/> 实例上。
	/// </summary>
	/// <typeparam name="TSource">源项的类型。</typeparam>
	/// <typeparam name="TDestination">目标项的类型。</typeparam>
	/// <param name="source">要投影的实例。</param>
	/// <param name="destination">待填充的目标实例。</param>
	/// <returns>填充完成后的 <paramref name="destination"/> 实例。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="source"/> 为 <see langword="null"/>。</exception>
	public static TDestination ProjectedAs<TSource, TDestination>(TSource source, TDestination destination)
		where TSource : class
		where TDestination : class
	{
		var adapter = TypeAdapterFactory.CreateAdapter();
		return adapter.Adapt(source, destination);
	}

	/// <summary>
	/// 将 <paramref name="item"/> 投影为 <typeparamref name="TDestination"/> 类型的新实例。
	/// </summary>
	/// <typeparam name="TDestination">目标项的类型。</typeparam>
	/// <param name="item">要投影的对象。</param>
	/// <returns>根据 <paramref name="item"/> 创建的 <typeparamref name="TDestination"/> 新实例。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="item"/> 为 <see langword="null"/>。</exception>
	public static TDestination ProjectedAs<TDestination>(object item)
		where TDestination : class
	{
		var adapter = TypeAdapterFactory.CreateAdapter();
		return adapter.Adapt<TDestination>(item);
	}

	/// <summary>
	/// 将 <paramref name="items"/> 中的每一项投影为 <typeparamref name="TDestination"/> 类型的新实例。
	/// </summary>
	/// <typeparam name="TDestination">目标项的类型。</typeparam>
	/// <param name="items">要投影的对象集合。</param>
	/// <returns>根据 <paramref name="items"/> 创建的 <typeparamref name="TDestination"/> 实例列表。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="items"/> 为 <see langword="null"/>。</exception>
	public static List<TDestination> ProjectedAsCollection<TDestination>(IEnumerable<object> items)
		where TDestination : class
	{
		var adapter = TypeAdapterFactory.CreateAdapter();
		return adapter.Adapt<List<TDestination>>(items);
	}
}