using System.ComponentModel;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents a contract for an object that can be used as business object.
/// </summary>
public interface IBusinessObject : IUseBusinessContext, INotifyPropertyChanged, INotifyPropertyChanging
{
    /// <summary>
    /// The manager that stores field data from Field objects related to this object. 
    /// </summary>
    FieldDataManager FieldManager { get; }

    /// <summary>
    /// Determines if the specified field exists in the current instance.
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    bool FieldExists(IPropertyInfo property);

    /// <summary>
    /// Reads the specified property from the current instance.
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <returns></returns>
    object ReadProperty(IPropertyInfo propertyInfo);

    /// <summary>
    /// Reads the specified property from the current instance.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="propertyInfo"></param>
    /// <returns></returns>
    TValue ReadProperty<TValue>(PropertyInfo<TValue> propertyInfo);

    /// <summary>
    /// Loads the specified value into the specified property of the current instance.
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <param name="newValue"></param>
    void LoadProperty(IPropertyInfo propertyInfo, object newValue);

    /// <summary>
    /// Loads the specified value into the specified property of the current instance.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="propertyInfo"></param>
    /// <param name="newValue"></param>
    void LoadProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue newValue);
}