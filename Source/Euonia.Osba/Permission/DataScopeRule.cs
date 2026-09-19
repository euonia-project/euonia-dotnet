namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 基于 <see cref="IDataScopeService"/> 的数据范围校验规则，将数据权限融入业务对象的规则体系。
/// </summary>
/// <remarks>
/// <para>
/// 当保存（创建/更新/删除）的数据行实现 <see cref="IDataScoped"/> 时，
/// 校验当前用户是否有权访问该数据行，越权数据将使规则失败，阻止落库。
/// </para>
/// <para>
/// 数据权限有两个落点，本规则对应"写入前阻止越权数据"：
/// 查询时通过 <see cref="IDataScopeService.Filter{T}(IEnumerable{T})"/> 排除越权行，
/// 写入时通过本规则失败阻止越权数据。授权值均由 <see cref="IUserScopeProvider"/>
/// 从应用数据实时解析，不固化在声明或代码里。
/// </para>
/// <para>
/// 目标对象未实现 <see cref="IDataScoped"/>，或数据范围服务不可用时，规则自动放行。
/// </para>
/// </remarks>
public class DataScopeRule : RuleBase
{
	/// <summary>
	/// 初始化 <see cref="DataScopeRule"/> 类的新实例。
	/// </summary>
	public DataScopeRule()
	{
	}

	/// <inheritdoc />
	public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		if (context.Target is IBusinessObject businessObject && context.Target is IDataScoped scoped)
		{
			var service = ResolveDataScopeService(businessObject);
			if (service != null && !service.CanAccess(scoped))
			{
				context.AddErrorResult("当前用户无权访问该数据（已在数据范围之外）。");
			}
		}

		await Task.CompletedTask;
	}

	/// <summary>
	/// 解析数据范围服务；数据范围服务或其依赖不可用时返回 <see langword="null"/> 并自动放行。
	/// </summary>
	/// <param name="businessObject">目标业务对象。</param>
	/// <returns>数据范围服务；不可用时返回 <see langword="null"/>。</returns>
	private static IDataScopeService ResolveDataScopeService(IBusinessObject businessObject)
	{
		try
		{
			return businessObject.BusinessContext?.GetService<IDataScopeService>();
		}
		catch
		{
			// 数据范围服务已注册但其依赖（例如 IUserScopeProvider）未配置时，
			// DI 激活会抛异常：视作数据范围不可用，规则放行。
			return null;
		}
	}
}