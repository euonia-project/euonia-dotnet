using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Osba.Tests.Rules;

namespace Nerosoft.Euonia.Osba.Tests;

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
	}

	[FactoryCreate]
	protected internal override async Task CreateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	[FactoryInsert]
	protected internal override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return base.InsertAsync(cancellationToken);
	}

	[FactoryUpdate]
	protected internal override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return base.UpdateAsync(cancellationToken);
	}

	[FactoryDelete]
	protected internal override Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		return base.DeleteAsync(cancellationToken);
	}
}