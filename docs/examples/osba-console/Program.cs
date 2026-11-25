// ---------------------------------------------------------------------------
// Euonia.Osba 最小可运行示例：工厂边界上的方法级权限
//
// 运行：
//   dotnet run --project docs/examples/osba-console
//
// 预期输出：
//   granted(order:read+order:update) => saved=A-1
//   missing(order:update) => SecurityException
//   missing(order:read) => SecurityException
//
// 要点：
//   * 类型级 [Permission("order:read")] 对该类型的所有操作生效；
//   * 方法级 [Permission("order:update")] 只在 UpdateAsync 被工厂调用时生效；
//   * 闸门由 BusinessObjectFactory 在调用业务方法之前强制执行，未授予即抛 SecurityException。
// ---------------------------------------------------------------------------

using System.Security;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

var failures = 0;

await RunScenario("granted(order:read+order:update)", ["order:read", "order:update"], expectDenied: false);
await RunScenario("missing(order:update)", ["order:read"], expectDenied: true);
await RunScenario("missing(order:read)", ["order:update"], expectDenied: true);

return failures == 0 ? 0 : 1;

async Task RunScenario(string name, string[] codes, bool expectDenied)
{
    var services = new ServiceCollection();

    // 1) 业务对象：扫描包含 Order 的程序集，注册工厂、业务上下文与对象类型。
    services.AddBusinessObject(typeof(Order).Assembly);

    // 2) 权限引擎：只需 Scan；[Permission] 声明自动生效，不需要声明任何操作入口规则。
    services.AddPermission(permission => permission.Scan(typeof(Order).Assembly));

    // 3) 授权值来源：真实应用在这里查询用户与授权表。
    services.AddSingleton<IScopeSubjectResolver>(new CodeSubjectResolver(codes));

    // 4) 当前用户（角色来自声明）。
    services.AddSingleton(CreateUser("alice"));

    await using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();

    // 业务上下文是环境作用域，工厂用它解析判定实现与授权数据。
    BusinessContextAccessor.SetCurrent(scope.ServiceProvider);
    var businessContext = scope.ServiceProvider.GetRequiredService<BusinessContext>();
    var factory = scope.ServiceProvider.GetRequiredService<IObjectFactory>();

    var order = new Order { Name = "A-1", BusinessContext = businessContext };
    order.MarkAsChanged();   // 有变更才会走到更新工厂方法

    try
    {
        var saved = await factory.SaveAsync(order, CancellationToken.None);
        Console.WriteLine($"{name} => saved={saved.Name}");

        if (expectDenied)
        {
            failures++;
        }
    }
    catch (SecurityException)
    {
        Console.WriteLine($"{name} => SecurityException");

        if (!expectDenied)
        {
            failures++;
        }
    }
    finally
    {
        BusinessContextAccessor.Clear();
    }
}

static UserPrincipal CreateUser(string userId)
{
    var identity = new ClaimsIdentity(
        [new Claim(UserClaimTypes.Subject, userId), new Claim(UserClaimTypes.Role, "operator")],
        authenticationType: "Bearer",
        nameType: ClaimTypes.Name,
        roleType: UserClaimTypes.Role);

    return new UserPrincipal(new ClaimsPrincipal(identity));
}

/// <summary>
/// 可编辑业务对象：类型级要求 <c>order:read</c>，更新方法另有 <c>order:update</c>。
/// </summary>
[Permission("order:read")]
public sealed class Order : EditableObject<Order>
{
    /// <summary>获取或设置订单名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 更新工厂方法：只在更新保存时被调用，其上的 <c>order:update</c> 因此只约束更新路径。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    [Permission("order:update")]
    [FactoryUpdate]
    protected override Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// 授予固定权限码集合的解析器（模拟授权数据）。
/// </summary>
public sealed class CodeSubjectResolver(string[] codes) : IScopeSubjectResolver
{
    /// <inheritdoc />
    public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(ScopeSubjectSet.CreateBuilder().AddCodes(codes).Build());
    }
}
