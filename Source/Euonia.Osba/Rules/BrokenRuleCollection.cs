using System.Collections.ObjectModel;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 当前违规规则的集合。
/// </summary>
public class BrokenRuleCollection : ObservableCollection<BrokenRule>
{
	/// <summary>
	/// 用于保护集合访问的锁对象。
	/// </summary>
	private static readonly object _lockObject = new();

	/// <summary>
	/// 获取集合中严重级别为 Error 的违规规则数量。
	/// </summary>
	public int ErrorCount { get; private set; }

    /// <summary>
    /// 获取集合中严重级别为 Warning 的违规规则数量。
    /// </summary>
    public int WarningCount { get; private set; }

    /// <summary>
    /// 获取集合中严重级别为 Information 的违规规则数量。
    /// </summary>
    public int InformationCount { get; private set; }

    /// <summary>
    /// 移除所有先前的结果。
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
	/// 移除给定属性的先前结果。
	/// </summary>
	/// <param name="property">属性信息。</param>
    internal void ClearRules(IPropertyInfo property)
    {
        ClearRules(property?.Name);
    }
	
	/// <summary>
	/// 移除给定属性名称的先前结果。
	/// </summary>
	/// <param name="propertyName">属性名称。</param>
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
	/// 将给定属性名称的结果添加到集合中。
	/// </summary>
	/// <param name="results">规则结果集合。</param>
	/// <param name="propertyName">属性名称。</param>
	/// <exception cref="InvalidOperationException">当结果的描述为空时抛出。</exception>
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

    /// <summary>
    /// 向集合中添加违规规则，并更新对应严重级别的计数。
    /// </summary>
    /// <param name="item">要添加的违规规则。</param>
    private new void Add(BrokenRule item)
    {
        base.Add(item);
        CountOne(item.Severity, 1);
    }

    /// <summary>
    /// 从集合中移除指定索引处的项，并更新对应严重级别的计数。
    /// </summary>
    /// <param name="i">要移除的项的索引。</param>
    private new void RemoveItem(int i)
    {
        CountOne(this[i].Severity, -1);

        base.RemoveItem(i);
    }

    /// <summary>
    /// 按严重级别更新计数。
    /// </summary>
    /// <param name="severity">严重级别。</param>
    /// <param name="one">计数的增量（1 或 -1）。</param>
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