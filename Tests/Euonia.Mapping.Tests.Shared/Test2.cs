using Nerosoft.Euonia.Mapping.Tests.Models;

namespace Nerosoft.Euonia.Mapping.Tests;

/// <summary>
/// 验证全名拼接映射行为的测试。
/// </summary>
public class Test2
{
    /// <summary>
    /// 源对象的名与姓应以空格拼接后映射到目标对象的完整姓名属性。
    /// </summary>
    [Fact]
    public void Test()
    {
        var source = new SourceObject2
        {
            Id = 1,
            FirstName = "FirstName",
            LastName = "LastName",
            Address = "New York",
            Age = 20
        };
        
        var destination = TypeAdapter.ProjectedAs<DestinationObject2>(source);
        Assert.Equal(source.Id, destination.Id);
        Assert.Equal($"{source.FirstName} {source.LastName}", destination.FullName);
        Assert.Equal(source.Address, destination.Address);
        Assert.Equal(source.Age, destination.Age);
    }
}