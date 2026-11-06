// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 提供全局验证器工厂的设置与验证器创建入口。
/// </summary>
public class ValidatorFactory
{
    #region Members

    private static IValidatorFactory? _factory;

    #endregion

    #region Public Methods

    /// <summary>
    /// 设置要使用的实体验证器工厂
    /// </summary>
    /// <param name="factory">要使用的验证器工厂。</param>
    public static void SetCurrent(IValidatorFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// 创建新的 <see cref="IValidator"/> 实例。
    /// </summary>
    /// <returns>创建的 ILog</returns>
    public static IValidator? Create()
    {
        return _factory?.Create();
    }

    #endregion
}