using Nerosoft.Euonia.Bus;

namespace Nerosoft.Euonia.Sample.Persist;

public abstract record PagedQueryRequest<TResult> : IRequest<List<TResult>>
{
	public int Skip { get; set; } = 0;

	public int Take { get; set; } = 20;
}