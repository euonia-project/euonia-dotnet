namespace Nerosoft.Euonia.Application;

/// <summary>
/// 定义拦截器执行顺序；数字越小越靠外层。
/// </summary>
/// <remarks>
/// Castle 按拦截器数组顺序执行，第一个最外层。未实现本接口的拦截器按 <c>0</c> 参与稳定排序。
/// </remarks>
public interface IOrderedInterceptor
{
	/// <summary>
	/// 获取执行顺序。
	/// </summary>
	int Order { get; }
}
