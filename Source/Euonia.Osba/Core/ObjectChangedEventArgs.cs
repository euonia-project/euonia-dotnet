using System.Collections.Specialized;
using System.ComponentModel;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 为对象或其属性更改时引发的事件提供数据。
/// </summary>
/// <remarks>
/// 此类将多种更改通知类型整合到单个事件参数结构中，支持属性更改、集合更改和列表更改。
/// 它通常用于需要将更改通知通过父子关系向上传播的复杂对象图场景。
/// </remarks>
public class ObjectChangedEventArgs : EventArgs
{
	/// <summary>
	/// 使用指定的更改对象和属性更改信息初始化 <see cref="ObjectChangedEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="changedObject">发生更改的对象。</param>
	/// <param name="propertyChangedArgs">包含属性更改详细信息的 <see cref="PropertyChangedEventArgs"/>。</param>
	public ObjectChangedEventArgs(object changedObject, PropertyChangedEventArgs propertyChangedArgs)
	{
		ChangedObject = changedObject;
		PropertyChangedArgs = propertyChangedArgs;
	}

	/// <summary>
	/// 使用指定的更改对象、属性更改信息和集合更改信息初始化 <see cref="ObjectChangedEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="changedObject">发生更改的对象。</param>
	/// <param name="propertyChangedArgs">包含属性更改详细信息的 <see cref="PropertyChangedEventArgs"/>。</param>
	/// <param name="collectionChangedArgs">包含集合更改详细信息的 <see cref="NotifyCollectionChangedEventArgs"/>。</param>
	public ObjectChangedEventArgs(object changedObject, PropertyChangedEventArgs propertyChangedArgs, NotifyCollectionChangedEventArgs collectionChangedArgs)
		: this(changedObject, propertyChangedArgs)
	{
		CollectionChangedArgs = collectionChangedArgs;
	}

	/// <summary>
	/// 使用指定的更改对象、属性更改信息和列表更改信息初始化 <see cref="ObjectChangedEventArgs"/> 类的新实例。
	/// </summary>
	/// <param name="changedObject">发生更改的对象。</param>
	/// <param name="propertyChangedArgs">包含属性更改详细信息的 <see cref="PropertyChangedEventArgs"/>。</param>
	/// <param name="listChangedArgs">包含列表更改详细信息的 <see cref="ListChangedEventArgs"/>。</param>
	public ObjectChangedEventArgs(object changedObject, PropertyChangedEventArgs propertyChangedArgs, ListChangedEventArgs listChangedArgs)
		: this(changedObject, propertyChangedArgs)
	{
		ListChangedArgs = listChangedArgs;
	}

	/// <summary>
	/// 获取发生更改的对象。
	/// </summary>
	/// <value>
	/// 触发更改通知的对象实例。
	/// </value>
	public object ChangedObject { get; }

	/// <summary>
	/// 获取属性更改事件参数。
	/// </summary>
	/// <value>
	/// 包含已更改属性信息的 <see cref="PropertyChangedEventArgs"/> 实例；
	/// 如果更改与属性无关，则为 <see langword="null"/>。
	/// </value>
	public PropertyChangedEventArgs PropertyChangedArgs { get; }

	/// <summary>
	/// 获取集合更改事件参数。
	/// </summary>
	/// <value>
	/// 包含集合更改信息的 <see cref="NotifyCollectionChangedEventArgs"/> 实例；
	/// 如果更改与集合无关，则为 <see langword="null"/>。
	/// </value>
	public NotifyCollectionChangedEventArgs CollectionChangedArgs { get; }

	/// <summary>
	/// 获取列表更改事件参数。
	/// </summary>
	/// <value>
	/// 包含列表更改信息的 <see cref="ListChangedEventArgs"/> 实例；
	/// 如果更改与列表无关，则为 <see langword="null"/>。
	/// </value>
	public ListChangedEventArgs ListChangedArgs { get; }
}