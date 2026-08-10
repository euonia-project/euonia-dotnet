namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示被标记的方法将查找已存在的领域对象数据。
/// 该方法可由 <see cref="IObjectFactory"/> 调用。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FactoryFetchAttribute : FactoryMethodAttribute
{
}