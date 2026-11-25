using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace Nerosoft.Euonia.Hosting.Azfunc.Tests;

/// <summary>
/// 针对函数入口方法解析 fail-closed 与结果缓存的测试。
/// </summary>
public class FunctionMethodResolverTests
{
	/// <summary>
	/// 同名重载无法确定授权特性归属时应抛异常拒绝执行，而不是取第一个匹配。
	/// </summary>
	[Fact]
	public void AmbiguousOverloads_FailClosed()
	{
		var entryPoint = typeof(OverloadFunctions).FullName + "." + nameof(OverloadFunctions.Run);

		var exception = Assert.Throws<InvalidOperationException>(() =>
			FunctionMethodResolver.Resolve(typeof(OverloadFunctions).Assembly.Location, entryPoint));

		Assert.Contains(entryPoint, exception.Message);
	}

	/// <summary>
	/// 歧义失败结果应被缓存，后续请求不再扫描程序集且仍 fail-closed。
	/// </summary>
	[Fact]
	public void AmbiguousOverloads_FailureCached()
	{
		var assemblyLocation = typeof(OverloadFunctions).Assembly.Location;
		var entryPoint = typeof(OverloadFunctions).FullName + "." + nameof(OverloadFunctions.Run);

		Assert.Throws<InvalidOperationException>(() => FunctionMethodResolver.Resolve(assemblyLocation, entryPoint));

		Assert.True(GetCache().Contains(assemblyLocation + "|" + entryPoint));
		Assert.Throws<InvalidOperationException>(() => FunctionMethodResolver.Resolve(assemblyLocation, entryPoint));
	}

	/// <summary>
	/// 无法解析的入口点应返回 null 并缓存失败结果。
	/// </summary>
	[Fact]
	public void UnresolvableEntry_ReturnsNullAndCachesFailure()
	{
		var assemblyLocation = typeof(FunctionMethodResolverTests).Assembly.Location;
		var entryPoint = "Nerosoft.Euonia.Hosting.Azfunc.Tests.MissingType.MissingMethod";

		var method = FunctionMethodResolver.Resolve(assemblyLocation, entryPoint);

		Assert.Null(method);
		Assert.True(GetCache().Contains(assemblyLocation + "|" + entryPoint));
	}

	private static IDictionary GetCache()
	{
		return (IDictionary)typeof(FunctionMethodResolver)
			.GetField("Cache", BindingFlags.NonPublic | BindingFlags.Static)!
			.GetValue(null)!;
	}

	private sealed class OverloadFunctions
	{
		[AllowAnonymous]
		public static void Run()
		{
		}

		[Authorize]
		public static void Run(int value)
		{
		}
	}
}
