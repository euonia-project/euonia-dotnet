using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 基于连接串模板的租户连接串解析器。
/// </summary>
/// <typeparam name="TContext">数据上下文类型。</typeparam>
public sealed class TenantConnectionStringResolver<TContext> : ITenantConnectionStringResolver<TContext>
{
	private readonly IOptions<MultiTenancyOptions> _options;

	/// <summary>
	/// 初始化 <see cref="TenantConnectionStringResolver{TContext}"/> 的新实例。
	/// </summary>
	/// <param name="options">多租户选项。</param>
	public TenantConnectionStringResolver(IOptions<MultiTenancyOptions> options)
	{
		_options = options;
	}

	/// <inheritdoc />
	public string? Resolve(ITenantInfo? tenant)
	{
		if (tenant?.ConnectionString is { Length: > 0 } connectionString)
		{
			return connectionString;
		}

		var template = _options.Value.ConnectionStringTemplate;
		if (string.IsNullOrWhiteSpace(template) || tenant == null)
		{
			return null;
		}

		return template
		       .Replace("{tenant}", tenant.Id, StringComparison.Ordinal)
		       .Replace("{identifier}", tenant.Identifier ?? tenant.Id, StringComparison.Ordinal);
	}
}
