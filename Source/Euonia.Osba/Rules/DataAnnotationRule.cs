using System.ComponentModel.DataAnnotations;

using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示基于 <see cref="ValidationAttribute"/> 的规则。
/// </summary>
public class DataAnnotationRule : RuleBase
{
    /// <summary>
    /// 获取验证特性。
    /// </summary>
    public ValidationAttribute Attribute { get; }

    /// <summary>
    /// 初始化 <see cref="DataAnnotationRule"/> 类的新实例。
    /// </summary>
    /// <param name="property">受规则影响的属性。</param>
    /// <param name="attribute">验证特性。</param>
    public DataAnnotationRule(IPropertyInfo property, ValidationAttribute attribute)
        : base(property, attribute.GetType())
    {
        Attribute = attribute;
    }

    /// <summary>
    /// 执行规则检查。
    /// </summary>
    /// <param name="context">规则上下文。</param>
    /// <param name="cancellationToken">用于取消操作的令牌。</param>
    /// <returns>表示异步规则执行操作的任务。</returns>
    public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidationResult result;
            if (context.Target is IBusinessObject target)
            {
                var value = target.ReadProperty(Property);
                var serviceProvider = target.BusinessContext?.CurrentServiceProvider;
                var validationContext = new ValidationContext(context.Target, serviceProvider, null);
                result = Attribute.GetValidationResult(value, validationContext);
            }
            else
            {
                var validationContext = new ValidationContext(context.Target, null, null);
                result = Attribute.GetValidationResult(Property.DefaultValue, validationContext);
            }

            if (result != null)
            {
                context.AddErrorResult(result.ErrorMessage);
            }
        }
        catch (Exception exception)
        {
            context.AddErrorResult(exception.Message);
        }

        await Task.CompletedTask;
    }
}