namespace Nerosoft.Euonia.Mapping.Tests.Models;

/// <summary>
/// 映射测试使用的目标对象，全名由源对象的名与姓拼接而成。
/// </summary>
public class DestinationObject2
{
    /// <summary>
    /// 获取或设置标识。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 获取或设置拼接后的完整姓名。
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>
    /// 获取或设置地址。
    /// </summary>
    public string Address { get; set; } = null!;

    /// <summary>
    /// 获取或设置年龄。
    /// </summary>
    public int Age { get; set; }
}