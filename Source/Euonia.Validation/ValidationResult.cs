namespace Nerosoft.Euonia.Validation;

/// <summary>
/// The validation result.
/// </summary>
[Serializable]
public class ValidationResult
{
    /// <summary>
    /// 
    /// </summary>
    public ValidationResult()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyName"></param>
    /// <param name="errorMessage"></param>
    public ValidationResult(string propertyName, string errorMessage)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// 
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string ErrorMessage { get; set; }
}
