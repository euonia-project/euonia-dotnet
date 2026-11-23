using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Repository.Tests;

public class RepositoryAdditionsTests
{
	[Fact]
	public async Task QueryHandleOverloads_ApplyQuery()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var fixture = await RepositoryFixture.CreateAsync(cancellationToken);
		var repository = fixture.Repository;

		await repository.InsertAsync(new AuditRecord { Id = Guid.NewGuid(), Name = "alpha" }, cancellationToken: cancellationToken);
		await repository.InsertAsync(new AuditRecord { Id = Guid.NewGuid(), Name = "beta" }, cancellationToken: cancellationToken);

		var single = await repository.GetAsync(query => query.Where(x => x.Name == "alpha"), cancellationToken);
		var list = await repository.FindAsync(query => query.Where(x => x.Name == "beta"), cancellationToken);
		var count = await repository.CountAsync(query => query.Where(x => x.Name == "alpha"), cancellationToken);

		Assert.NotNull(single);
		Assert.Equal("alpha", single.Name);
		Assert.Single(list);
		Assert.Equal("beta", list[0].Name);
		Assert.Equal(1, count);
	}

	[Fact]
	public async Task DeleteAsync_ById_SoftDeletesTombstone()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var fixture = await RepositoryFixture.CreateAsync(cancellationToken);
		var repository = fixture.Repository;
		var record = new AuditRecord { Id = Guid.NewGuid(), Name = "to-delete" };

		await repository.InsertAsync(record, cancellationToken: cancellationToken);
		await repository.DeleteAsync(record.Id, cancellationToken: cancellationToken);

		Assert.True(record.IsDeleted);
		Assert.Equal("user-1", record.DeletedBy);
		Assert.NotNull(record.DeletedAt);

		var stored = await fixture.Context.Set<AuditRecord>()
		                        .IgnoreQueryFilters()
		                        .SingleAsync(x => x.Id == record.Id, cancellationToken);

		Assert.True(stored.IsDeleted);
	}

	[Fact]
	public async Task LookupAsync_ReturnsDictionary()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		await using var fixture = await RepositoryFixture.CreateAsync(cancellationToken);
		var repository = fixture.Repository;
		var first = new AuditRecord { Id = Guid.NewGuid(), Name = "one" };
		var second = new AuditRecord { Id = Guid.NewGuid(), Name = "two" };

		await repository.InsertAsync([first, second], cancellationToken: cancellationToken);

		var lookup = await repository.LookupAsync([first.Id, second.Id], x => new KeyValuePair<Guid, string>(x.Id, x.Name), cancellationToken);

		Assert.Equal("one", lookup[first.Id]);
		Assert.Equal("two", lookup[second.Id]);
	}

	private sealed class RepositoryFixture : IAsyncDisposable
	{
		private readonly SqliteConnection _connection;

		private RepositoryFixture(SqliteConnection connection, RepositoryTestContext context, EfCoreRepository<RepositoryTestContext, AuditRecord, Guid> repository)
		{
			_connection = connection;
			Context = context;
			Repository = repository;
		}

		public RepositoryTestContext Context { get; }

		public EfCoreRepository<RepositoryTestContext, AuditRecord, Guid> Repository { get; }

		public static async Task<RepositoryFixture> CreateAsync(CancellationToken cancellationToken)
		{
			var connection = new SqliteConnection("DataSource=:memory:");
			await connection.OpenAsync(cancellationToken);

			var options = new DbContextOptionsBuilder<RepositoryTestContext>()
			              .UseSqlite(connection)
			              .Options;

			var context = new RepositoryTestContext(options);
			await context.Database.EnsureCreatedAsync(cancellationToken);
			var repository = new EfCoreRepository<RepositoryTestContext, AuditRecord, Guid>(new ContextProvider(context));

			return new RepositoryFixture(connection, context, repository);
		}

		public async ValueTask DisposeAsync()
		{
			await Context.DisposeAsync();
			await _connection.DisposeAsync();
		}
	}

	private sealed class ContextProvider(DbContext context) : IContextProvider
	{
		public TContext GetContext<TContext>()
			where TContext : class, IRepositoryContext
		{
			return (TContext)(object)context;
		}

		public void SetFactory<TContext>(Func<TContext> factory)
			where TContext : class, IRepositoryContext
		{
			throw new NotSupportedException();
		}
	}

	private sealed class RepositoryTestContext(DbContextOptions<RepositoryTestContext> options)
		: DataContextBase<RepositoryTestContext>(options)
	{
		protected override bool AutoSetEntryValues => true;

		protected override object? GetCurrentUserId()
		{
			return "user-1";
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<AuditRecord>(builder => builder.HasKey(x => x.Id));
			base.OnModelCreating(modelBuilder);
		}
	}

	private sealed class AuditRecord : IAuditable<string>, IEntity<Guid>
	{
		public Guid Id { get; set; }

		public string Name { get; set; } = string.Empty;

		public DateTime CreatedAt { get; set; }

		public DateTime? UpdatedAt { get; set; }

		public string CreatedBy { get; set; } = string.Empty;

		public string? UpdatedBy { get; set; }

		public DateTime? DeletedAt { get; set; }

		public bool IsDeleted { get; set; }

		public string? DeletedBy { get; set; }

		public object[] GetKeys() => [Id];
	}
}
