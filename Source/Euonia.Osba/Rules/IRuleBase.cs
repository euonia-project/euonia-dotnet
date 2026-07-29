namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 
/// </summary>
public interface IRuleBase
{
    /// <summary>
    /// Gets a unique name for the specific instance
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the property affected by this rule.
    /// </summary>
    IPropertyInfo Property { get; }

    /// <summary>
    /// Gets the related properties.
    /// </summary>
    List<IPropertyInfo> RelatedProperties { get; }

    /// <summary>
    /// Gets the rule priority.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Business or validation rule implementation.
    /// </summary>
    /// <param name="context">Rule context object.</param>
    /// <param name="cancellationToken"></param>
    Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default);
}