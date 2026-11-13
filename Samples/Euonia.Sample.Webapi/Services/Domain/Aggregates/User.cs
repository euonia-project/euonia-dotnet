using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Business.Rules;
using Nerosoft.Euonia.Sample.Domain.Events;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Persist.Entities;
using Nerosoft.Euonia.Sample.Toolkit;

namespace Nerosoft.Euonia.Sample.Domain.Aggregates;

/// <summary>
/// 用户聚合根：承载账号资料、口令变更与角色集合，并通过工厂方法完成创建、加载、插入与更新。
/// </summary>
internal class User : EditableObjectBase<User, string>
{
	/// <summary>
	/// 用户名的属性元数据，供属性系统、规则与持久化映射共享同一属性标识。
	/// </summary>
	public static readonly PropertyInfo<string> UsernameProperty = RegisterProperty<string>(p => p.Username);

	/// <summary>
	/// 口令的明文属性元数据；空值由密码强度规则负责校验。
	/// </summary>
	public static readonly PropertyInfo<string> PasswordProperty = RegisterProperty<string>(p => p.Password);

	/// <summary>
	/// 口令最近一次变更时间的属性元数据，供密码过期策略读取。
	/// </summary>
	public static readonly PropertyInfo<DateTime?> PasswordChangedTimeProperty = RegisterProperty<DateTime?>(p => p.PasswordChangedTime);

	/// <summary>
	/// 昵称的属性元数据，昵称不参与唯一性校验。
	/// </summary>
	public static readonly PropertyInfo<string> NicknameProperty = RegisterProperty<string>(p => p.Nickname);

	/// <summary>
	/// 电子邮件地址的属性元数据，唯一性由 <see cref="EmailAddressCheckRule"/> 校验。
	/// </summary>
	public static readonly PropertyInfo<string> EmailProperty = RegisterProperty<string>(p => p.Email);

	/// <summary>
	/// 联系电话的属性元数据，格式由电话号码规则校验。
	/// </summary>
	public static readonly PropertyInfo<string> PhoneProperty = RegisterProperty<string>(p => p.Phone);

	/// <summary>
	/// 角色名称集合的属性元数据，角色经由 <see cref="SetRoles"/> 去重后写入。
	/// </summary>
	public static readonly PropertyInfo<string[]> RolesProperty = RegisterProperty<string[]>(p => p.Roles);

	/// <summary>
	/// 获取用户名；用户名只能由工厂创建方法设置，创建后对外只读。
	/// </summary>
	public string Username
	{
		get => GetProperty(UsernameProperty);
		private set => SetProperty(UsernameProperty, value);
	}

	/// <summary>
	/// 获取聚合暂存的口令明文；仅供规则校验与插入时取哈希使用，不直接持久化。
	/// </summary>
	public string Password
	{
		get => GetProperty(PasswordProperty);
		private set => SetProperty(PasswordProperty, value);
	}

	/// <summary>
	/// 获取口令最近一次变更时间；尚未变更过时为 <see langword="null"/>。
	/// </summary>
	public DateTime? PasswordChangedTime
	{
		get => GetProperty<DateTime?>(PasswordChangedTimeProperty);
		private set => SetProperty(PasswordChangedTimeProperty, value);
	}

	/// <summary>
	/// 获取或设置昵称，用于界面展示。
	/// </summary>
	public string Nickname
	{
		get => GetProperty(NicknameProperty);
		set => SetProperty(NicknameProperty, value);
	}

	/// <summary>
	/// 获取或设置电子邮件地址；写入时由数据注解与唯一性规则共同校验。
	/// </summary>
	public string Email
	{
		get => GetProperty(EmailProperty);
		set => SetProperty(EmailProperty, value);
	}

	/// <summary>
	/// 获取或设置联系电话；格式由电话号码规则校验。
	/// </summary>
	public string Phone
	{
		get => GetProperty(PhoneProperty);
		set => SetProperty(PhoneProperty, value);
	}

	/// <summary>
	/// 获取角色名称集合；集合只能通过 <see cref="SetRoles"/> 整体替换，避免绕过去重逻辑。
	/// </summary>
	public string[] Roles
	{
		get => GetProperty(RolesProperty);
		private set => SetProperty(RolesProperty, value);
	}

	/// <inheritdoc/>
	/// <remarks>注册数据注解、用户名、邮箱、电话与密码强度规则，并要求用户名必填。</remarks>
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
	/// 设置用户的密码。
	/// </summary>
	/// <param name="password">新密码。</param>
	/// <param name="actionType">触发密码变更的操作类型。</param>
	public void SetPassword(string password, string? actionType = null)
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
	/// 设置用户的角色。
	/// </summary>
	/// <param name="roles">要设置的角色。</param>
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
		Id = Guid.NewGuid().ToString("N");
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

	/// <inheritdoc/>
	/// <remarks>插入时会重新生成盐并计算哈希，聚合暂存的明文口令不会进入持久层。</remarks>
	[FactoryInsert]
	protected override async Task InsertAsync(CancellationToken cancellationToken = default)
	{
		// 聚合只暂持明文口令以便强度规则校验；落库时重新加盐取哈希，明文不进入持久层。
		var salt = RandomUtility.GenerateRandomString(64);
		var entity = UserEntity.Create(Id, Username, Nickname, Cryptography.SHA.Encrypt(salt + Password), salt);
		entity.SetEmail(Email);
		entity.SetPhone(Phone);
		await BusinessContext.GetRequiredService<IUserRepository>().InsertAsync(entity, true, cancellationToken);
	}

	/// <inheritdoc/>
	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
	}
}