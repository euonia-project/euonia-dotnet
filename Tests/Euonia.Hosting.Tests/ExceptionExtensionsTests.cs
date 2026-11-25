using System.ComponentModel.DataAnnotations;
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

	/// <summary>
	/// 验证异常的 MemberNames 含重复与空白键时应过滤去重，而不是在异常处理器内再次抛异常。
	/// </summary>
	[Fact]
	public void GetErrorDetails_DuplicateAndBlankMemberNames_Deduplicates()
	{
		var result = new ValidationResult("字段无效", ["name", "name", "", " ", null!]);
		var exception = new ValidationException(result, null, null);

		var details = exception.GetErrorDetails();

		Assert.NotNull(details);
		var entry = Assert.Single(details);
		Assert.Equal("name", entry.Key);
		Assert.Equal(["字段无效"], entry.Value);
	}
}
