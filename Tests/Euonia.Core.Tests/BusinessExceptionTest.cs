// ReSharper disable All
using Xunit;

namespace Nerosoft.Euonia.Core.Tests;

public class BusinessExceptionTest
{
    [Fact]
    public void CodeOnlyConstructor_SetsCodeAndMessage()
    {
        var exception = new BusinessException("E001");

        Assert.Equal("E001", exception.Code);
        Assert.Equal("E001", exception.Message);
    }

    [Fact]
    public void CodeAndMessageConstructor_KeepsMessage()
    {
        var exception = new BusinessException("E001", "something went wrong");

        Assert.Equal("E001", exception.Code);
        Assert.Equal("something went wrong", exception.Message);
    }
}