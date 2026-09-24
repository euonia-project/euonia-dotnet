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
	private readonly object _lockObject = new();

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
            // 计数由 ClearItems 覆写维护
            Clear();
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
    internal void Add(IEnumerable<RuleResult> results, string propertyName)
    {
        lock (_lockObject)
        {
            foreach (var result in results)
            {
                if (result.Success)
                {
                    continue;
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

    #region Counting

    /// <summary>
    /// 在指定索引处插入违规规则，并更新对应严重级别的计数。
    /// </summary>
    /// <param name="index">插入位置。</param>
    /// <param name="item">要插入的违规规则。</param>
    /// <remarks>
    /// 计数改由覆写 <see cref="ObservableCollection{T}.InsertItem"/> /
    /// <see cref="ObservableCollection{T}.RemoveItem"/> / <see cref="ObservableCollection{T}.SetItem"/> /
    /// <see cref="ObservableCollection{T}.ClearItems"/> 维护。此前用私有 <c>new</c> 方法遮蔽基类成员，
    /// 只覆盖了类内部的调用；外部通过 <see cref="Collection{T}.Clear"/> /
    /// <see cref="Collection{T}.Remove"/> / 索引器改动集合时计数不会更新——例如
    /// <c>GetBrokenRules().Clear()</c> 会清空条目却留下 <see cref="ErrorCount"/>，
    /// 令 <c>IsValid</c> 永久为 <see langword="false"/>，后续每次保存都抛验证异常。
    /// </remarks>
    protected override void InsertItem(int index, BrokenRule item)
    {
        base.InsertItem(index, item);
        CountOne(item.Severity, 1);
    }

    /// <inheritdoc cref="InsertItem"/>
    protected override void RemoveItem(int index)
    {
        CountOne(this[index].Severity, -1);
        base.RemoveItem(index);
    }

    /// <inheritdoc cref="InsertItem"/>
    protected override void SetItem(int index, BrokenRule item)
    {
        CountOne(this[index].Severity, -1);
        base.SetItem(index, item);
        CountOne(item.Severity, 1);
    }

    /// <inheritdoc cref="InsertItem"/>
    protected override void ClearItems()
    {
        base.ClearItems();
        ErrorCount = WarningCount = InformationCount = 0;
    }

    /// <summary>
    /// 按严重级别更新计数。
    /// </summary>
    /// <param name="severity">严重级别。</param>
    /// <param name="one">计数的增量（1 或 -1）。</param>
    /// <remarks>
    /// <see cref="RuleSeverity.Success"/> 不参与计数（它不表示违规），直接忽略——
    /// 此前会抛 <see cref="Exception"/>，而调用点在规则完成的回调里，会让整轮检查以
    /// 难以定位的异常失败。
    /// </remarks>
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
                break;
        }
    }

    #endregion
}