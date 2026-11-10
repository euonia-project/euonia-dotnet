using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Osba.Tests.Rules;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 用于验证新建态对象级校验规则的业务对象：只声明用户名规则，写操作沿用基类实现。
/// </summary>
public class UserGeneralBusiness : EditableObjectBase<UserGeneralBusiness>
{
	/// <summary>
	/// 用户名属性的注册句柄，供 <see cref="Name"/> 读写与规则校验使用。
	/// </summary>
	public static readonly PropertyInfo<string> NameProperty = RegisterProperty<string>(p => p.Name);

	/// <summary>
	/// 用户名；新建时须通过 <c>UsernameCheckRule</c> 校验。
	/// </summary>
	public string Name
	{
		get => GetProperty(NameProperty);
		set => SetProperty(NameProperty, value);
	}

	/// <summary>
	/// 注册用户名检查规则（<c>UsernameCheckRule</c>），使其参与保存时的校验线。
	/// </summary>
	protected override void AddRules()
	{
		Rules.AddRule<UsernameCheckRule>();
	}

	/// <summary>
	/// 工厂新建钩子：本测试替身不做持久化，直接完成。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示异步操作的任务。</returns>
	[FactoryCreate]
	protected internal override async Task CreateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	/// <summary>
	/// 工厂插入钩子：沿用基类实现，不做额外处理。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示异步操作的任务。</returns>
	[FactoryInsert]
	protected internal override Task InsertAsync(CancellationToken cancellationToken = default)
	{
		return base.InsertAsync(cancellationToken);
	}

	/// <summary>
	/// 工厂更新钩子：沿用基类实现，不做额外处理。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示异步操作的任务。</returns>
	[FactoryUpdate]
	protected internal override Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		return base.UpdateAsync(cancellationToken);
	}

	/// <summary>
	/// 工厂删除钩子：沿用基类实现，不做额外处理。
	/// </summary>
	/// <param name="cancellationToken">取消标记。</param>
	/// <returns>表示异步操作的任务。</returns>
	[FactoryDelete]
	protected internal override Task DeleteAsync(CancellationToken cancellationToken = default)
	{
		return base.DeleteAsync(cancellationToken);
	}
}