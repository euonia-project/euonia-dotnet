namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents a contract for an object that can get and set properties.
/// </summary>
public interface IOperableProperty
{
    /// <summary>
    /// Gets the value of specified property.
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <returns></returns>
    object GetProperty(IPropertyInfo propertyInfo);

    /// <summary>
    /// Sets the value of specified property.
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <param name="newValue"></param>
    void SetProperty(IPropertyInfo propertyInfo, object newValue);
}
