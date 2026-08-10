using System.ComponentModel;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 定义与字段数据交互并支持属性更改通知的业务对象契约。
/// </summary>
/// <remarks>
/// 实现此接口允许类管理字段数据操作并响应属性更改，确保业务逻辑被正确封装且数据完整性得以保持。
/// </remarks>
public interface IBusinessObject : IUseBusinessContext, INotifyPropertyChanged, INotifyPropertyChanging
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
	/// <remarks>
	/// 此方法用于在执行依赖属性存在的操作之前验证属性是否存在。
	/// </remarks>
	/// <param name="property">要检查其是否存在的属性信息。此参数不能为 <c>null</c>。</param>
	/// <returns>如果属性存在，则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	bool FieldExists(IPropertyInfo property);

	/// <summary>
	/// 检索由给定属性信息指定的属性值。
	/// </summary>
	/// <remarks>
	/// 使用此方法访问由 IPropertyInfo 实例标识的属性的当前值。
	/// 确保 propertyInfo 参数引用的是可读的有效属性。此方法不会设置或修改属性值。
	/// </remarks>
	/// <param name="propertyInfo">提供要读取属性元数据的对象。必须表示可读属性；否则结果可能为 <c>null</c>。</param>
	/// <returns>指定属性的值；如果属性尚未设置，则为 <c>null</c>。</returns>
	object ReadProperty(IPropertyInfo propertyInfo);

	/// <summary>
	/// 按名称检索指定属性的值。
	/// </summary>
	/// <remarks>
	/// 使用此方法访问由名称标识的属性的当前值。
	/// 确保提供的属性名称对应一个可读的有效属性。此方法不会设置或修改属性值。
	/// </remarks>
	/// <param name="propertyName">要读取的属性名称。必须表示可读属性。</param>
	/// <returns>指定属性的值；如果属性尚未设置，则为 <c>null</c>。</returns>
	object ReadProperty(string propertyName);
	
	/// <summary>
	/// 读取指定属性的值，并将其作为请求的类型返回。
	/// </summary>
	/// <remarks>
	/// 在调用此方法之前，确保 <paramref name="propertyInfo"/> 引用的属性可访问且包含有效值。
	/// 如果属性不可读或值无法转换为 <typeparamref name="TValue"/>，则可能抛出异常。
	/// </remarks>
	/// <typeparam name="TValue">要读取的属性值的类型。</typeparam>
	/// <param name="propertyInfo">提供要读取属性元数据（包括其类型和访问信息）的对象。不能为 <c>null</c>。</param>
	/// <returns>指定属性的值，转换为 <typeparamref name="TValue"/> 指定的类型。</returns>
	TValue ReadProperty<TValue>(PropertyInfo<TValue> propertyInfo);
	
	/// <summary>
	/// 按名称读取指定属性的值，并将其作为请求的类型返回。
	/// </summary>
	/// <remarks>
	/// 确保提供的属性名称对应一个可读的有效属性，且值可以转换为 <typeparamref name="TValue"/>。
	/// 如果属性不可读或值无法转换为指定类型，则可能抛出异常。
	/// </remarks>
	/// <param name="propertyName">要读取的属性名称。必须表示可读属性。</param>
	/// <typeparam name="TValue">要读取的属性值的类型。</typeparam>
	/// <returns>指定属性的值，转换为 <typeparamref name="TValue"/> 指定的类型。</returns>
	TValue ReadProperty<TValue>(string propertyName);

	/// <summary>
	/// 使用新值加载指定属性，根据其元数据信息更新属性值。
	/// </summary>
	/// <remarks>在某些平台（如 iOS）上，此方法显式处理可空类型以避免运行时错误。
	/// 对于其他类型，使用反射来赋值。确保新值与属性的预期类型匹配，以避免异常。</remarks>
	/// <param name="propertyInfo">标识要加载属性的元数据信息。此参数决定属性的类型和其他特征。</param>
	/// <param name="newValue">要赋给指定属性的新值。该值必须与属性的类型兼容。</param>
	void LoadProperty(IPropertyInfo propertyInfo, object newValue);

	/// <summary>
	/// 使用新值更新指定属性并相应地调整其状态。
	/// </summary>
	/// <remarks>
	/// 确保新值对正在更新的属性有效。提供无效值可能导致异常或未定义行为。
	/// </remarks>
	/// <typeparam name="TValue">要赋给属性的值的类型。</typeparam>
	/// <param name="propertyInfo">标识要更新属性的元数据。此参数定义属性的特征和类型。</param>
	/// <param name="newValue">要赋给属性的新值。必须与属性的类型兼容。</param>
	void LoadProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue newValue);
}