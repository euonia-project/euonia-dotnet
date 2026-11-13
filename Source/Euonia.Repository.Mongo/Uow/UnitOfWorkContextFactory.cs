using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Threading;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// 用于创建 <see cref="IRepositoryContext"/> 实例的 <see cref="IContextFactory"/> 实现。
/// </summary>
/// <remarks>
/// <para>
/// 连接字符串按以下优先级依次读取，取第一个非空白结果：
/// <c>ConnectionStrings:&lt;ConnectionStringAttribute.Name&gt;</c> →
/// <c>ConnectionStrings:&lt;数据上下文类型名&gt;</c> →
/// <c>ConnectionStrings:Default</c> →
/// <see cref="ConnectionStringAttribute.Value"/>（代码默认值，优先级最低）。
/// </para>
/// <para>
/// 环境变量（例如 <c>ConnectionStrings__Default</c>）通过配置自动进入前三步，并可覆盖 <see cref="ConnectionStringAttribute.Value"/>。
/// 事务性工作单元以数据上下文类型与最终连接字符串组合为键复用上下文。
/// </para>
/// </remarks>
internal class UnitOfWorkContextFactory : IContextFactory
{
	private readonly IUnitOfWorkManager _manager;
	private readonly IConfiguration _configuration;

	/// <summary>
	/// 初始化 <see cref="UnitOfWorkContextFactory"/> 的新实例。
	/// </summary>
	/// <param name="manager">工作单元管理器，用于获取当前工作单元及其服务提供程序。</param>
	/// <param name="configuration">配置源，用于按约定读取数据库连接字符串。</param>
	public UnitOfWorkContextFactory(IUnitOfWorkManager manager, IConfiguration configuration)
	{
		_manager = manager;
		_configuration = configuration;
	}

	/// <inheritdoc />
	public TContext? GetContext<TContext>()
		where TContext : class, IRepositoryContext
	{
		var unitOfWork = _manager.Current;
		if (unitOfWork == null)
		{
			return null;
		}

		if (!unitOfWork.Options.IsTransactional)
		{
			return unitOfWork.ServiceProvider.GetService<TContext>();
		}

		var contextType = typeof(TContext);
		var attribute = contextType.GetCustomAttribute<ConnectionStringAttribute>();

		string? connectionString;

		// 按 ConnectionStrings:<Name> → <上下文类型名> → Default → ConnectionStringAttribute.Value 的顺序解析。
		if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Name))
		{
			connectionString = _configuration.GetConnectionString(attribute.Name);
		}
		else
		{
			connectionString = null;
		}

		if (string.IsNullOrWhiteSpace(connectionString))
		{
			connectionString = _configuration.GetConnectionString(contextType.Name);
		}

		if (string.IsNullOrWhiteSpace(connectionString))
		{
			connectionString = _configuration.GetConnectionString("Default");
		}

		if (string.IsNullOrWhiteSpace(connectionString))
		{
			connectionString = attribute?.Value;
		}

		var key = $"{contextType.FullName}_{connectionString}";

		var context = unitOfWork.FindContext(key);

		if (context is UnitOfWorkContext uowContext)
		{
			return (TContext)uowContext.Context;
		}

		var dbContext = unitOfWork.ServiceProvider.GetService<TContext>()!;

		// 事务性工作单元必须显式开启 Mongo 会话事务。
		AsyncContext.Run(() => dbContext.BeginTransactionAsync());

		uowContext = new UnitOfWorkContext(dbContext);
		unitOfWork.AddContext(key, uowContext);
		return dbContext;
	}

	/// <inheritdoc />
	public int Order => 1;
}