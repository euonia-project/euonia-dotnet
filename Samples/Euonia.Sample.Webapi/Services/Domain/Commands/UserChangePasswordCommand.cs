using System.Security.Cryptography;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Events;
using Nerosoft.Euonia.Sample.Domain.Repositories;
using Nerosoft.Euonia.Sample.Toolkit;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 命令对象：为当前登录用户改密。
/// </summary>
/// <remarks>
/// 旧密码必须先校验通过才换盐换哈希——改密是账号失守后的第一道自助手段，
/// 因此「知道旧密码」本身就是前提；命令成功后经总线发布 <see cref="UserPasswordChangedEvent"/>，
/// 供订阅方（审计、异地登录提醒等）消费。
/// </remarks>
public sealed class UserChangePasswordCommand : CommandObjectBase<UserChangePasswordCommand>
{
	/// <summary>新密码的最小长度。</summary>
	public const int MinPasswordLength = 8;

	/// <summary>被改密的账号标识（由工厂执行时解析当前用户填入）。</summary>
	public string UserId { get; private set; }

	/// <summary>命令是否已成功执行。</summary>
	public bool Changed { get; private set; }

	[FactoryCreate]
	private Task CreateAsync(string oldPassword, string newPassword, CancellationToken cancellationToken = default)
	{
		OldPassword = oldPassword;
		NewPassword = newPassword;
		return Task.CompletedTask;
	}

	/// <summary>当前密码。</summary>
	public string OldPassword { get; private set; }

	/// <summary>新密码。</summary>
	public string NewPassword { get; private set; }

	[FactoryExecute]
	protected override async Task ExecuteAsync(CancellationToken cancellationToken = default)
	{
		var userId = Identity.UserId;
		if (string.IsNullOrWhiteSpace(userId))
		{
			throw new BusinessException("无法识别当前登录用户。");
		}

		var repository = BusinessContext.GetRequiredService<IUserRepository>();
		var user = await repository.GetAsync(userId, false, cancellationToken);
		if (user == null)
		{
			throw new NotFoundException($"User with ID '{userId}' not found.");
		}

		if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < MinPasswordLength)
		{
			throw new BusinessException($"新密码长度不能少于 {MinPasswordLength} 位。");
		}

		if (!Verify(user.PasswordHash, user.PasswordSalt, OldPassword))
		{
			throw new BusinessException("旧密码不正确。");
		}

		// 换盐换哈希：盐每次重新生成，避免相同密码产生相同哈希。
		var salt = RandomUtility.GenerateRandomString(64);
		user.PasswordSalt = salt;
		user.PasswordHash = Cryptography.SHA.Encrypt(salt + NewPassword);
		user.PasswordChangedTime = DateTime.UtcNow;
		await repository.UpdateAsync(user, true, cancellationToken);

		UserId = user.Id;
		await BusinessContext.GetRequiredService<IBus>()
			.PublishAsync(new UserPasswordChangedEvent(user.Id, "change", user.PasswordChangedTime.Value), cancellationToken);

		Changed = true;
	}

	/// <summary>常量时间比对，避免按字节比较泄露密码前缀信息。</summary>
	private static bool Verify(string hash, string salt, string password)
	{
		if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(salt) || password == null)
		{
			return false;
		}

		var candidate = Cryptography.SHA.Encrypt(salt + password);
		var stored = Convert.FromBase64String(hash);
		var input = Convert.FromBase64String(candidate);
		return stored.Length == input.Length && CryptographicOperations.FixedTimeEquals(stored, input);
	}
}