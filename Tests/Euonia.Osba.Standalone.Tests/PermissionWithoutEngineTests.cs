using System.Reflection;
using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Core.Tests;

/// <summary>
/// 不引入策略引擎时的权限：Osba 的契约由宿主自己实现（这里模拟「权限码来自配置表」）。
/// <para>
/// 本项目<b>只引用 <c>Euonia.Osba</c></b>——引用图即守卫：Osba 一旦需要引擎才能编译，这里就编译不过。
/// </para>
/// </summary>
public class PermissionWithoutEngineTests
{
	[Fact]
	public void Osba_Assembly_Should_Not_Reference_Security()
	{
		var referenced = typeof(BusinessObject).Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name).ToArray();

		Assert.DoesNotContain("Euonia.Security", referenced);

		// 直接引用之外，间接引用同样会把它拖回来（例如某个依赖突然引用了引擎）
		Assert.DoesNotContain("Euonia.Security", TransitiveReferences(typeof(BusinessObject).Assembly));
	}

	[Fact]
	public async Task Custom_Implementation_Should_Deny_When_Requirement_Is_Not_Granted()
	{
		using var scope = CreateScope(codes: [], out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();
		var entity = GroundedEntity("host:update");
		entity.BusinessContext = provider.GetRequiredService<BusinessContext>();
		entity.BusinessContext = provider.GetRequiredService<BusinessContext>();

		await Assert.ThrowsAsync<SecurityException>(
			() => factory.SaveAsync(entity, TestContext.Current.CancellationToken));

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Custom_Implementation_Should_Allow_When_Requirement_Is_Granted()
	{
		using var scope = CreateScope(codes: ["host:update"], out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();
		var entity = GroundedEntity("host:update");
		entity.BusinessContext = provider.GetRequiredService<BusinessContext>();
		entity.BusinessContext = provider.GetRequiredService<BusinessContext>();

		var saved = await factory.SaveAsync(entity, TestContext.Current.CancellationToken);

		Assert.Same(entity, saved);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Requirements_Without_Checker_Should_FailLoudly_Not_Silently_Allow()
	{
		// 声明了要求却没人判定 —— 必须报错；CanUpdateObject 仍是查询（返回 true），闸门才是判定
		using var scope = CreateScope(codes: null, out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();
		var entity = GroundedEntity("host:update");
		entity.BusinessContext = provider.GetRequiredService<BusinessContext>();
		entity.BusinessContext = provider.GetRequiredService<BusinessContext>();

		entity.BusinessContext = provider.GetRequiredService<BusinessContext>();

		Assert.True(entity.CanUpdateObject());   // 查询语义：无从判定时返回 true

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => factory.SaveAsync(entity, TestContext.Current.CancellationToken));

		Assert.Contains(nameof(IOperationPermissionChecker), exception.Message);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Unwired_Object_With_Declared_Requirement_Should_FailLoudly()
	{
		// 声明了要求、对象却没接线：不能因为「拿不到上下文」就当作没有要求（默认来源兜底扫描声明）
		using var scope = CreateScope(codes: [], out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();
		var entity = GroundedEntity("host:update");   // 刻意不设 BusinessContext

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => factory.SaveAsync(entity, TestContext.Current.CancellationToken));

		Assert.Contains("BusinessContext", exception.Message);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public async Task Object_Without_Requirements_Should_Not_Need_Any_Wiring()
	{
		using var scope = CreateScope(codes: [], out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();
		var entity = new PlainEntity { Name = "free" };
		entity.BusinessContext = provider.GetRequiredService<BusinessContext>();
		entity.MarkAsChanged();

		var saved = await factory.SaveAsync(entity, TestContext.Current.CancellationToken);

		Assert.Same(entity, saved);

		BusinessContextAccessor.Clear();
	}

	[Fact]
	public void Custom_Row_Authorizer_Should_Drive_Row_Queries_And_The_Gate()
	{
		using var scope = CreateScope(codes: ["host:update"], out var provider);

		var factory = provider.GetRequiredService<IObjectFactory>();
		var visible = GroundedEntity("host:read");
		visible.BusinessContext = provider.GetRequiredService<BusinessContext>();

		Assert.True(visible.CanSeeOwnRow());                  // 授权器放行
		Assert.Equal("可见", visible.ExplainOwnRow());

		// 工厂边界同样走授权器：受约束的对象没接线时必须报错，而不是静默放行
		var unwired = GroundedEntity("host:read");
		var exception = Assert.Throws<InvalidOperationException>(
			() => factory.SaveAsync(unwired, TestContext.Current.CancellationToken).GetAwaiter().GetResult());

		Assert.Contains("BusinessContext", exception.Message);

		BusinessContextAccessor.Clear();
	}

	#region 装配

	private static IServiceScope CreateScope(string[] codes, out IServiceProvider provider)
	{
		var services = new ServiceCollection();

		services.AddBusinessObject(typeof(GroundedEntity).Assembly);

		// 宿主的实现：要求来自「配置表」（这里的 GroundedEntity 声明了 host:* 码），
		// 判定来自当前用户的权限码集合（现实里可能是配置、权限表或已有鉴权框架）
		services.AddSingleton<IPermissionRequirementProvider, TableRequirementProvider>();
		services.AddSingleton<IObjectScopeAuthorizer, VisibleScopeAuthorizer>();

		if (codes != null)
		{
			services.AddSingleton<IOperationPermissionChecker>(new CodeSetChecker(codes));
		}

		services.AddSingleton(User("dev"));

		var built = services.BuildServiceProvider();
		var scope = built.CreateScope();

		BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
		provider = scope.ServiceProvider;

		return scope;
	}

	private static GroundedEntity GroundedEntity(string code)
	{
		var entity = new GroundedEntity { Name = "grounded", RequiredCode = code };

		// 有「待保存的变更」才会走到工厂边界（否则工厂直接返回）
		entity.MarkAsChanged();

		return entity;
	}

	private static UserPrincipal User(string userId)
	{
		var identity = new ClaimsIdentity(
			[new Claim(UserClaimTypes.Subject, userId)],
			"Bearer",
			ClaimTypes.Name,
			UserClaimTypes.Role);

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	private static string[] TransitiveReferences(Assembly assembly, HashSet<string> visited = null)
	{
		visited ??= [];

		var result = new List<string>();

		foreach (var reference in assembly.GetReferencedAssemblies())
		{
			if (!visited.Add(reference.Name))
			{
				continue;
			}

			result.Add(reference.Name);

			if (Assembly.Load(reference) is { } loaded)
			{
				result.AddRange(TransitiveReferences(loaded, visited));
			}
		}

		return [.. result];
	}

	#endregion
}

#region 受控资源与宿主实现

/// <summary>要求由实体自己声明（<c>[Permission]</c>），判定由宿主实现。</summary>
public class GroundedEntity : EditableObject<GroundedEntity>
{
	public string Name { get; set; }

	/// <summary>本实体在更新操作上要求的权限码，由宿主的要求来源读取。</summary>
	public string RequiredCode { get; set; }

	/// <summary>业务方法内的行级分支：protected 的 CanAccessRow 只能在派生类型里调用。</summary>
	public bool CanSeeOwnRow() => CanAccessRow();

	/// <summary>业务方法内的行级判定说明。</summary>
	public string ExplainOwnRow() => ExplainRowAccess();

	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>没有任何权限要求的实体：不需要任何权限装配。</summary>
public class PlainEntity : EditableObject<PlainEntity>
{
	public string Name { get; set; }

	[FactoryUpdate]
	protected override async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}
}

/// <summary>把「要求」当作数据提供的来源（模拟配置表 / 权限表）。</summary>
internal sealed class TableRequirementProvider : IPermissionRequirementProvider
{
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
	{
		// 只对更新操作提要求，且要求写在实体数据上——真实宿主这里会查配置或权限表
		return operation == BusinessOperation.Update
			? [new PermissionAttribute(type == typeof(GroundedEntity) ? "host:update" : string.Empty)]
			: [];
	}
}

/// <summary>按当前用户持有的权限码集合判定（支持末尾 <c>*</c> 通配）。</summary>
internal sealed class CodeSetChecker(string[] codes) : IOperationPermissionChecker
{
	public bool IsGranted(string permission)
	{
		if (string.IsNullOrEmpty(permission))
		{
			return true;
		}

		return codes.Any(code => string.Equals(code, permission, StringComparison.OrdinalIgnoreCase)
		                      || (code.EndsWith('*') && permission.StartsWith(code[..^1], StringComparison.OrdinalIgnoreCase)));
	}

	public bool IsInRole(string role)
	{
		return string.IsNullOrEmpty(role);
	}

	public bool IsRequirementSatisfied(string permission, IReadOnlyList<string> roles)
	{
		return IsGranted(permission) && (roles is not { Count: > 0 } || roles.Any(IsInRole));
	}

	public ValueTask<bool> IsGrantedAsync(string permission, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(IsGranted(permission));
	}
}

/// <summary>行级判定：一切可见（真实宿主这里会按租户/部门/归属人比较对象属性）。</summary>
internal sealed class VisibleScopeAuthorizer : IObjectScopeAuthorizer
{
	public bool IsConstrained(Type resourceType)
	{
		return resourceType == typeof(GroundedEntity);
	}

	public bool AllowsOperation(BusinessContext context, object target, string operation)
	{
		return true;
	}

	public string ExplainOperation(BusinessContext context, object target, string operation)
	{
		return "可见";
	}

	public bool AllowsRow(BusinessContext context, object target, string scopeKey = null)
	{
		return true;
	}

	public string ExplainRow(BusinessContext context, object target, string scopeKey = null)
	{
		return "可见";
	}
}

#endregion
