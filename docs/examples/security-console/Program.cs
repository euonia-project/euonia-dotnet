// ---------------------------------------------------------------------------
// Euonia.Security 最小可运行示例：操作权限 + 数据（行级）权限
//
// 运行：
//   dotnet run --project docs/examples/security-console
//
// 预期输出（每行含义见 README.md）：
//   gate: read=allowed approve=denied delete=allowed export=allowed audit=denied
//   visible=A-1,A-2
//   allows(read)=True,True,False,False
//   allows(order:delete)=True,False,False,True
//
// 本例只做装配与判定，不连接数据库；IQueryable 是内存集合，用于演示 Apply 的下推形状。
// ---------------------------------------------------------------------------

using System.Reflection;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

// 1) 注册引擎：扫描本程序集即可发现 [Permission] 声明与 OrderScope 模型。
var services = new ServiceCollection();
services.AddPermission(permission => permission.Scan(typeof(Order).Assembly));

// 2) 授权值来自应用数据：这个解析器就是「当前用户被授予了什么」的唯一来源。
services.AddScoped<IScopeSubjectResolver, DeptSubjectResolver>();

// 3) 当前用户（角色仍来自声明；权限码不放进令牌，由解析器实时给出）。
services.AddSingleton(CreateUser("alice"));

await using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
var app = scope.ServiceProvider;

// 同步判定只读「已解析的快照」，因此宿主入口先预热一次（异步入口的职责）。
await app.GetRequiredService<IScopeGuard>().EnsureResolvedAsync();

var checker = app.GetRequiredService<IPermissionChecker>();

// 4) 操作权限：通用宿主闸门——收集「类型 + 将被调用的方法」上的要求，逐条交给 IPermissionChecker。
//    [Permission("order:read")] 在类型上：对该类型的所有方法生效；
//    [Permission("order:approve")] 等方法级声明：只在对应方法被调用时生效。
Console.WriteLine(
    $"gate: read={Verdict(checker, typeof(Order), nameof(Order.Read))} " +
    $"approve={Verdict(checker, typeof(Order), nameof(Order.Approve))} " +
    $"delete={Verdict(checker, typeof(Order), nameof(Order.Delete))} " +
    $"export={Verdict(checker, typeof(Order), nameof(Order.Export))} " +
    $"audit={Verdict(checker, typeof(Order), nameof(Order.Audit))}");

// 5) 数据权限：OrderScope 把 DeptId 映射到部门维度，默认策略为「本部门可见」；
//    order:delete 另外声明为「本人可删」。解析器授予 dept=sales 与 owner=alice。
var guard = app.GetRequiredService<IScopeGuard>();

var orders = new[]
{
    new Order { Id = "A-1", DeptId = "sales", OwnerId = "alice" },
    new Order { Id = "A-2", DeptId = "sales", OwnerId = "bob" },
    new Order { Id = "B-1", DeptId = "finance", OwnerId = "bob" },
    new Order { Id = "B-2", DeptId = "finance", OwnerId = "alice" }
};

// 读侧下推：只有 sales 部门的行可见。
var visible = guard.Apply(orders.AsQueryable(), "read").Select(order => order.Id).ToArray();
Console.WriteLine($"visible={string.Join(",", visible)}");

// 单行判定：与 Apply 共用同一份策略与授权数据。
Console.WriteLine($"allows(read)={string.Join(",", orders.Select(order => guard.Allows(order, "read")))}");

// 同一类型、不同标识可以有不同行范围：order:delete 只要求「本人」。
Console.WriteLine($"allows(order:delete)={string.Join(",", orders.Select(order => guard.Allows(order, "order:delete")))}");

// 通用宿主闸门：类型级与方法级要求取并集，角色要求满足任意一个即可。
// 这里的 Verdict 就是宿主框架接入本引擎所需的全部判定逻辑。
static string Verdict(IPermissionChecker checker, Type resourceType, string methodName)
{
    var method = resourceType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                 ?? throw new InvalidOperationException($"method '{methodName}' not found on {resourceType.Name}");

    var requirements = PermissionRequirements.For(resourceType, method);

    return requirements.All(requirement => IsSatisfied(checker, requirement)) ? "allowed" : "denied";
}

static bool IsSatisfied(IPermissionChecker checker, PermissionAttribute requirement)
{
    var rolesSatisfied = requirement.Roles.Length == 0 || requirement.Roles.Any(checker.IsInRole);
    var codeSatisfied = string.IsNullOrEmpty(requirement.Permission) || checker.IsGranted(requirement.Permission);

    return rolesSatisfied && codeSatisfied;
}

static UserPrincipal CreateUser(string userId)
{
    var identity = new ClaimsIdentity(
        [new Claim(UserClaimTypes.Subject, userId), new Claim(UserClaimTypes.Role, "manager")],
        authenticationType: "Bearer",
        nameType: ClaimTypes.Name,
        roleType: UserClaimTypes.Role);

    return new UserPrincipal(new ClaimsPrincipal(identity));
}

/// <summary>
/// 业务对象：类型级要求 <c>order:read</c> 对该类型的所有方法生效。
/// </summary>
[Permission("order:read")]
public sealed class Order
{
    /// <summary>订单标识。</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>所属部门标识（映射到部门维度）。</summary>
    public string DeptId { get; init; } = string.Empty;

    /// <summary>负责人标识（映射到所有者维度）。</summary>
    public string OwnerId { get; init; } = string.Empty;

    /// <summary>读取入口：没有方法级声明，只受类型级 <c>order:read</c> 约束。</summary>
    public void Read()
    {
    }

    /// <summary>审批入口：方法级要求 <c>order:approve</c>。</summary>
    [Permission("order:approve")]
    public void Approve()
    {
    }

    /// <summary>删除入口：方法级要求 <c>order:delete</c>。</summary>
    [Permission("order:delete")]
    public void Delete()
    {
    }

    /// <summary>导出入口：权限码 + 角色，满足任意一个角色即可。</summary>
    [Permission("order:export", "manager", "auditor")]
    public void Export()
    {
    }

    /// <summary>审计入口：权限码已授予，但角色不满足（用户是 manager，不是 auditor）。</summary>
    [Permission("order:audit", "auditor")]
    public void Audit()
    {
    }
}

/// <summary>
/// 订单的数据权限模型：部门维度用于默认策略，所有者维度用于 <c>order:delete</c>。
/// </summary>
public sealed class OrderScope : ScopeModel<Order>
{
    /// <inheritdoc />
    public override void Define(ScopeModelBuilder<Order> builder)
    {
        builder.Map(ScopeDimensions.Dept, order => order.DeptId)
               .Map(ScopeDimensions.Owner, order => order.OwnerId);
    }

    /// <inheritdoc />
    public override ScopePolicy<Order> Policy => ScopePolicy<Order>.Grant(ScopeDimensions.Dept);

    /// <inheritdoc />
    public override void Declare(ScopePolicySet<Order> policies)
    {
        // 同一个用户、同一个类型，不同标识可以有不同行范围
        policies.For("order:delete", ScopePolicy<Order>.Grant(ScopeDimensions.Owner));
    }
}

/// <summary>
/// 授权值来源：本例固定授予 <c>order:read</c>、<c>order:delete</c>，部门 <c>sales</c>，本人 <c>alice</c>。
/// 真实应用在这里查询用户、角色、组织与授权表。
/// </summary>
public sealed class DeptSubjectResolver : IScopeSubjectResolver
{
    /// <inheritdoc />
    public ValueTask<ScopeSubjectSet> ResolveAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default)
    {
        var subjects = ScopeSubjectSet.CreateBuilder()
                                       .AddCode("order:read")
                                       .AddCode("order:delete")
                                       .AddCode("order:export")
                                       .AddCode("order:audit")
                                       .Add(ScopeDimensions.Dept, "sales")
                                       .AddSelf("alice")
                                       .Build();

        return ValueTask.FromResult(subjects);
    }
}
