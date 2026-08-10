using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="IRules"/> 接口的实现。
/// </summary>
public class Rules : IRules
{
	private static readonly object _lockObject = new();

	internal Rules(IHasRuleCheck @object)
	{
		_target = @object;
	}

	private IHasRuleCheck _target;

	/// <inheritdoc />
	public object Target => _target;

	/// <summary>
	/// 获取规则管理器。
	/// </summary>
	internal RuleManager RuleManager
	{
		get
		{
			if (field == null && Target != null)
			{
				field = RuleManager.GetRules(Target.GetType());
			}

			return field;
		}
	}

	/// <summary>
	/// 获取一个值，指示当前是否存在违规规则，存在则意味着对象无效。
	/// </summary>
	public bool IsValid => BrokenRules?.ErrorCount == 0;

	internal BrokenRuleCollection BrokenRules { get; } = new();

	/// <summary>
	/// 获取或设置一个值，指示是否抑制规则检查。
	/// </summary>
	public bool SuppressRuleChecking { get; set; }

	private List<IRuleBase> RunningRules { get; } = new();

	/// <summary>
	/// 获取一个值，指示当前是否有正在运行的规则。
	/// </summary>
	public bool HasRunningRules { get; private set; }

	internal void SetTarget(IHasRuleCheck target)
	{
		_target = target;
	}

	/// <summary>
	/// 向业务规则管理器添加规则。
	/// </summary>
	/// <param name="rule">要添加的规则。</param>
	public void AddRule(IRuleBase rule)
	{
		RuleManager.Rules.Add(rule);
	}

	/// <summary>
	/// 向业务规则管理器添加规则。
	/// </summary>
	/// <typeparam name="TRule">规则类型。</typeparam>
	public void AddRule<TRule>()
		where TRule : class, IRuleBase, new()
	{
		AddRule(new TRule());
	}

	/// <summary>
	/// 向业务规则管理器添加规则。
	/// </summary>
	/// <param name="provider">用于解析规则实例的服务提供程序。</param>
	/// <typeparam name="TRule">规则类型。</typeparam>
	public void AddRule<TRule>(IServiceProvider provider)
		where TRule : class, IRuleBase
	{
		var rule = ActivatorUtilities.GetServiceOrCreateInstance<TRule>(provider);
		AddRule(rule);
	}

	#region Rule check

	/// <summary>
	/// 检查当前对象的规则。
	/// </summary>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <returns>受规则影响且发生变化的属性名称列表。</returns>
	public List<string> CheckObjectRules(bool cascade)
	{
		if (SuppressRuleChecking)
		{
			return new List<string>();
		}

		var currentRunningState = HasRunningRules;
		HasRunningRules = true;
		var rules = RuleManager.Rules
		                       .Where(t => t.Property == null)
		                       .OrderBy(t => t.Priority);
		BrokenRules.ClearRules(null);
		var (properties, tasks) = RunRules(rules, cascade);
		Task.WaitAll(tasks.ToArray());
		HasRunningRules = currentRunningState;
		return properties.Distinct().ToList();
	}

	/// <summary>
	/// 异步检查当前对象的规则。
	/// </summary>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>受规则影响且发生变化的属性名称列表。</returns>
	public async Task<List<string>> CheckObjectRulesAsync(bool cascade, CancellationToken cancellationToken = default)
	{
		if (SuppressRuleChecking)
		{
			return new List<string>();
		}

		var currentRunningState = HasRunningRules;
		HasRunningRules = true;
		var rules = RuleManager.Rules
		                       .Where(t => t.Property == null)
		                       .OrderBy(t => t.Priority);
		BrokenRules.ClearRules(null);
		var (properties, tasks) = RunRules(rules, cascade);
		await Task.WhenAll(tasks);

		HasRunningRules = currentRunningState;
		return properties.Distinct().ToList();
	}

	/// <summary>
	/// 检查指定属性的规则。
	/// </summary>
	/// <param name="property">要检查规则的属性。</param>
	/// <returns>受规则影响且发生变化的属性名称列表。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="property"/> 为 <c>null</c> 时抛出。</exception>
	public List<string> CheckRules(IPropertyInfo property)
	{
		if (property == null)
		{
			throw new ArgumentNullException(nameof(property));
		}

		if (SuppressRuleChecking)
		{
			return new List<string> { property.Name };
		}

		var (properties, tasks) = CheckRulesForProperty(property, true);
		Task.WaitAll(tasks.ToArray());
		return properties.Distinct().ToList();
	}

	/// <summary>
	/// 为指定属性执行所有规则检查逻辑。
	/// </summary>
	/// <param name="property">
	/// 要执行属性规则检查的属性。
	/// </param>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <returns>受影响的属性列表和规则任务列表。</returns>
	private Tuple<List<string>, List<Task>> CheckRulesForProperty(IPropertyInfo property, bool cascade)
	{
		var rules = from rule in RuleManager.Rules
		            where ReferenceEquals(rule.Property, property) // || rule.RelatedProperties.Contains(property)
		            orderby rule.Priority
		            select rule;

		BrokenRules.ClearRules(property);

		return RunRules(rules, cascade);
	}

	/// <summary>
	/// 运行规则检查。
	/// </summary>
	/// <param name="rules">要运行的规则集合。</param>
	/// <param name="cascade">是否级联检查相关属性的规则。</param>
	/// <returns>受影响的属性列表和规则任务列表。</returns>
	private Tuple<List<string>, List<Task>> RunRules(IEnumerable<IRuleBase> rules, bool cascade)
	{
		var affectProperties = new List<string>();
		var tasks = new List<Task>();
		foreach (var rule in rules)
		{
			if (Target is IEditableObject editableObject)
			{
				var attribute = rule.GetType().GetCustomAttribute<ExecuteOnStateAttribute>();

				if (attribute != null && !attribute.States.Contains(editableObject.State))
				{
					continue;
				}
			}

			var context = new RuleContext(ruleContext =>
			{
				lock (_lockObject)
				{
					BrokenRules.Add(ruleContext.Results, ruleContext.Rule.Property?.Name);

					RunningRules.Remove(ruleContext.Rule);

					var properties = Enumerable.Empty<IPropertyInfo>();

					if (ruleContext.Rule.Property != null)
					{
						properties = properties.Append(ruleContext.Rule.Property);
					}

					properties = properties.Concat(ruleContext.Rule.RelatedProperties);

					foreach (var property in properties)
					{
						if (RunningRules.All(r => r.Property != property))
						{
							_target.RuleCheckComplete(property);
						}
					}

					if (!HasRunningRules)
					{
						_target.AllRulesComplete();
					}
				}
			})
			{
				Target = Target,
				Rule = rule,
				PropertyName = rule.Property?.Name
			};

			if (cascade)
			{
				lock (_lockObject)
				{
					foreach (var property in rule.RelatedProperties)
					{
						var (properties, cascadeTasks) = CheckRulesForProperty(property, false);
						affectProperties.AddRange(properties);
						tasks.AddRange(cascadeTasks);
					}
				}
			}

			try
			{
				RunningRules.Add(rule);
				tasks.Add(RunAsync(rule, context));
			}
			catch (Exception ex)
			{
				context.AddErrorResult($"{rule.Name}: {ex.Message}");
				context.Complete();
			}
		}

		return Tuple.Create(affectProperties, tasks);
	}

	/// <summary>
	/// 运行异步规则检查任务。
	/// </summary>
	/// <param name="rule">要执行的规则。</param>
	/// <param name="context">规则上下文。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	private static async Task RunAsync(IRuleBase rule, IRuleContext context, CancellationToken cancellationToken = default)
	{
		try
		{
			await rule.ExecuteAsync(context, cancellationToken);
		}
		catch (Exception ex)
		{
			context.AddErrorResult($"{rule.Name}: {ex.Message}");
		}
		finally
		{
			context.Complete();
		}
	}

	#endregion

	#region DataAnnotations

	/// <summary>
	/// 向业务规则管理器添加数据注解规则。
	/// </summary>
	public void AddDataAnnotations()
	{
		var registeredProperties = ((IBusinessObject)_target).FieldManager.GetRegisteredProperties();

		if (registeredProperties == null || registeredProperties.Count == 0)
		{
			return;
		}

		var properties = _target.GetType().GetRuntimeProperties();

		foreach (var property in properties)
		{
			var registeredProperty = registeredProperties.FirstOrDefault(t => t.Name == property.Name);
			if (registeredProperty == null)
			{
				continue;
			}

			var attributes = property.GetCustomAttributes<ValidationAttribute>(true);
			foreach (var attribute in attributes)
			{
				AddRule(new DataAnnotationRule(registeredProperty, attribute));
			}
		}
	}

	#endregion
}