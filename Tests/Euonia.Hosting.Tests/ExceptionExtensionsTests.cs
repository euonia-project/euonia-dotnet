using System.Reflection;
using System.Security.Authentication;
using Nerosoft.Euonia.Hosting;

namespace Nerosoft.Euonia.Hosting.Tests;

/// <summary>
/// 针对异常到 HTTP 状态码映射的测试。
/// </summary>
public class ExceptionExtensionsTests
{
	/// <summary>
	/// 常见异常应映射到与 Web API 一致的状态码。
	/// </summary>
	[Fact]
	public void GetStatusCode_MapsCommonExceptions()
	{
		Assert.Equal(400, (int)new ArgumentException().GetStatusCode());
		Assert.Equal(401, (int)new AuthenticationException().GetStatusCode());
		Assert.Equal(403, (int)new UnauthorizedAccessException().GetStatusCode());
		Assert.Equal(501, (int)new NotImplementedException().GetStatusCode());
		Assert.Equal(504, (int)new TimeoutException().GetStatusCode());
		Assert.Equal(500, (int)new InvalidOperationException().GetStatusCode());
	}

	/// <summary>
	/// 聚合异常与反射调用异常应取内部异常的状态码。
	/// </summary>
	[Fact]
	public void GetStatusCode_UnwrapsInnerException()
	{
		Assert.Equal(400, (int)new AggregateException(new ArgumentException()).GetStatusCode());
		Assert.Equal(403, (int)new TargetInvocationException(new UnauthorizedAccessException()).GetStatusCode());
	}
}
