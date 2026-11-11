using System.Linq.Expressions;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户查询过滤器的纯表达式工具；不依赖 EF Core。
/// </summary>
public static class TenantFilterExpression
{
	/// <summary>
	/// 将已有过滤器与租户过滤器按逻辑与组合，并统一参数。
	/// </summary>
	/// <param name="existing">已有过滤器（如软删除）。</param>
	/// <param name="tenant">租户过滤器。</param>
	/// <returns>组合后的过滤器。</returns>
	/// <exception cref="ArgumentNullException">任一参数为 <c>null</c> 时抛出。</exception>
	/// <exception cref="ArgumentException">两个过滤器的参数类型不一致时抛出。</exception>
	/// <remarks>
	/// EF Core 9 的 <c>SetQueryFilter</c> 是替换语义；在缺少命名过滤器的提供程序上，
	/// 必须把软删除与租户过滤器组合后一次性设置，避免互相覆盖。
	/// </remarks>
	public static LambdaExpression Combine(LambdaExpression existing, LambdaExpression tenant)
	{
		ArgumentNullException.ThrowIfNull(existing);
		ArgumentNullException.ThrowIfNull(tenant);

		if (existing.Parameters.Count != 1 || tenant.Parameters.Count != 1)
		{
			throw new ArgumentException("Query filters must have exactly one parameter.");
		}

		var parameter = existing.Parameters[0];
		if (parameter.Type != tenant.Parameters[0].Type)
		{
			throw new ArgumentException("Query filters must use the same parameter type.");
		}

		var body = new ParameterReplacer(tenant.Parameters[0], parameter).Visit(tenant.Body)!;
		var combined = Expression.AndAlso(existing.Body, body);
		return Expression.Lambda(combined, parameter);
	}

	private sealed class ParameterReplacer : ExpressionVisitor
	{
		private readonly ParameterExpression _source;
		private readonly ParameterExpression _target;

		public ParameterReplacer(ParameterExpression source, ParameterExpression target)
		{
			_source = source;
			_target = target;
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			return ReferenceEquals(node, _source) ? _target : base.VisitParameter(node);
		}
	}
}
