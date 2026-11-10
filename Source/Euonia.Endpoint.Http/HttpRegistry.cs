namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 路由预留结果。
/// </summary>
public enum HttpReservation
{
	/// <summary>预留成功。</summary>
	Reserved,

	/// <summary>生成路径已经映射过该路由。</summary>
	AlreadyGenerated,

	/// <summary>与另一个运行时/生成端点冲突。</summary>
	Conflict
}

/// <summary>
/// 记录已映射的 HTTP 路由与动词，避免生成路径与运行时兜底重复。
/// </summary>
public sealed class HttpRegistry
{
	private readonly object _gate = new();
	private readonly Dictionary<string, bool> _keys = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>预留路由与动词。</summary>
	/// <param name="routePattern">路由模板。</param>
	/// <param name="methods">HTTP 方法。</param>
	/// <param name="generated">是否由生成代码预留。</param>
	/// <returns>预留结果。</returns>
	public HttpReservation TryReserve(string routePattern, IEnumerable<string> methods, bool generated)
	{
		return TryReserve(routePattern, methods, generated, out _);
	}

	/// <summary>
	/// 预留路由与动词，并返回本次实际可映射的方法。
	/// </summary>
	/// <param name="routePattern">路由模板。</param>
	/// <param name="methods">HTTP 方法。</param>
	/// <param name="generated">是否由生成代码预留。</param>
	/// <param name="availableMethods">本次实际可映射的方法；已全部映射时为空。</param>
	/// <returns>预留结果。</returns>
	public HttpReservation TryReserve(string routePattern, IEnumerable<string> methods, bool generated, out IReadOnlyList<string> availableMethods)
	{
		var requested = methods.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
		var keys = requested.Select(method => $"{routePattern}|{method}").ToArray();
		lock (_gate)
		{
			var existing = keys.Where(_keys.ContainsKey).ToArray();
			if (existing.Length == 0)
			{
				foreach (var key in keys)
				{
					_keys[key] = generated;
				}

				availableMethods = requested;
				return HttpReservation.Reserved;
			}

			if (generated)
			{
				availableMethods = Array.Empty<string>();
				return HttpReservation.Conflict;
			}

			if (existing.Length == keys.Length)
			{
				availableMethods = Array.Empty<string>();
				return existing.All(key => _keys[key])
					? HttpReservation.AlreadyGenerated
					: HttpReservation.Conflict;
			}

			if (!existing.All(key => _keys[key]))
			{
				availableMethods = Array.Empty<string>();
				return HttpReservation.Conflict;
			}

			var available = new List<string>();
			for (var index = 0; index < keys.Length; index++)
			{
				if (_keys.ContainsKey(keys[index]))
				{
					continue;
				}

				_keys[keys[index]] = false;
				available.Add(requested[index]);
			}

			availableMethods = available;
			return HttpReservation.Reserved;
		}
	}
}
