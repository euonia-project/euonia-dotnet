namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 业务对象成员的元数据，通常是一个方法或属性。
/// </summary>
public interface IMemberInfo
{
    /// <summary>
    /// 获取成员名称值。
    /// </summary>
    string Name { get; }
}