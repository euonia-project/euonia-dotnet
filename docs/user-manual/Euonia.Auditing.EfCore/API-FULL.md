# Euonia.Auditing.EfCore 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Auditing.EfCore |
| 公共类型 | 4 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.AuditingEfCoreServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAuditingEfCore<TContext>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> configure)

## 命名空间 Nerosoft.Euonia.Auditing.EfCore

### Nerosoft.Euonia.Auditing.EfCore.AuditingDbContext

**类型**：class
基类 Microsoft.EntityFrameworkCore.DbContext；接口 IInfrastructure<System.IServiceProvider>、Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies、Microsoft.EntityFrameworkCore.Internal.IDbSetCache、Microsoft.EntityFrameworkCore.Internal.IDbContextPoolable、Microsoft.EntityFrameworkCore.Infrastructure.IResettableService、System.IDisposable、System.IAsyncDisposable

- new Nerosoft.Euonia.Auditing.EfCore.AuditingDbContext(DbContextOptions<Nerosoft.Euonia.Auditing.EfCore.AuditingDbContext> options)
- DbSet<Nerosoft.Euonia.Auditing.AuditingRecord> Records { get; }

### Nerosoft.Euonia.Auditing.EfCore.AuditingModelBuilderExtensions

**类型**：static class

- static Microsoft.EntityFrameworkCore.ModelBuilder ApplyAuditingModel(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)

### EfCoreAuditingStore<TContext>

**类型**：class
接口 Nerosoft.Euonia.Auditing.IAuditingStore、Nerosoft.Euonia.Auditing.IAuditingStoreQuery

- new EfCoreAuditingStore<TContext>(IDbContextFactory<TContext> factory)
- Task<System.Int32> DeleteBeforeAsync(System.DateTime cutoff, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Auditing.AuditingRecord> FindByScopeAsync(System.String scopeId, System.Threading.CancellationToken cancellationToken = default)
- Task<IReadOnlyList<Nerosoft.Euonia.Auditing.AuditingRecord>> QueryAsync(Nerosoft.Euonia.Auditing.AuditingQuery query, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SaveAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SaveManyAsync(IReadOnlyCollection<Nerosoft.Euonia.Auditing.AuditingRecord> records, System.Threading.CancellationToken cancellationToken = default)
