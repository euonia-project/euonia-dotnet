namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示被标记的方法可由 <see cref="IObjectFactory"/> 调用。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public abstract class FactoryMethodAttribute : Attribute
{
}