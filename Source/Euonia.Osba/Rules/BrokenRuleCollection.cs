using System.Collections.ObjectModel;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// A collection of currently broken rules.
/// </summary>
public class BrokenRuleCollection : ObservableCollection<BrokenRule>
{
    private static readonly object _lockObject = new();

	/// <summary>
	/// Gets the number of broken rules in the collection that have a severity of Error.
	/// </summary>
	public int ErrorCount { get; private set; }

    /// <summary>
    /// Gets the number of broken rules in the collection that have a severity of Warning.
    /// </summary>
    public int WarningCount { get; private set; }

    /// <summary>
    /// Gets the number of broken rules in the collection that have a severity of Information.
    /// </summary>
    public int InformationCount { get; private set; }

    /// <summary>
    /// Remove all previous results.
    /// </summary>
    internal void ClearRules()
    {
        lock (_lockObject)
        {
            Clear();
            ErrorCount = WarningCount = InformationCount = 0;
        }
    }
	
	/// <summary>
	/// Remove the previous result for the given property.
	/// </summary>
	/// <param name="property"></param>
    internal void ClearRules(IPropertyInfo property)
    {
        ClearRules(property?.Name);
    }
	
	/// <summary>
	/// Remove the previous result for the given property name.
	/// </summary>
	/// <param name="propertyName"></param>
    private void ClearRules(string propertyName)
    {
        lock (_lockObject)
        {
            var count = Count;
            for (var index = 0; index < count;)
            {
                var rule = this[index];
                if (rule.Property != propertyName)
                {
                    index++;
                }
                else
                {
	                RemoveItem(index);
	                count--;
                }
            }
        }
    }

	/// <summary>
	/// Adds the results to the collection for the given property name.
	/// </summary>
	/// <param name="results"></param>
	/// <param name="propertyName"></param>
	/// <exception cref="ArgumentNullException"></exception>
    internal void Add(IEnumerable<RuleResult> results, string propertyName)
    {
        lock (_lockObject)
        {
            foreach (var result in results)
            {
                //ClearRules(propertyName);
                if (result.Success)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(result.Description))
                {
	                throw new InvalidOperationException(Resources.IDS_RULE_MESSAGE_REQUIRED);
                }

                var rule = new BrokenRule
                {
                    Description = result.Description,
                    Severity = result.Severity,
                    Property = propertyName
                };

                Add(rule);
            }
        }
    }

    private new void Add(BrokenRule item)
    {
        base.Add(item);
        CountOne(item.Severity, 1);
    }

    private new void RemoveItem(int i)
    {
        CountOne(this[i].Severity, -1);

        base.RemoveItem(i);
    }

    private void CountOne(RuleSeverity severity, int one)
    {
        switch (severity)
        {
            case RuleSeverity.Error:
                ErrorCount += one;
                break;
            case RuleSeverity.Warning:
                WarningCount += one;
                break;
            case RuleSeverity.Information:
                InformationCount += one;
                break;
            case RuleSeverity.Success:
            default:
                throw new Exception("Unhandled severity=" + severity);
        }
    }
}