using System.Reflection;
using Nerosoft.Euonia.Endpoint;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 表示扫描到的一个待暴露 HTTP 方法。
/// </summary>
internal sealed class ExposedWebApiMethod
{
	/// <summary>
	/// 初始化 <see cref="ExposedWebApiMethod"/> 的新实例。
	/// </summary>
	/// <param name="serviceType">声明方法的服务接口类型。</param>
	/// <param name="method">接口方法。</param>
	/// <param name="attribute">方法上的暴露特性。</param>
	public ExposedWebApiMethod(Type serviceType, MethodInfo method, ExposeWebApiAttribute attribute)
	{
		ServiceType = serviceType;
		Method = method;
		Attribute = attribute;
	}

	/// <summary>
	/// 获取声明方法的服务接口类型。
	/// </summary>
	public Type ServiceType { get; }

	/// <summary>
	/// 获取接口方法。
	/// </summary>
	public MethodInfo Method { get; }

	/// <summary>
	/// 获取方法上的暴露特性。
	/// </summary>
	public ExposeWebApiAttribute Attribute { get; }
}
