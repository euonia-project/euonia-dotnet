using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Business.Rules;
using Nerosoft.Euonia.Sample.Domain.Events;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Domain.Aggregates;

internal class User : EditableObjectBase<User, string>
{
	public static readonly PropertyInfo<string> UsernameProperty = RegisterProperty<string>(p => p.Username);
	public static readonly PropertyInfo<string> PasswordProperty = RegisterProperty<string>(p => p.Password);
	public static readonly PropertyInfo<string> PasswordChangedTimeProperty = RegisterProperty<string>(p => p.PasswordChangedTime);
	public static readonly PropertyInfo<string> NicknameProperty = RegisterProperty<string>(p => p.Nickname);
	public static readonly PropertyInfo<string> EmailProperty = RegisterProperty<string>(p => p.Email);
	public static readonly PropertyInfo<string> PhoneProperty = RegisterProperty<string>(p => p.Phone);
	public static readonly PropertyInfo<string[]> RolesProperty = RegisterProperty<string[]>(p => p.Roles);

	public string Username
	{
		get => GetProperty(UsernameProperty);
		private set => SetProperty(UsernameProperty, value);
	}

	public string Password
	{
		get => GetProperty(PasswordProperty);
		private set => SetProperty(PasswordProperty, value);
	}

	public DateTime? PasswordChangedTime
	{
		get => GetProperty<DateTime?>(PasswordChangedTimeProperty);
		private set => SetProperty(PasswordChangedTimeProperty, value);
	}

	public string Nickname
	{
		get => GetProperty(NicknameProperty);
		set => SetProperty(NicknameProperty, value);
	}

	public string Email
	{
		get => GetProperty(EmailProperty);
		set => SetProperty(EmailProperty, value);
	}

	public string Phone
	{
		get => GetProperty(PhoneProperty);
		set => SetProperty(PhoneProperty, value);
	}

	public string[] Roles
	{
		get => GetProperty(RolesProperty);
		private set => SetProperty(RolesProperty, value);
	}

	protected override void AddRules()
	{
		Rules.AddDataAnnotations();
		Rules.AddRule(new UsernameCheckRule(UsernameProperty));
		Rules.AddRule(new EmailAddressCheckRule(EmailProperty));
		Rules.AddRule(new PhoneNumberCheckRule(PhoneProperty));
		Rules.AddRule(new PasswordStrengthRule(PasswordProperty));
		Rules.AddRule(new CommonRule.Required(UsernameProperty, "Username is required."));
	}

	/// <summary>
	/// Sets the password for the user.
	/// </summary>
	/// <param name="password">The new password.</param>
	/// <param name="actionType">The type of action triggering the password change.</param>
	public void SetPassword(string password, string actionType = null)
	{
		// var salt = RandomUtility.GenerateRandomString();
		// var hash = Cryptography.DES.Encrypt(password, Encoding.UTF8.GetBytes(salt));
		// PasswordHash = hash;
		// PasswordSalt = salt;
		Password = password;
		PasswordChangedTime = DateTime.Now;
		if (!string.IsNullOrWhiteSpace(actionType))
		{
			RaiseEvent(new UserPasswordChangedEvent(Id, actionType, PasswordChangedTime.Value));
		}
	}

	/// <summary>
	/// Sets the roles for the user.
	/// </summary>
	/// <param name="roles">The roles to set.</param>
	internal void SetRoles(params string[] roles)
	{
		if (roles?.Any() != true)
		{
			return;
		}

		Roles = roles.Distinct().ToArray();
	}

	[FactoryCreate]
	private async Task CreateAsync(string username, CancellationToken cancellationToken = default)
	{
		Username = username;
		await Task.CompletedTask;
	}

	[FactoryFetch]
	private async Task FetchAsync(string id, CancellationToken cancellationToken = default)
	{
		var repository = BusinessContext.GetRequiredService<IUserRepository>();
		var user = await repository.GetAsync(id, true, cancellationToken);
		if (user == null)
		{
			throw new InvalidOperationException($"User with ID '{id}' not found.");
		}

		LoadProperty(IdProperty, user.Id);
	}

	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
	}

	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
	}
}