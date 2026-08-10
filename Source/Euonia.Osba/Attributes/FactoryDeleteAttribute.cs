namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示被标记的方法将删除领域对象数据。
/// 该方法可由 <see cref="IObjectFactory"/> 调用。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FactoryDeleteAttribute : FactoryMethodAttribute
{
}