namespace Nerosoft.Euonia.Collections;

/// <summary>
/// <see cref="ITypeList{TBaseType}"/> 的快捷方式，使用 object 作为基类型。
/// </summary>
public interface ITypeList : ITypeList<object>
{

}

/// <summary>
/// 扩展 <see cref="IList{Type}"/> 以添加对特定基类型的限制。
/// </summary>
/// <typeparam name="TBaseType">此列表中 <see cref="Type"/> 的基类型</typeparam>
public interface ITypeList<in TBaseType> : IList<Type>
{
    /// <summary>
    /// 向列表中添加一个类型。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    void Add<T>() where T : TBaseType;

    /// <summary>
    /// 如果列表中尚未存在该类型，则将其添加到列表中。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    bool TryAdd<T>() where T : TBaseType;

    /// <summary>
    /// 检查列表中是否存在某个类型。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <returns>如果列表中存在该类型，则为 true；否则为 false。</returns>
    bool Contains<T>() where T : TBaseType;

    /// <summary>
    /// 从列表中移除一个类型。
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    void Remove<T>() where T : TBaseType;
}
