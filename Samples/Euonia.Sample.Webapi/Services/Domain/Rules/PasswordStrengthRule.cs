using System.Text.RegularExpressions;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Sample.Business.Rules;

/// <summary>
/// 表示在创建或更新用户时校验密码强度的规则。
/// </summary>
internal sealed class PasswordStrengthRule(IPropertyInfo property) : PropertyRuleBase(property)
{
	/// <summary>
	/// 用于校验密码强度的正则表达式模式。
	/// 密码必须：
	/// - 至少包含一个小写字母。
	/// - 至少包含一个大写字母。
	/// - 至少包含一个数字。
	/// - 长度介于 8 到 32 个字符之间。
	/// </summary>
	private const string REGEX_PATTERN = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[\x00-\xff]{8,32}$";

	/// <summary>
	/// 执行规则以校验密码强度。
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

		// 在创建用户时校验密码。
		if (target.IsNew)
		{
			// 检查密码是否为空或空白。
			if (string.IsNullOrWhiteSpace(value))
			{
				// 如果密码为必填但缺失，则添加错误结果。
				context.AddErrorResult("Password is required.");
			}
			// 检查密码是否满足强度要求。
			else if (!Regex.IsMatch(value, REGEX_PATTERN))
			{
				// 如果密码不满足强度要求，则添加错误结果。
				context.AddErrorResult("Password does not meet strength requirements.");
			}
		}
		// 在更新用户时校验密码。
		else if (target.IsChanged && !string.IsNullOrEmpty(value) && !Regex.IsMatch(value, REGEX_PATTERN))
		{
			// 如果更新后的密码不满足强度要求，则添加错误结果。
			context.AddErrorResult("Password does not meet strength requirements.");
		}

		// 完成任务。
		await Task.CompletedTask;
	}
}