using System.ComponentModel.DataAnnotations;

using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Represents a rule based on a <see cref="ValidationAttribute"/>.
/// </summary>
public class DataAnnotationRule : RuleBase
{
    /// <summary>
    /// Gets the validation attribute.
    /// </summary>
    public ValidationAttribute Attribute { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataAnnotationRule"/> class.
    /// </summary>
    /// <param name="property"></param>
    /// <param name="attribute"></param>
    public DataAnnotationRule(IPropertyInfo property, ValidationAttribute attribute)
        : base(property, attribute.GetType())
    {
        Attribute = attribute;
    }

    /// <summary>
    /// Executes the rule check.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidationResult result;
            if (context.Target is IBusinessObject target)
            {
                var value = target.ReadProperty(Property);
                var validationContext = new ValidationContext(context.Target, target.BusinessContext.CurrentServiceProvider, null);
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