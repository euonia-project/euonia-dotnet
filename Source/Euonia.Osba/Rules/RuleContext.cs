namespace Nerosoft.Euonia.Osba;

/// <inheritdoc />
public class RuleContext : IRuleContext
{
    private readonly Action<IRuleContext> _completeAction;
    private readonly List<RuleResult> _results = new();

    internal RuleContext(Action<IRuleContext> completeAction)
    {
        _completeAction = completeAction;
    }

    /// <inheritdoc />
    public IRuleBase Rule { get; internal set; }

    /// <inheritdoc />
    public object Target { get; internal set; }

    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public string PropertyName { get; internal set; }

    /// <inheritdoc />
    public IReadOnlyList<RuleResult> Results => _results;

    /// <inheritdoc />
    public void AddErrorResult(string description)
    {
        _results.Add(new RuleResult(Rule.Name, description, RuleSeverity.Error));
    }

    /// <inheritdoc />
    public void AddWarningResult(string description)
    {
        _results.Add(new RuleResult(Rule.Name, description, RuleSeverity.Warning));
    }

    /// <inheritdoc />
    public void AddInformationResult(string description)
    {
        _results.Add(new RuleResult(Rule.Name, description, RuleSeverity.Information));
    }

    /// <inheritdoc />
    public void AddSuccessResult()
    {
        _results.Add(new RuleResult(Rule.Name) { Severity = RuleSeverity.Success });
    }

    /// <inheritdoc />
    public void Complete()
    {
        if (Results.Count == 0)
        {
            _results.Add(new RuleResult(Rule.Name));
        }

        _completeAction?.Invoke(this);
    }
}