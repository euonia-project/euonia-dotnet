using System.ComponentModel;

namespace Nerosoft.Euonia.Collections;

/// <summary>
/// 表示分组项集合的接口。
/// 它允许通过提供一个可使用 x:DataType 声明的非泛型类型，
/// 来对 <see cref="ObservableGroup{TKey, TValue}"/> 和 <see cref="ReadOnlyObservableGroup{TKey, TValue}"/> 使用 x:Bind。
/// </summary>
public interface IReadOnlyObservableGroup : INotifyPropertyChanged
{
    /// <summary>
    /// 获取当前集合的键，类型为 <see cref="object"/>。
    /// 此属性不可变。
    /// </summary>
    object Key { get; }

    /// <summary>
    /// 获取当前分组集合中的项数量。
    /// </summary>
    int Count { get; }
}
