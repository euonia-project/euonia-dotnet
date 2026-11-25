using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Moq;

namespace Nerosoft.Euonia.Hosting.Azfunc.Tests;

/// <summary>
/// 构造中间件测试所需的 <see cref="FunctionContext"/> 与 <see cref="FunctionDefinition"/> 模拟对象。
/// </summary>
internal static class TestFunctionContext
{
	// GetHttpContext 扩展从 Items 的该键读取 HttpContext；键名为扩展程序集内部常量，经反射取得以免硬编码。
	private static readonly object HttpContextItemKey = typeof(FunctionContextExtensions)
		.Assembly.GetType("Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.Constants")!
		.GetField("HttpContextKey", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
		.GetValue(null)!;

	/// <summary>
	/// 创建携带指定 HTTP 上下文与服务提供程序的函数上下文。
	/// </summary>
	/// <param name="httpContext">HTTP 上下文；为 <c>null</c> 时模拟非 HTTP 触发器。</param>
	/// <param name="services">函数实例服务提供程序。</param>
	/// <param name="definition">函数定义。</param>
	/// <param name="cancellationToken">函数调用取消令牌。</param>
	/// <returns>模拟的函数上下文。</returns>
	public static FunctionContext Create(HttpContext? httpContext, IServiceProvider services, FunctionDefinition definition, CancellationToken cancellationToken = default)
	{
		var items = new Dictionary<object, object>();

		if (httpContext is not null)
		{
			items[HttpContextItemKey] = httpContext;
		}

		var context = new Mock<FunctionContext>();
		context.SetupGet(item => item.Items).Returns(items);
		context.SetupGet(item => item.InstanceServices).Returns(services);
		context.SetupGet(item => item.FunctionDefinition).Returns(definition);
		context.SetupGet(item => item.CancellationToken).Returns(cancellationToken);
		return context.Object;
	}

	/// <summary>
	/// 创建指定名称与入口点的函数定义。
	/// </summary>
	/// <param name="name">函数名。</param>
	/// <param name="entryPoint">入口点（类型全名.方法名）。</param>
	/// <param name="pathToAssembly">入口程序集路径。</param>
	/// <returns>模拟的函数定义。</returns>
	public static FunctionDefinition CreateDefinition(string name, string entryPoint, string? pathToAssembly = null)
	{
		var definition = new Mock<FunctionDefinition>();
		definition.SetupGet(item => item.Name).Returns(name);
		definition.SetupGet(item => item.EntryPoint).Returns(entryPoint);
		definition.SetupGet(item => item.PathToAssembly).Returns(pathToAssembly!);
		return definition.Object;
	}
}
