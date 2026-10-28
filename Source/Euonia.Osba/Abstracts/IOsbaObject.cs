namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 所有 Osba 对象的公共契约：字段数据、属性读取/装载，以及业务上下文。
/// </summary>
/// <remarks>
/// 这是 <see cref="IBusinessObject"/> 与 <see cref="ICommandObject"/> 的公共部分：
/// 它只承诺“有属性和上下文”，<b>不承诺</b>变更通知、变更追踪或可保存生命周期。
/// </remarks>
public interface IOsbaObject : IUseBusinessContext
{
	/// <summary>
	/// 获取负责字段数据操作的管理器实例。
	/// </summary>
	/// <remarks>
	/// 使用此属性访问字段数据以进行检索和存储操作。返回的管理器
	/// 提供了与字段相关数据交互的方法，对于需要操作或查询字段的场景至关重要。
	/// </remarks>
	FieldDataManager FieldManager { get; }

	/// <summary>
	/// 确定指定属性是否存在于当前上下文中。
	/// </summary>
	/// <param name="property">要检查其是否存在的属性信息。此参数不能为 <c>null</c>。</param>
	/// <returns>如果属性存在，则为 <see langword="true"/>；否则为 <see langword="false"/>。</returns>
	bool FieldExists(IPropertyInfo property);

	/// <summary>
	/// 检索由给定属性信息指定的属性值。
	/// </summary>
	/// <param name="propertyInfo">提供要读取属性元数据的对象。</param>
	/// <returns>指定属性的值；如果属性尚未设置，则返回注册的默认值。</returns>
	object ReadProperty(IPropertyInfo propertyInfo);

	/// <summary>
	/// 按名称检索指定属性的值。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>指定属性的值；如果属性尚未设置，则返回注册的默认值。</returns>
	object ReadProperty(string propertyName);

	/// <summary>
	/// 读取指定属性的值，并将其作为请求的类型返回。
	/// </summary>
	/// <typeparam name="TValue">要读取的属性值的类型。</typeparam>
	/// <param name="propertyInfo">属性元数据。</param>
	/// <returns>指定属性的值。</returns>
	TValue ReadProperty<TValue>(PropertyInfo<TValue> propertyInfo);

	/// <summary>
	/// 按名称读取指定属性的值，并将其作为请求的类型返回。
	/// </summary>
	/// <typeparam name="TValue">要读取的属性值的类型。</typeparam>
	/// <param name="propertyName">属性名称。</param>
	/// <returns>指定属性的值。</returns>
	TValue ReadProperty<TValue>(string propertyName);

	/// <summary>
	/// 使用新值装载指定属性。
	/// </summary>
	/// <param name="propertyInfo">属性元数据。</param>
	/// <param name="newValue">新值。</param>
	void LoadProperty(IPropertyInfo propertyInfo, object newValue);

	/// <summary>
	/// 使用新值装载指定属性。
	/// </summary>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="propertyInfo">属性元数据。</param>
	/// <param name="newValue">新值。</param>
	void LoadProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue newValue);
}
