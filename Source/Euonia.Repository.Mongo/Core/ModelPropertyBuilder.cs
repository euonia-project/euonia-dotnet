namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// The mongo model property builder.
/// </summary>
public class ModelPropertyBuilder
{
    internal ModelPropertyBuilder(string propertyName, Type propertyType)
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
    }

    internal string PropertyName { get; }

    internal Type PropertyType { get; }

    internal string? ElementName { get; private set; }

    internal bool? IsRequired { get; private set; }

    internal IValueGenerator? ValueGenerator { get; private set; }

    /// <summary>
    /// Sets the element name for current property.
    /// </summary>
    /// <param name="elementName"></param>
    /// <returns></returns>
    public ModelPropertyBuilder HasElementName(string elementName)
    {
        ElementName = elementName;
        return this;
    }

    /// <summary>
    /// Sets a value indicating whether the current property is required.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public ModelPropertyBuilder Required(bool value)
    {
        IsRequired = value;
        return this;
    }

    /// <summary>
    /// Sets the value generator for current property.
    /// </summary>
    /// <typeparam name="TGenerator"></typeparam>
    /// <returns></returns>
    public ModelPropertyBuilder HasValueGenerator<TGenerator>()
        where TGenerator : class, IValueGenerator, new()
    {
        ValueGenerator = Singleton<TGenerator>.Get(() => new());
        return this;
    }

    /// <summary>
    /// Sets the value generator for current property.
    /// </summary>
    /// <param name="generator"></param>
    /// <returns></returns>
    public ModelPropertyBuilder HasValueGenerator(IValueGenerator generator)
    {
        ValueGenerator = generator;
        return this;
    }
}