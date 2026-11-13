using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 默认租户解析链。
/// </summary>
public sealed class TenantResolver : ITenantResolver
{
	private readonly IEnumerable<ITenantResolveContributor> _contributors;
	private readonly ITenantStore _store;
	private readonly IOptionsMonitor<MultiTenancyOptions> _options;

	/// <summary>
	/// 初始化 <see cref="TenantResolver"/> 的新实例。
	/// </summary>
	/// <param name="contributors">租户候选值来源集合。</param>
	/// <param name="store">租户存储。</param>
	/// <param name="options">多租户选项。</param>
	public TenantResolver(
		IEnumerable<ITenantResolveContributor> contributors,
		ITenantStore store,
		IOptionsMonitor<MultiTenancyOptions> options)
	{
		_contributors = contributors;
		_store = store;
		_options = options;
	}

	/// <inheritdoc />
	public async ValueTask<TenantResolutionResult> ResolveAsync(TenantResolveContext context, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(context);

		var candidates = new List<(int Order, string Value, string Source)>();

		foreach (var contributor in _contributors.OrderBy(x => x.Order))
		{
			string? value;
			try
			{
				value = await contributor.ResolveAsync(context, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception exception) when (exception is not OperationCanceledException)
			{
				throw new TenantStoreUnavailableException(exception);
			}

			if (!string.IsNullOrWhiteSpace(value))
			{
				if (!TenantIdValidator.IsValidIdentifier(value))
				{
					return Fail(TenantFailureCodes.Invalid, candidates);
				}

				candidates.Add((contributor.Order, value, contributor.GetType().Name));
			}
		}

		var options = _options.CurrentValue;
		if (candidates.Count == 0 && !string.IsNullOrWhiteSpace(options.DefaultTenantId))
		{
			if (!TenantIdValidator.IsValidIdentifier(options.DefaultTenantId))
			{
				throw new ConfigurationException("Euonia:MultiTenancy:DefaultTenantId is not a valid tenant identifier.");
			}

			candidates.Add((int.MaxValue, options.DefaultTenantId, nameof(MultiTenancyOptions.DefaultTenantId)));
		}

		if (candidates.Count == 0)
		{
			// RequireTenant=false：允许无租户请求继续，由宿主决定端点是否需要租户。
			return options.RequireTenant
				       ? Fail(TenantFailureCodes.Required, candidates)
				       : new TenantResolutionResult { Candidates = [] };
		}

		var resolved = new List<ITenantInfo>(candidates.Count);
		foreach (var candidate in candidates)
		{
			var tenant = await FindAsync(candidate.Value, cancellationToken).ConfigureAwait(false);
			if (tenant == null)
			{
				return Fail(TenantFailureCodes.NotFound, candidates);
			}

			resolved.Add(tenant);
		}

		var distinct = resolved.Select(x => x.Id).Distinct(StringComparer.Ordinal).ToArray();
		if (distinct.Length > 1)
		{
			return Fail(TenantFailureCodes.Conflict, candidates);
		}

		var selected = resolved[0];
		if (selected.Status != TenantStatus.Active)
		{
			var failure = selected.Status == TenantStatus.Archived ? TenantFailureCodes.Archived : TenantFailureCodes.Suspended;
			return new TenantResolutionResult
			{
				Tenant = selected,
				Source = candidates[0].Source,
				Candidates = candidates.Select(x => x.Value).ToArray(),
				FailureCode = failure,
			};
		}

		return new TenantResolutionResult
		{
			Tenant = selected,
			Source = candidates[0].Source,
			Candidates = candidates.Select(x => x.Value).ToArray(),
		};
	}

	private async ValueTask<ITenantInfo?> FindAsync(string value, CancellationToken cancellationToken)
	{
		try
		{
			return await _store.FindByIdAsync(value, cancellationToken).ConfigureAwait(false)
			       ?? await _store.FindByIdentifierAsync(value, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			throw new TenantStoreUnavailableException(exception);
		}
	}

	private static TenantResolutionResult Fail(string code, List<(int Order, string Value, string Source)> candidates)
	{
		return new TenantResolutionResult
		{
			FailureCode = code,
			Candidates = candidates.Select(x => x.Value).ToArray(),
		};
	}
}
