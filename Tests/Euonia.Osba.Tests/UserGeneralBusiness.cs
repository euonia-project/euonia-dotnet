using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Core.Tests.Rules;

namespace Nerosoft.Euonia.Core.Tests;

public class UserGeneralBusiness : EditableObjectBase<UserGeneralBusiness>
{
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	protected override void AddRules()
	{
		Rules.AddRule<UsernameCheckRule>();
		Rules.AddRule<PermissionCheckRule>();
	}

	[FactoryCreate]
	protected override async Task CreateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryInsert]
	protected override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return base.InsertAsync(cancellationToken);
	}

	[FactoryUpdate]
	protected override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return base.UpdateAsync(cancellationToken);
	}

	[FactoryDelete]
	protected override Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		return base.DeleteAsync(cancellationToken);
	}
}