namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Context information provided to a business rule when it is invoked.
/// </summary>
public interface IRuleContext
{
    /// <summary>
    /// Gets the rule object.
    /// </summary>
    IRuleBase Rule { get; }

    /// <summary>
    /// Gets a reference to the target business object.
    /// </summary>
    object Target { get; }

    /// <summary>
    /// Gets the rule check results.
    /// </summary>
    IReadOnlyList<RuleResult> Results { get; }

    /// <summary>
    /// Adds an error result to the rule context.
    /// </summary>
    /// <param name="description"></param>
    void AddErrorResult(string description);

    /// <summary>
    /// Adds a warning result to the rule context.
    /// </summary>
    /// <param name="description"></param>
    void AddWarningResult(string description);

    /// <summary>
    /// Adds an information result to the rule context.
    /// </summary>
    /// <param name="description"></param>
    void AddInformationResult(string description);

    /// <summary>
    /// Adds a success result to the rule context.
    /// </summary>
    void AddSuccessResult();

    /// <summary>
    /// Completes the rule context.
    /// </summary>
    void Complete();
}