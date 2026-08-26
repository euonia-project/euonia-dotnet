using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// An implementation of <see cref="IRequestContextAccessor"/>.
/// </summary>
public class RequestContextAccessor : IRequestContextAccessor
{
	private readonly RequestContextAccessorOptions _options;
	private readonly IServiceAccessor _service;

	/// <summary>
	/// Initializes a new instance of the <see cref="RequestContextAccessor"/> class.
	/// </summary>
	/// <param name="options"></param>
	/// <param name="service"></param>
	public RequestContextAccessor(IOptions<RequestContextAccessorOptions> options, IServiceAccessor service)
	{
		_options = options.Value;
		_service = service;
	}

	/// <summary>
	/// Gets the current request context instance.
	/// </summary>
	public RequestContext Context
	{
		get
		{
			if (_options.UseDefaultAccessor)
			{
				return _service.GetService<DefaultRequestContextAccessor>().Context;
			}
			else
			{
				return _service.GetService<DelegateRequestContextAccessor>().Invoke();
			}
		}
	}
}