namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The property info list.
/// </summary>
public class PropertyInfoList : List<IPropertyInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyInfoList"/> class.
    /// </summary>
    public PropertyInfoList()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyInfoList"/> class.
    /// </summary>
    /// <param name="collection"></param>
    public PropertyInfoList(IEnumerable<IPropertyInfo> collection)
        : base(collection)
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the current instance is locked for edit.
    /// </summary>
    public bool IsLocked { get; set; }
}