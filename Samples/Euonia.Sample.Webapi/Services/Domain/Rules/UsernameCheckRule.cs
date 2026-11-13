using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Business.Rules;

/// <summary>
/// 表示一条在创建用户期间检查用户名可用性的规则。
/// </summary>
internal sealed class UsernameCheckRule(IPropertyInfo property) : PropertyRuleBase(property)
{
	/// <summary>
	/// 执行规则，以验证用户名是否可用。
	/// </summary>
	/// <param name="context">包含目标对象的规则上下文。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		// 确保目标对象的类型为 UserGeneralBusiness。
		if (context.Target is not IEditableObject target)
		{
			return;
		}

		// 如果操作不是插入，则跳过该规则。
		if (!target.IsNew)
		{
			return;
		}

		var property = Property;
		var value = target.ReadProperty(property)?.ToString();

		if (string.IsNullOrWhiteSpace(value))
		{
			context.AddErrorResult("Username is required.");
		}
		else
		{
			var configuration = target.BusinessContext.GetRequiredService<IConfiguration>();

			// 从配置中获取保留用户名的列表。
			var reserved = configuration.GetValue<List<string>>("ReservedUsernames");

			// 检查用户名是否位于保留列表中。
			if (reserved?.Contains(value, StringComparer.InvariantCultureIgnoreCase) == true)
			{
				// 如果用户名已被保留，则添加错误结果。
				context.AddErrorResult($"Username '{value}' is unavailable.");
				return;
			}

			var repository = target.BusinessContext.GetRequiredService<IUserRepository>();

			// 检查仓储中是否已存在该用户名。
			var exists = await repository.AnyAsync(t => t.Username == value, cancellationToken);
			if (exists)
			{
				// 如果用户名已存在，则添加错误结果。
				context.AddErrorResult($"Username '{value}' is unavailable.");
			}
		}
	}
}