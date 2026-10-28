namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 可加入父子对象图的容器节点。
/// </summary>
/// <remarks>
/// <see cref="ObjectBase"/> 通过父链实现“子属性变更通知父对象”和“规则绕过向下穿透”。
/// 集合（<see cref="ObservableList{T}"/>）不是 <see cref="ObjectBase"/>，但可以中转这两件事：
/// 它把父链接传给自己的元素，并在集合本身变化时通知父对象。
/// </remarks>
public interface IChildGraphNode
{
	/// <summary>
	/// 登记父对象及其持有本节点的属性。
	/// </summary>
	/// <param name="parent">父对象。</param>
	/// <param name="property">父对象上持有本节点的属性。</param>
	void AddParent(ObjectBase parent, IPropertyInfo property);

	/// <summary>
	/// 移除父对象链接。
	/// </summary>
	/// <param name="parent">父对象。</param>
	/// <param name="property">父对象上持有本节点的属性。</param>
	void RemoveParent(ObjectBase parent, IPropertyInfo property);
}
