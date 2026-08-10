namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示被标记的方法将执行一个已定义的命令。
/// 该方法可由 <see cref="IObjectFactory"/> 调用。
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class FactoryExecuteAttribute : FactoryMethodAttribute
{
}