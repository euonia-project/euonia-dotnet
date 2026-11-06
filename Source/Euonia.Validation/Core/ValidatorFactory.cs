// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 
/// </summary>
public class ValidatorFactory
{
    #region Members

    private static IValidatorFactory? _factory;

    #endregion

    #region Public Methods

    /// <summary>
    /// Set the  entity validator factory to use
    /// </summary>
    /// <param name="factory">Log factory to use</param>
    public static void SetCurrent(IValidatorFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Create a new <see cref="IValidator"/> instance.
    /// </summary>
    /// <returns>Created ILog</returns>
    public static IValidator? Create()
    {
        return _factory?.Create();
    }

    #endregion
}