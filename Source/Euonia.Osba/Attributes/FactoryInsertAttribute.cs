namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示被标记的方法将使用领域对象数据插入新行。
/// 该方法可由 <see cref="IObjectFactory"/> 调用。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FactoryInsertAttribute : FactoryMethodAttribute
{
}