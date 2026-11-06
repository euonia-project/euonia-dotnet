using Nerosoft.Euonia.Mapping.Tests.Models;

namespace Nerosoft.Euonia.Mapping.Tests;

/// <summary>
/// 验证基础属性同名映射行为的测试。
/// </summary>
public class Test1
{
    /// <summary>
    /// 同名的标识、名称、地址与年龄属性应被完整映射到目标对象。
    /// </summary>
    [Fact]
    public void SimpleTest()
    {
        var source = new SourceObject1
        {
            Id = 1,
            Name = "Name", Address = "New York",
            Age = 20
        };

        var destination = TypeAdapter.ProjectedAs<DestinationObject1>(source);
        Assert.Equal(source.Id, destination.Id);
        Assert.Equal(source.Name, destination.Name);
        Assert.Equal(source.Address, destination.Address);
        Assert.Equal(source.Age, destination.Age);
    }
}