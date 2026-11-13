using System.Collections.ObjectModel;

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 用于验证对象的接口。
/// </summary>
public interface IValidatableObject
{
    /// <summary>
    /// 获取错误集合。
    /// </summary>
    /// <value>错误集合。</value>
    ObservableCollection<string> Errors { get; }

    /// <summary>
    /// 如果对象有效则返回 true。
    /// </summary>
    /// <value>如果此实例有效则为 <c>true</c>；否则为 <c>false</c>。</value>
    bool IsValid { get; }

    /// <summary>
    /// 验证此实例。
    /// </summary>
    void Validate();
}
