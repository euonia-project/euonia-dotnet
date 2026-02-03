using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Implementation of <see cref="IRules"/> interface.
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
	/// Gets the rule manager.
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
	/// Gets a value indicating whether there are any currently broken rules, which would mean the object is not valid.
	/// </summary>
	public bool IsValid => BrokenRules?.ErrorCount == 0;

	internal BrokenRuleCollection BrokenRules { get; } = new();

	/// <summary>
	/// Gets or sets a value indicating whether to suppress rule checking.
	/// </summary>
	public bool SuppressRuleChecking { get; set; }

	private List<IRuleBase> RunningRules { get; } = new();

	/// <summary>
	/// Gets a value indicating whether there are any currently running rules.
	/// </summary>
	public bool HasRunningRules { get; private set; }

	internal void SetTarget(IHasRuleCheck target)
	{
		_target = target;
	}

	/// <summary>
	/// Add rule to business rule manager.
	/// </summary>
	/// <param name="rule"></param>
	public void AddRule(IRuleBase rule)
	{
		RuleManager.Rules.Add(rule);
	}

	/// <summary>
	/// Add rule to business rule manager.
	/// </summary>
	/// <typeparam name="TRule"></typeparam>
	public void AddRule<TRule>()
		where TRule : class, IRuleBase, new()
	{
		AddRule(new TRule());
	}

	/// <summary>
	/// Add rule to business rule manager.
	/// </summary>
	/// <param name="provider"></param>
	/// <typeparam name="TRule"></typeparam>
	public void AddRule<TRule>(IServiceProvider provider)
		where TRule : class, IRuleBase
	{
		var rule = ActivatorUtilities.GetServiceOrCreateInstance<TRule>(provider);
		AddRule(rule);
	}

	#region Rule check

	/// <summary>
	/// Check rule for current object.
	/// </summary>
	/// <param name="cascade"></param>
	/// <returns></returns>
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
	/// 
	/// </summary>
	/// <param name="cascade"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
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
	/// Check rule for specified property.
	/// </summary>
	/// <param name="property"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
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
	/// Execute all rules check logic for specified property.
	/// </summary>
	/// <param name="property">
	/// The property to execute property rule check.
	/// </param>
	/// <param name="cascade"></param>
	/// <returns></returns>
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
	/// Run rule checks.
	/// </summary>
	/// <param name="rules"></param>
	/// <param name="cascade"></param>
	/// <returns></returns>
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
	/// Run a async rule check job.
	/// </summary>
	/// <param name="rule"></param>
	/// <param name="context"></param>
	/// <param name="cancellationToken"></param>
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
	/// Add data annotations to business rule manager.
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