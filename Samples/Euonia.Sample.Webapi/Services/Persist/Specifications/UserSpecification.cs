using Nerosoft.Euonia.Linq;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Persist.Specifications;

/// <summary>
/// 用户查询规约。
/// </summary>
internal static class UserSpecification
{
	/// <summary>
	/// 匹配标识非 <c>null</c> 的全部用户，可用作不附加筛选条件的基准规约。
	/// </summary>
	public static readonly Specification<UserEntity> All = new DirectSpecification<UserEntity>(t => t.Id != null);

	/// <summary>
	/// Id 等于 <paramref name="id"/>。
	/// </summary>
	/// <param name="id">要比较的用户标识。</param>
	/// <returns>用于筛选 Id 等于 <paramref name="id"/> 的用户的规约。</returns>
	public static Specification<UserEntity> IdEquals(string id)
	{
		return new DirectSpecification<UserEntity>(t => t.Id == id);
	}

	/// <summary>
	/// Id 不等于 <paramref name="id"/>。
	/// </summary>
	/// <param name="id">要排除的用户标识。</param>
	/// <returns>用于筛选 Id 不等于 <paramref name="id"/> 的用户的规约。</returns>
	public static Specification<UserEntity> IdNotEquals(string id)
	{
		return new DirectSpecification<UserEntity>(t => t.Id != id);
	}

	/// <summary>
	/// 用户名等于 <paramref name="username"/>。
	/// </summary>
	/// <param name="username">要匹配的用户名；比较前会统一转换为小写。</param>
	/// <returns>用于筛选用户名等于 <paramref name="username"/> 的用户的规约。</returns>
	public static Specification<UserEntity> UsernameEquals(string username)
	{
		username = username.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Username == username);
	}

	/// <summary>
	/// 用户名包含 <paramref name="username"/>。
	/// </summary>
	/// <param name="username">要匹配的用户名片段；比较前会统一转换为小写。</param>
	/// <returns>用于筛选用户名包含 <paramref name="username"/> 的用户的规约。</returns>
	public static Specification<UserEntity> UsernameContains(string username)
	{
		username = username.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Username.Contains(username));
	}

	/// <summary>
	/// 昵称包含 <paramref name="nickname"/>。
	/// </summary>
	/// <param name="nickname">要匹配的昵称片段；比较前会统一转换为小写。</param>
	/// <returns>用于筛选昵称包含 <paramref name="nickname"/> 的用户的规约。</returns>
	public static Specification<UserEntity> NicknameContains(string nickname)
	{
		nickname = nickname.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Nickname.ToLower().Contains(nickname));
	}

	/// <summary>
	/// 电子邮件地址等于 <paramref name="email"/>。
	/// </summary>
	/// <param name="email">要匹配的电子邮件地址；比较前会统一转换为小写。</param>
	/// <returns>用于筛选电子邮件地址等于 <paramref name="email"/> 的用户的规约。</returns>
	public static Specification<UserEntity> EmailEquals(string email)
	{
		email = email.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Email == email);
	}

	/// <summary>
	/// 电子邮件地址包含 <paramref name="email"/>。
	/// </summary>
	/// <param name="email">要匹配的电子邮件地址片段；比较前会统一转换为小写。</param>
	/// <returns>用于筛选电子邮件地址包含 <paramref name="email"/> 的用户的规约。</returns>
	public static Specification<UserEntity> EmailContains(string email)
	{
		email = email.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Email.Contains(email));
	}

	/// <summary>
	/// 电话号码等于 <paramref name="phone"/>。
	/// </summary>
	/// <param name="phone">要匹配的电话号码；比较前会统一转换为小写。</param>
	/// <returns>用于筛选电话号码等于 <paramref name="phone"/> 的用户的规约。</returns>
	public static Specification<UserEntity> PhoneEquals(string phone)
	{
		phone = phone.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Phone == phone);
	}

	/// <summary>
	/// 电话号码包含 <paramref name="phone"/>。
	/// </summary>
	/// <param name="phone">要匹配的电话号码片段；比较前会统一转换为小写。</param>
	/// <returns>用于筛选电话号码包含 <paramref name="phone"/> 的用户的规约。</returns>
	public static Specification<UserEntity> PhoneContains(string phone)
	{
		phone = phone.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Phone.Contains(phone));
	}

	/// <summary>
	/// 匹配用户名、昵称、电子邮件地址或电话号码中包含 <paramref name="keyword"/> 的任一用户。
	/// </summary>
	/// <param name="keyword">要匹配的关键字，会分别用于用户名、昵称、电子邮件地址与电话号码的包含匹配。</param>
	/// <returns>由上述四个包含规约以“或”关系组合而成的规约。</returns>
	public static Specification<UserEntity> Matches(string keyword)
	{
		ISpecification<UserEntity>[] specifications =
		[
			UsernameContains(keyword),
			NicknameContains(keyword),
			EmailContains(keyword),
			PhoneContains(keyword)
		];

		return new CompositeSpecification<UserEntity>(PredicateOperator.OrElse).With(specifications);
	}

	/// <summary>
	/// 匹配拥有指定名称角色的用户。
	/// </summary>
	/// <param name="name">要匹配的角色名称，需与用户所属角色的名称完全相等。</param>
	/// <returns>用于筛选拥有该角色名称的用户的规约。</returns>
	public static Specification<UserEntity> HasRole(string name)
	{
		return new DirectSpecification<UserEntity>(user => user.Roles.Any(role => role.Name == name));
	}
}
