namespace Nerosoft.Euonia.Mapping.Tests.Models;

/// <summary>
/// 映射测试使用的源对象，提供标识、名称、地址与年龄。
/// </summary>
public class SourceObject1
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