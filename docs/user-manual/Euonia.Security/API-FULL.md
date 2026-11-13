# Euonia.Security 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

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
- Microsoft.Extensions.DependencyInjection.PermissionOptions NoOperationCodes()
- Microsoft.Extensions.DependencyInjection.PermissionOptions OnAttributeOrName(System.String operation, System.String namePrefix, System.Type[] attributeTypes)
- Microsoft.Extensions.DependencyInjection.PermissionOptions OnMethod(System.String operation, Func<System.Reflection.MethodInfo, System.Boolean> predicate)
- Microsoft.Extensions.DependencyInjection.PermissionOptions Scan(System.Reflection.Assembly[] assemblies)
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
