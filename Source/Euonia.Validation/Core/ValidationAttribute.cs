namespace Nerosoft.Euonia.Validation;

/// <summary>
/// 表示方法/参数/类应被验证。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Method)]
public class ValidationAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="ValidationAttribute"/> 类的新实例。
    /// </summary>
    public ValidationAttribute()
    {
    }

    /// <summary>
    /// 初始化 <see cref="ValidationAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="validatorType">验证器的类型。</param>
    public ValidationAttribute(Type validatorType)
        : this()
    {
        ValidatorType = validatorType;
    }

    /// <summary>
    /// 获取验证器的类型。
    /// </summary>
    /// <value>验证器的类型。</value>
    public Type? ValidatorType { get; }
}