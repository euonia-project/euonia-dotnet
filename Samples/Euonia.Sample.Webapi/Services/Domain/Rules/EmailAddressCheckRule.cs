using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Business.Rules;

/// <summary>
/// 表示在更新用户时检查电子邮件地址是否可用的规则。
/// </summary>
internal sealed class EmailAddressCheckRule : PropertyRuleBase
{
	private readonly string? _ignoreId;

	/// <summary>
	/// 初始化电子邮件地址唯一性规则。
	/// </summary>
	/// <param name="property">要校验的电子邮件地址属性。</param>
	/// <param name="ignoreId">校验时排除的用户标识；更新场景传入当前用户，避免与其自身冲突。</param>
	public EmailAddressCheckRule(IPropertyInfo property, string? ignoreId = null)
		: base(property)
	{
		_ignoreId = ignoreId;
	}

	/// <summary>
	/// 执行规则以验证电子邮件地址是否可用。
	/// </summary>
	/// <param name="context">包含目标对象的规则上下文。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		// 确保目标对象为 IEditableObject 类型。
		if (context.Target is not IEditableObject target)
		{
			return;
		}

		var property = Property;
		var value = target.ReadProperty(property)?.ToString();

		// 如果电子邮件地址为空或空白，则跳过该规则。
		if (string.IsNullOrWhiteSpace(value))
		{
			return;
		}

		// 检查电子邮件属性是否已更改。
		if (Equals(target.FieldManager.GetOriginalValue(property), target.ReadProperty(property)))
		{
			return;
		}

		// 检查该电子邮件地址是否已被其他用户使用。

		var repository = target.BusinessContext.GetRequiredService<IUserRepository>();

		var exists = await repository.AnyAsync(t => t.Email == value && t.Id != _ignoreId, cancellationToken);
		if (exists)
		{
			// 如果电子邮件地址不可用，则添加错误结果。
			context.AddErrorResult($"Email address '{value}' is unavailable.");
		}
	}
}