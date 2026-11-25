# Euonia.Security 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。
> 文末另附「跨程序集契约」一节：它们位于 `Euonia.Core`（命名空间相同），不属于本程序集，
> 但本包的编程模型（`[Permission]` 声明、要求来源、兼容契约适配）直接依赖它们。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Security |
| 公共类型 | 27 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.PermissionOptions

**类型**：class

- new Microsoft.Extensions.DependencyInjection.PermissionOptions()
- Microsoft.Extensions.DependencyInjection.PermissionOptions NoModels()
- Microsoft.Extensions.DependencyInjection.PermissionOptions Requirements(Nerosoft.Euonia.Security.IPermissionRequirementSource source)
- Microsoft.Extensions.DependencyInjection.PermissionOptions Scan(System.Reflection.Assembly[] assemblies)
- [Obsolete] Microsoft.Extensions.DependencyInjection.PermissionOptions NoOperationCodes()
- [Obsolete] Microsoft.Extensions.DependencyInjection.PermissionOptions OnAttributeOrName(System.String operation, System.String namePrefix, System.Type[] attributeTypes)
- [Obsolete] Microsoft.Extensions.DependencyInjection.PermissionOptions OnMethod(System.String operation, Func<System.Reflection.MethodInfo, System.Boolean> predicate)
- [Obsolete] Microsoft.Extensions.DependencyInjection.PermissionOptions Source(Nerosoft.Euonia.Security.IPermissionCodeSource source)

> `[Obsolete]` 成员保留只为兼容：`OnAttributeOrName` / `OnMethod` 与方法级 `[Permission]` 的生效
> 无关（现在总是按被调用的方法生效），`NoOperationCodes` 是空操作，`Source` 的替代入口是
> `Requirements`。清单见 [API.md](API.md#已废弃成员仅兼容勿用于新代码)。

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddPermission(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Microsoft.Extensions.DependencyInjection.PermissionOptions> configure)

## 命名空间 Nerosoft.Euonia.Security

### CompiledScopePolicy<T>

**类型**：class

- Expression<Func<T, System.Boolean>> Allow { get; }
- Expression<Func<T, System.Boolean>> Deny { get; }
- System.Boolean HasAllow { get; }
- System.String ScopeKey { get; }

### Nerosoft.Euonia.Security.IScopeGuard

**类型**：interface

- IReadOnlyCollection<System.String> Permissions { get; }
- System.Security.Claims.ClaimsPrincipal User { get; }
- System.Boolean Allows<T>(T resource, System.String identifier = null)
- System.Boolean AllowsObject(System.Object resource, System.String identifier = null)
- IQueryable<T> Apply<T>(IQueryable<T> source, System.String identifier = null)
- System.Threading.Tasks.ValueTask EnsureResolvedAsync(System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Security.ScopeDecision Explain<T>(T resource, System.String identifier = null)
- System.String ExplainObject(System.Object resource, System.String identifier = null)
- CompiledScopePolicy<T> GetPolicy<T>(System.String identifier = null)
- Nerosoft.Euonia.Security.ScopeSubjectSet GetSubjects()
- ValueTask<Nerosoft.Euonia.Security.ScopeSubjectSet> GetSubjectsAsync(System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Security.ScopeSubjectSet> RefreshAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Security.IScopeModel

**类型**：interface

- IReadOnlyDictionary<System.String, System.String> DeclaredKeys { get; }
- IReadOnlyDictionary<System.String, System.Object> DeclaredPolicies { get; }
- System.Object PolicyObject { get; }
- System.Type ResourceType { get; }
- void Define(Nerosoft.Euonia.Security.IScopeModelBuilder builder)

### IScopeModel<T>

**类型**：interface
接口 Nerosoft.Euonia.Security.IScopeModel

- ScopePolicy<T> Policy { get; }

### Nerosoft.Euonia.Security.IScopeModelBuilder

**类型**：interface

- IReadOnlyDictionary<System.String, System.Linq.Expressions.LambdaExpression> Classifications { get; }
- IReadOnlyDictionary<System.String, System.Linq.Expressions.LambdaExpression> Dimensions { get; }

### Nerosoft.Euonia.Security.IScopeSubjectResolver

**类型**：interface

- ValueTask<Nerosoft.Euonia.Security.ScopeSubjectSet> ResolveAsync(System.Security.Claims.ClaimsPrincipal user, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Security.ScopeDecision

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Security.ScopeDecision>

- new Nerosoft.Euonia.Security.ScopeDecision(System.Boolean Allowed, System.String ScopeKey, IReadOnlyList<System.String> MatchedAllows, IReadOnlyList<System.String> MatchedDenies)
- System.Boolean Allowed { get; set; }
- IReadOnlyList<System.String> MatchedAllows { get; set; }
- IReadOnlyList<System.String> MatchedDenies { get; set; }
- System.String ScopeKey { get; set; }
- System.String ToString()

### Nerosoft.Euonia.Security.ScopeDimensions

**类型**：static class

- static const System.String Dept = dept
- static const System.String Member = member
- static const System.String Owner = owner
- static const System.String Project = project
- static const System.String Region = region
- static const System.String Team = team
- static System.String Register(System.String name)

### Nerosoft.Euonia.Security.ScopeFilter

**类型**：static class

- static System.Boolean Allows<T>(T resource, CompiledScopePolicy<T> policy)
- static IQueryable<T> Apply<T>(IQueryable<T> source, CompiledScopePolicy<T> policy)
- static Nerosoft.Euonia.Security.ScopeDecision Explain<T>(T resource, ScopePolicy<T> policy, Nerosoft.Euonia.Security.ScopeModelDescriptor model, Nerosoft.Euonia.Security.ScopeSubjectSet subjects, System.String scopeKey = null)
- static IEnumerable<T> Filter<T>(IEnumerable<T> source, CompiledScopePolicy<T> policy)

### Nerosoft.Euonia.Security.ScopeGuard

**类型**：class
接口 Nerosoft.Euonia.Security.IScopeGuard、System.IDisposable

### Nerosoft.Euonia.Security.ScopeKeys

**类型**：static class

- static const System.String Default = @default
- static const System.String Prefix = @
- static System.Boolean IsReserved(System.String code)
- static System.String Validate(System.String code)
- static System.String ValidateOperation(System.String operation)

### ScopeModel<T>

**类型**：class
接口 IScopeModel<T>、Nerosoft.Euonia.Security.IScopeModel

- ScopePolicy<T> Policy { get; }
- System.Type ResourceType { get; }
- void Declare(ScopePolicySet<T> policies)
- void Define(ScopeModelBuilder<T> builder)

### ScopeModelBuilder<T>

**类型**：class
接口 Nerosoft.Euonia.Security.IScopeModelBuilder

- new ScopeModelBuilder<T>()
- ScopeModelBuilder<T> Classify(System.String name, Expression<Func<T, System.Object>> selector)
- ScopeModelBuilder<T> Map(System.String dimension, Expression<Func<T, System.String>> selector)
- ScopeModelBuilder<T> MapMany(System.String dimension, Expression<Func<T, IEnumerable<System.String>>> selector)

### Nerosoft.Euonia.Security.ScopeModelDescriptor

**类型**：class

- IReadOnlyCollection<System.String> Classifications { get; }
- IReadOnlyCollection<System.String> CollectionDimensions { get; }
- IReadOnlyCollection<System.String> Dimensions { get; }
- System.Type ResourceType { get; }
- static Nerosoft.Euonia.Security.ScopeModelDescriptor Create(Nerosoft.Euonia.Security.IScopeModel model)
- System.Boolean IsCollectionDimension(System.String dimension)

### Nerosoft.Euonia.Security.ScopeModelDiagnostic

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Security.ScopeModelDiagnostic>

- new Nerosoft.Euonia.Security.ScopeModelDiagnostic(System.String ModelName, System.String Message)
- System.String Message { get; set; }
- System.String ModelName { get; set; }

### Nerosoft.Euonia.Security.ScopeModelRegistration

**类型**：class

- Nerosoft.Euonia.Security.ScopeModelDescriptor Descriptor { get; }

### Nerosoft.Euonia.Security.ScopeModelRegistry

**类型**：class

- static Nerosoft.Euonia.Security.ScopeModelRegistry Empty { get; }
- System.Boolean HasDeclarations { get; }
- IReadOnlyCollection<System.Type> ResourceTypes { get; }
- static Nerosoft.Euonia.Security.ScopeModelRegistry Create(System.Reflection.Assembly[] assemblies)
- System.Boolean IsDeclared(System.Type resourceType)
- System.Boolean TryGet(System.Type resourceType, out Nerosoft.Euonia.Security.ScopeModelRegistration registration)
- System.Boolean TryGetInherited(System.Type resourceType, out Nerosoft.Euonia.Security.ScopeModelRegistration registration)

### Nerosoft.Euonia.Security.ScopeModelValidationException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Security.ScopeModelValidationException(IReadOnlyList<Nerosoft.Euonia.Security.ScopeModelDiagnostic> diagnostics)
- IReadOnlyList<Nerosoft.Euonia.Security.ScopeModelDiagnostic> Diagnostics { get; }

### ScopePolicy<T>

**类型**：class

- static ScopePolicy<T> All(ScopePolicy<T>[] policies)
- static ScopePolicy<T> Any(ScopePolicy<T>[] policies)
- static ScopePolicy<T> Deny(ScopePolicy<T> policy)
- static ScopePolicy<T> Grant(System.String dimension)
- static ScopePolicy<T> Self()
- System.String ToString()
- static ScopePolicy<T> Where(Expression<Func<T, System.Boolean>> predicate)

### Nerosoft.Euonia.Security.ScopePolicyCompiler

**类型**：static class

- static CompiledScopePolicy<T> Compile<T>(ScopePolicy<T> policy, Nerosoft.Euonia.Security.ScopeModelDescriptor model, Nerosoft.Euonia.Security.ScopeSubjectSet subjects, System.String scopeKey = null)

### ScopePolicySet<T>

**类型**：class

- new ScopePolicySet<T>()
- IReadOnlyCollection<System.String> Identifiers { get; }
- ScopePolicySet<T> For(System.String code, ScopePolicy<T> policy)
- ScopePolicySet<T> ForOperation(System.String operation, ScopePolicy<T> policy, System.String scopeKey = null)

### Nerosoft.Euonia.Security.ScopeSubjectSet

**类型**：class

- IReadOnlyCollection<System.String> Codes { get; }
- static Nerosoft.Euonia.Security.ScopeSubjectSet Empty { get; }
- IReadOnlyCollection<System.String> KeysWithGrants { get; }
- System.Boolean Contains(System.String scopeKey, System.String dimension, System.String value)
- static Nerosoft.Euonia.Security.ScopeSubjectSetBuilder CreateBuilder()
- System.Boolean HoldsPermission(System.String code)
- IReadOnlyCollection<System.String> ValuesOf(System.String scopeKey, System.String dimension)

### Nerosoft.Euonia.Security.ScopeSubjectSetBuilder

**类型**：class

- new Nerosoft.Euonia.Security.ScopeSubjectSetBuilder()
- Nerosoft.Euonia.Security.ScopeSubjectSetBuilder Add(System.String dimension, System.String value)
- Nerosoft.Euonia.Security.ScopeSubjectSetBuilder AddCode(System.String code)
- Nerosoft.Euonia.Security.ScopeSubjectSetBuilder AddCodes(IEnumerable<System.String> codes)
- Nerosoft.Euonia.Security.ScopeSubjectSetBuilder AddGrant(System.String scopeKey, System.String dimension, System.String value)
- Nerosoft.Euonia.Security.ScopeSubjectSetBuilder AddGrant(System.String scopeKey, System.String dimension, IEnumerable<System.String> values)
- Nerosoft.Euonia.Security.ScopeSubjectSetBuilder AddRange(System.String dimension, IEnumerable<System.String> values)
- Nerosoft.Euonia.Security.ScopeSubjectSetBuilder AddSelf(System.String userId)
- Nerosoft.Euonia.Security.ScopeSubjectSet Build()

### Nerosoft.Euonia.Security.SecurityTelemetry

**类型**：static class

- static const System.String AuthorizeActivity = euonia.security.authorize
- static const System.String DecisionsMetric = euonia.security.authorization.decisions
- static readonly System.Diagnostics.ActivitySource Source
- static const System.String SourceName = Nerosoft.Euonia.Security
- static void Complete(System.Diagnostics.Activity activity, System.Boolean granted)
- static System.Diagnostics.Activity Start(System.String permission)

### Nerosoft.Euonia.Security.SubjectPermissionChecker

**类型**：class
接口 Nerosoft.Euonia.Security.IPermissionChecker

- new Nerosoft.Euonia.Security.SubjectPermissionChecker(Nerosoft.Euonia.Security.UserPrincipal user, Nerosoft.Euonia.Security.IScopeGuard guard)
- System.Boolean IsGranted(System.String permission)
- System.Boolean IsInRole(System.String role)
- System.Threading.Tasks.ValueTask<System.Boolean> IsGrantedAsync(System.String permission, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.ValueTask EnsureResolvedAsync(System.Threading.CancellationToken cancellationToken = default)

## 跨程序集契约（位于 Euonia.Core）

> 以下类型与前面的类型同属命名空间 `Nerosoft.Euonia.Security`，但定义在 `Euonia.Core` 程序集，
> 因此不计入上表的 27 个公共类型。它们本包全部直接使用。

### Nerosoft.Euonia.Security.IPermissionRequirementSource

**类型**：interface

- IReadOnlyList<Nerosoft.Euonia.Security.PermissionAttribute> RequirementsFor(System.Type type, System.Reflection.MethodInfo method, System.String operation)

> `method` 可以为 `null`（表示「不是从某个具体方法进入的」，只返回类型级要求）；`operation` 由宿主传入
> 且非空，实现忽略它。

### Nerosoft.Euonia.Security.PermissionRequirements

**类型**：static class

- static IReadOnlyList<Nerosoft.Euonia.Security.PermissionAttribute> For(System.Type type)
- static IReadOnlyList<Nerosoft.Euonia.Security.PermissionAttribute> For(System.Reflection.MethodInfo method)
- static IReadOnlyList<Nerosoft.Euonia.Security.PermissionAttribute> For(System.Type type, System.Reflection.MethodInfo method)
- static IReadOnlyList<Nerosoft.Euonia.Security.PermissionAttribute> Merge(IEnumerable<Nerosoft.Euonia.Security.PermissionAttribute>[] sets)
- static Nerosoft.Euonia.Security.IPermissionRequirementSource Adapt(Nerosoft.Euonia.Security.IPermissionCodeSource source)

> `For(Type, MethodInfo)` 的 `method` 可为 `null`（只返回类型级）；`Merge` 为 `params` 形参，
> 按「码 + 角色」去重并保持首次出现顺序。

### Nerosoft.Euonia.Security.PermissionAttribute

**类型**：sealed class
基类 System.Attribute

- new Nerosoft.Euonia.Security.PermissionAttribute(System.String permission)
- new Nerosoft.Euonia.Security.PermissionAttribute(System.String permission, System.String[] roles)
- System.String Permission { get; }
- System.String[] Roles { get; }
- System.String Message { get; set; }

> 可标注类型或方法，`AllowMultiple = true`、`Inherited = true`。`permission` 为任意非空字符串
> （不以 `@` 开头、首尾空白在读取时裁剪）；只想校验角色时可以传空码并至少给一个角色。

### Nerosoft.Euonia.Security.IPermissionCodeSource（**已废弃**）

**类型**：interface

- IReadOnlyList<System.String> AllOperations { get; }
- IReadOnlyCollection<System.String> CodesFor(System.Type type, System.String operation)
- IReadOnlyList<Nerosoft.Euonia.Security.PermissionAttribute> RequirementsFor(System.Type type, System.String operation)

> 引擎不再消费本契约；保留仅为既有实现与 `Source(...)` 配置能继续编译。`RequirementsFor` 有默认
> 实现（把 `CodesFor` 的码折算为「有码、无角色」）。新代码请实现 `IPermissionRequirementSource`。
