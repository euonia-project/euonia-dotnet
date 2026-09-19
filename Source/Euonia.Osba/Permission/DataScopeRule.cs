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
/// 目标对象未实现 <see cref="IDataScoped"/> 时规则自动放行。但<b>无法判定</b>时一律失败，
/// 不静默放行：数据范围服务不可用（未注册 <see cref="IDataScopeService"/>、或未注册其依赖
/// <see cref="IUserScopeProvider"/>）属配置错误，规则失败以暴露问题，避免出现
/// "看似启用了数据权限、实际没有生效"的情况。
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
			// 注意：此处不捕获 IDataScopeService 解析异常。数据范围服务或其依赖
			// （IUserScopeProvider）缺失属配置错误，应向上暴露：规则引擎会把异常
			// 转换为规则错误，从而阻止落库（fail-closed），而不是静默放行。
			var service = businessObject.BusinessContext?.GetService<IDataScopeService>();
			if (service == null)
			{
				context.AddErrorResult("数据范围服务不可用，无法校验数据权限（请确认已注册 IDataScopeService 及其依赖 IUserScopeProvider）。");
			}
			else if (!service.CanAccess(scoped))
			{
				context.AddErrorResult("当前用户无权访问该数据（已在数据范围之外）。");
			}
		}

		await Task.CompletedTask;
	}
}