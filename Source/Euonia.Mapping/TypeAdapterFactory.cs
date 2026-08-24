namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 类型适配器工厂。
/// </summary>
/// <remarks>
/// 维护当前使用的 <see cref="ITypeAdapterFactory"/>，并据此创建类型适配器。
/// </remarks>
public static class TypeAdapterFactory
{
	#region Members

	private static ITypeAdapterFactory _factory;

	#endregion

	#region Public Static Methods

	/// <summary>
	/// 设置当前使用的类型适配器工厂。
	/// </summary>
	/// <param name="adapterFactory">要设置的适配器工厂。</param>
	public static void SetCurrent(ITypeAdapterFactory adapterFactory)
	{
		_factory = adapterFactory;
	}

	/// <summary>
	/// 从当前工厂创建一个新的类型适配器。
	/// </summary>
	/// <returns>所创建的类型适配器；若尚未通过 <see cref="SetCurrent"/> 配置工厂，则返回 <see langword="null"/>。</returns>
	public static ITypeAdapter CreateAdapter()
	{
		return _factory?.Create();
	}

	#endregion
}