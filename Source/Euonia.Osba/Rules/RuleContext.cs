namespace Nerosoft.Euonia.Osba;

/// <inheritdoc />
public class RuleContext : IRuleContext
{
	/// <summary>
	/// 上下文完成时调用的操作。
	/// </summary>
	private readonly Action<IRuleContext> _completeAction;

	/// <summary>
	/// 存储规则结果的列表。
	/// </summary>
	private readonly List<RuleResult> _results = new();

	/// <summary>
	/// 初始化 <see cref="RuleContext"/> 类的新实例。
	/// </summary>
	/// <param name="completeAction">上下文完成时调用的操作。</param>
	internal RuleContext(Action<IRuleContext> completeAction)
	{
		_completeAction = completeAction;
	}

	/// <inheritdoc />
	public IRuleBase Rule { get; internal set; }

	/// <inheritdoc />
	public object Target { get; internal set; }

	/// <summary>
	/// 获取或设置属性的名称。
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