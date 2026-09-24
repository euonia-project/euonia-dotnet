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
                result = Attribute.GetValidationResult(value, CreateContext(context.Target, serviceProvider));
            }
            else
            {
                result = Attribute.GetValidationResult(Property.DefaultValue, CreateContext(context.Target, null));
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

    /// <summary>
    /// 创建校验上下文，并把<b>字段名</b>填进去。
    /// </summary>
    /// <param name="instance">被校验的对象实例。</param>
    /// <param name="serviceProvider">服务提供程序。</param>
    /// <returns>校验上下文。</returns>
    /// <remarks>
    /// 必须显式设置 <see cref="ValidationContext.DisplayName"/> 与 <see cref="ValidationContext.MemberName"/>：
    /// <see cref="ValidationContext"/> 由实例构造时，<c>DisplayName</c> 默认取<b>对象类型名</b>，
    /// 于是 <c>[Required(ErrorMessage = "{0} 不能为空。")]</c> 这类消息会把占位符填成业务对象的名字
    /// （「Repo 不能为空」）而不是字段名——校验结果本身就是给表单用的，字段名错了就失去了意义。
    /// </remarks>
    private ValidationContext CreateContext(object instance, IServiceProvider serviceProvider)
    {
        return new ValidationContext(instance, serviceProvider, null)
        {
            DisplayName = Property.FriendlyName ?? Property.Name,
            MemberName = Property.Name
        };
    }
}