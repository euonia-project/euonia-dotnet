namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 创建验证器实例的工厂契约。
/// </summary>
public interface IValidatorFactory
{
    /// <summary>
    /// 创建一个新的验证器实例。
    /// </summary>
    /// <returns>新建的验证器实例。</returns>
    IValidator Create();
}