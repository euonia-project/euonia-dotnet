namespace Nerosoft.Euonia.Mapping.Tests.Models;

/// <summary>
/// 映射测试使用的源对象，分别提供名与姓以验证全名拼接映射。
/// </summary>
public class SourceObject2
{
    /// <summary>
    /// 获取或设置标识。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 获取或设置名。
    /// </summary>
    public string FirstName { get; set; } = null!;

    /// <summary>
    /// 获取或设置姓。
    /// </summary>
    public string LastName { get; set; } = null!;

    /// <summary>
    /// 获取或设置地址。
    /// </summary>
    public string Address { get; set; } = null!;

    /// <summary>
    /// 获取或设置年龄。
    /// </summary>
    public int Age { get; set; }
}