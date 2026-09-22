using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// <see cref="IContextFactory"/> 的实现，根据当前工作单元创建或复用 <see cref="IRepositoryContext"/> 实例。
/// </summary>
/// <remarks>
/// <para>当没有活动的工作单元时返回 <c>null</c>，从而让其他工厂继续尝试。</para>
/// <para>对于非事务性工作单元，直接由服务提供程序解析上下文；对于事务性工作单元，则以数据上下文类型与连接字符串组合为键复用上下文。</para>
/// <para>连接字符串优先取 <see cref="ConnectionStringAttribute.Value"/>，否则以数据上下文类型名称从配置中读取。</para>
/// </remarks>
internal class UnitOfWorkContextFactory : IContextFactory
{
	private readonly IUnitOfWorkManager _manager;
	private readonly IConfiguration _configuration;

	/// <summary>
	/// 初始化 <see cref="UnitOfWorkContextFactory"/> 类的新实例。
	/// </summary>
	/// <param name="manager">工作单元管理器，用于获取当前工作单元。</param>
	/// <param name="configuration">配置源，用于按名称读取连接字符串。</param>
	public UnitOfWorkContextFactory(IUnitOfWorkManager manager, IConfiguration configuration)
	{
		_manager = manager;
		_configuration = configuration;
	}

	/// <inheritdoc />
	public TContext GetContext<TContext>()
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

		string connectionString;

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

		var dbContext = unitOfWork.ServiceProvider.GetService<TContext>();

		//var transaction = dbContext.GetConnection().BeginTransaction(unitOfWork.Options.IsolationLevel ?? IsolationLevel.Unspecified);
		uowContext = new UnitOfWorkContext(dbContext);
		unitOfWork.AddContext(key, uowContext);
		return dbContext;
	}

	/// <inheritdoc />
	public int Order => 1;
}