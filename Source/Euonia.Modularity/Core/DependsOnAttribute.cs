namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 表示应用此特性的类依赖于指定的模块。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute : Attribute
{
    /// <summary>
    /// 初始化继承自 <see cref="DependsOnAttribute"/> 的新实例。
    /// </summary>
    /// <param name="types">所依赖的模块类型；为 <see langword="null"/> 时视为空数组。</param>
    public DependsOnAttribute(params Type[] types)
    {
        ModuleTypes = types ?? Array.Empty<Type>();
    }

    /// <summary>
    /// 获取所依赖的模块。
    /// </summary>
    public Type[] ModuleTypes { get; }
}
