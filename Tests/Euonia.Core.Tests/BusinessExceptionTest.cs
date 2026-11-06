// ReSharper disable All
using Xunit;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 针对 <see cref="BusinessException"/> 构造函数行为的测试。
/// </summary>
public class BusinessExceptionTest
{
    /// <summary>
    /// 验证仅传入错误码时，<see cref="BusinessException.Code"/> 与 <see cref="Exception.Message"/> 都取该错误码。
    /// </summary>
    [Fact]
    public void CodeOnlyConstructor_SetsCodeAndMessage()
    {
        var exception = new BusinessException("E001");

        Assert.Equal("E001", exception.Code);
        Assert.Equal("E001", exception.Message);
    }

    /// <summary>
    /// 验证同时传入错误码与消息时，<see cref="BusinessException.Code"/> 与自定义消息都被保留。
    /// </summary>
    [Fact]
    public void CodeAndMessageConstructor_KeepsMessage()
    {
        var exception = new BusinessException("E001", "something went wrong");

        Assert.Equal("E001", exception.Code);
        Assert.Equal("something went wrong", exception.Message);
    }
}