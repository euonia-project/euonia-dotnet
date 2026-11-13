namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// Mongo 模型属性构建器。
/// </summary>
public class ModelPropertyBuilder
{
    /// <summary>
    /// 使用属性名与属性类型初始化模型属性构建器。
    /// </summary>
    /// <param name="propertyName">属性的 CLR 名称。</param>
    /// <param name="propertyType">属性的 CLR 类型。</param>
    internal ModelPropertyBuilder(string propertyName, Type propertyType)
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
    }

    /// <summary>
    /// 获取属性的 CLR 名称。
    /// </summary>
    internal string PropertyName { get; }

    /// <summary>
    /// 获取属性的 CLR 类型。
    /// </summary>
    internal Type PropertyType { get; }

    /// <summary>
    /// 获取映射到 Mongo 文档中的元素名称；未设置时为 <c>null</c>。
    /// </summary>
    internal string? ElementName { get; private set; }

    /// <summary>
    /// 获取属性是否必填；未显式设置时为 <c>null</c>。
    /// </summary>
    internal bool? IsRequired { get; private set; }

    /// <summary>
    /// 获取属性的值生成器；未配置时为 <c>null</c>。
    /// </summary>
    internal IValueGenerator? ValueGenerator { get; private set; }

    /// <summary>
    /// 设置当前属性的元素名称。
    /// </summary>
    /// <param name="elementName">元素名称，用于映射到 Mongo 文档中的字段名。</param>
    /// <returns>当前构建器实例，以便链式调用。</returns>
    public ModelPropertyBuilder HasElementName(string elementName)
    {
        ElementName = elementName;
        return this;
    }

    /// <summary>
    /// 设置一个值，指示当前属性是否必填。
    /// </summary>
    /// <param name="value">指示属性是否必填；<see langword="true"/> 表示必填。</param>
    /// <returns>当前构建器实例，以便链式调用。</returns>
    public ModelPropertyBuilder Required(bool value)
    {
        IsRequired = value;
        return this;
    }

    /// <summary>
    /// 为当前属性设置值生成器。
    /// </summary>
    /// <typeparam name="TGenerator">值生成器类型，必须实现 <see cref="IValueGenerator"/> 且具有无参构造函数。</typeparam>
    /// <returns>当前构建器实例，以便链式调用。</returns>
    public ModelPropertyBuilder HasValueGenerator<TGenerator>()
        where TGenerator : class, IValueGenerator, new()
    {
        ValueGenerator = Singleton<TGenerator>.Get(() => new());
        return this;
    }

    /// <summary>
    /// 为当前属性设置值生成器。
    /// </summary>
    /// <param name="generator">用于为当前属性生成值的生成器实例。</param>
    /// <returns>当前构建器实例，以便链式调用。</returns>
    public ModelPropertyBuilder HasValueGenerator(IValueGenerator generator)
    {
        ValueGenerator = generator;
        return this;
    }
}