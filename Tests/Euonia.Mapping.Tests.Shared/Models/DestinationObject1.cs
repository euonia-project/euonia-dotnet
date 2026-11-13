namespace Nerosoft.Euonia.Mapping.Tests.Models;

/// <summary>
/// 映射测试使用的目标对象，包含与源对象同名的属性。
/// </summary>
public class DestinationObject1
{
    /// <summary>
    /// 获取或设置标识。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 获取或设置名称。
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// 获取或设置地址。
    /// </summary>
    public string Address { get; set; } = null!;

    /// <summary>
    /// 获取或设置年龄。
    /// </summary>
    public int Age { get; set; }
}