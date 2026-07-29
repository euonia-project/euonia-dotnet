using Nerosoft.Euonia.Linq;
using Nerosoft.Euonia.Sample.Persist.Entities;

namespace Nerosoft.Euonia.Sample.Persist.Specifications;

/// <summary>
/// The user query specification.
/// </summary>
internal static class UserSpecification
{
	public static readonly Specification<UserEntity> All = new DirectSpecification<UserEntity>(t => t.Id != null);

	/// <summary>
	/// Id equals <paramref name="id"/>.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public static Specification<UserEntity> IdEquals(string id)
	{
		return new DirectSpecification<UserEntity>(t => t.Id == id);
	}

	/// <summary>
	/// Id not equals <paramref name="id"/>.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public static Specification<UserEntity> IdNotEquals(string id)
	{
		return new DirectSpecification<UserEntity>(t => t.Id != id);
	}

	/// <summary>
	/// Username equals <paramref name="username"/>.
	/// </summary>
	/// <param name="username"></param>
	/// <returns></returns>
	public static Specification<UserEntity> UsernameEquals(string username)
	{
		username = username.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Username == username);
	}

	/// <summary>
	/// Username contains <paramref name="username"/>.
	/// </summary>
	/// <param name="username"></param>
	/// <returns></returns>
	public static Specification<UserEntity> UsernameContains(string username)
	{
		username = username.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Username.Contains(username));
	}

	/// <summary>
	/// Nickname equals <paramref name="nickname"/>.
	/// </summary>
	/// <param name="nickname"></param>
	/// <returns></returns>
	public static Specification<UserEntity> NicknameContains(string nickname)
	{
		nickname = nickname.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Nickname.ToLower().Contains(nickname));
	}

	/// <summary>
	/// Email address equals <paramref name="email"/>
	/// </summary>
	/// <param name="email"></param>
	/// <returns></returns>
	public static Specification<UserEntity> EmailEquals(string email)
	{
		email = email.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Email == email);
	}

	public static Specification<UserEntity> EmailContains(string email)
	{
		email = email.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Email.Contains(email));
	}

	public static Specification<UserEntity> PhoneEquals(string phone)
	{
		phone = phone.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Phone == phone);
	}

	public static Specification<UserEntity> PhoneContains(string phone)
	{
		phone = phone.Normalize(TextCaseType.Lower);
		return new DirectSpecification<UserEntity>(t => t.Phone.Contains(phone));
	}

	public static Specification<UserEntity> Matches(string keyword)
	{
		ISpecification<UserEntity>[] specifications =
		[
			UsernameContains(keyword),
			NicknameContains(keyword),
			EmailContains(keyword),
			PhoneContains(keyword)
		];

		return new CompositeSpecification<UserEntity>(PredicateOperator.OrElse, specifications);
	}

	public static Specification<UserEntity> HasRole(string name)
	{
		return new DirectSpecification<UserEntity>(user => user.Roles.Any(role => role.Name == name));
	}
}
