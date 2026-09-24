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
	/// 指示上下文是否已完成，防止重复完成导致规则结果被重复处理。
	/// </summary>
	private bool _completed;

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
		_results.Add(new RuleResult(Rule.Name, Normalize(description), RuleSeverity.Error));
	}

	/// <inheritdoc />
	public void AddWarningResult(string description)
	{
		_results.Add(new RuleResult(Rule.Name, Normalize(description), RuleSeverity.Warning));
	}

	/// <inheritdoc />
	public void AddInformationResult(string description)
	{
		_results.Add(new RuleResult(Rule.Name, Normalize(description), RuleSeverity.Information));
	}

	/// <inheritdoc />
	public void AddSuccessResult()
	{
		_results.Add(new RuleResult(Rule.Name) { Severity = RuleSeverity.Success });
	}

	/// <summary>
	/// 规范违规描述：空白描述替换为占位消息。
	/// </summary>
	/// <param name="description">规则给出的描述。</param>
	/// <returns>非空白的描述。</returns>
	/// <remarks>
	/// <b>必须在此处补默认消息，不能放行空白描述。</b>
	/// <see cref="RuleResult.Success"/> 由描述是否为空推导，因此
	/// <c>AddErrorResult(null)</c> 或 <c>AddErrorResult("")</c> 会构造出一个「成功」结果，
	/// 被 <see cref="BrokenRuleCollection"/> 跳过——规则明明报了错，对象却被判为有效。
	/// 这是比抛异常更危险的静默失效。
	/// </remarks>
	private string Normalize(string description)
	{
		return string.IsNullOrWhiteSpace(description)
			       ? $"{Resources.IDS_RULE_MESSAGE_REQUIRED} ({Rule.Name})"
			       : description;
	}

	/// <inheritdoc />
	public void Complete()
	{
		if (_completed)
		{
			return;
		}

		_completed = true;

		if (Results.Count == 0)
		{
			_results.Add(new RuleResult(Rule.Name));
		}
	
		_completeAction?.Invoke(this);
	}
}