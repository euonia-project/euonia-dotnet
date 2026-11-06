using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Threading;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// 用于创建 <see cref="IRepositoryContext"/> 实例的 <see cref="IContextFactory"/> 实现。
/// </summary>
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

		string? connectionString;

		var attribute = contextType.GetCustomAttribute<ConnectionStringAttribute>();

		if (attribute != null)
		{
			if (!string.IsNullOrWhiteSpace(attribute.Value))
			{
				connectionString = attribute.Value;
			}
			else
			{
				connectionString = _configuration.GetConnectionString(contextType.Name);
			}
		}
		else
		{
			connectionString = string.Empty;
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