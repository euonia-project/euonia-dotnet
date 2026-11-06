using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Business.Rules;

/// <summary>
/// 表示一条在用户更新期间检查手机号码可用性的规则。
/// </summary>
internal sealed class PhoneNumberCheckRule(IPropertyInfo property) : PropertyRuleBase(property)
{
	/// <summary>
	/// 执行规则，以验证手机号码是否可用。
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

		var property = Property;
		var value = target.ReadProperty(property)?.ToString();

		// 如果手机号码为 null 或空白，则跳过该规则。
		if (string.IsNullOrWhiteSpace(value))
		{
			return;
		}

		// 检查手机属性是否已更改。
		if (Equals(target.FieldManager.GetOriginalValue(property), target.ReadProperty(property)))
		{
			return;
		}

		// 检查该手机号码是否已被其他用户使用。
		var repository = target.BusinessContext.GetRequiredService<IUserRepository>();
		var exists = await repository.AnyAsync(t => t.Phone == value, cancellationToken);
		if (exists)
		{
			// 如果手机号码不可用，则添加错误结果。
			context.AddErrorResult($"Phone number '{value}' is unavailable.");
		}
	}
}