using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 一次消息处理使用的执行作用域：获取或复用服务提供程序、安装请求上下文，
/// 并在释放时恢复请求上下文、只释放自身拥有的作用域。
/// </summary>
/// <remarks>
/// 本类型不修改任何环境服务访问器：<c>IServiceAccessor</c> 已是作用域服务，从当前作用域解析即得到正确提供程序。
/// <para>
/// 处理作用域在处理器上下文返回时释放；处理器内启动且未等待的后台任务会观察到已释放的作用域，
/// 需要延后执行的工作应自行创建作用域。
/// </para>
/// </remarks>
internal sealed class MessageExecutionScope : IDisposable, IAsyncDisposable
{
	private readonly DefaultRequestContextAccessor _requestContextAccessor;
	private readonly RequestContext _inheritedRequestContext;
	private readonly bool _requestContextInstalled;
	private readonly IServiceScope _ownedScope;
	private bool _disposed;

	internal MessageExecutionScope(
		IServiceProvider container,
		IServiceScopeFactory scopeFactory,
		MessageExecutionDescriptor descriptor,
		MessageContext message,
		ILogger logger)
	{
		ServiceProvider = AcquireProvider(container, scopeFactory, descriptor, message, logger, out var ownedScope);
		_ownedScope = ownedScope;

		try
		{
			_requestContextAccessor = ServiceProvider.GetService<DefaultRequestContextAccessor>();
			if (_requestContextAccessor == null)
			{
				return;
			}

			_inheritedRequestContext = _requestContextAccessor.Context;

			switch (descriptor.RequestContextSource)
			{
				case MessageRequestContextSource.Inherit:
					break;

				case MessageRequestContextSource.Envelope when _inheritedRequestContext == null:
					var envelopeContext = CreateFromEnvelope(message, ServiceProvider);
					if (envelopeContext != null)
					{
						_requestContextAccessor.Context = envelopeContext;
						_requestContextInstalled = true;
					}

					break;

				case MessageRequestContextSource.EnvelopeOverride:
					_requestContextAccessor.Context = CreateFromEnvelope(message, ServiceProvider);
					_requestContextInstalled = true;
					break;

				case MessageRequestContextSource.PublisherSnapshot:
					// 快照为空也必须安装（可能为 null），否则后台泵会沿用上一条消息的上下文。
					_requestContextAccessor.Context = descriptor.PublisherContext != null
						                                  ? descriptor.CreateRequestContext(ServiceProvider)
						                                  : CreateFromEnvelope(message, ServiceProvider);
					_requestContextInstalled = true;
					break;
			}
		}
		catch
		{
			// 构造失败时对象不会被释放，必须在此恢复已修改的上下文并释放自有作用域。
			RestoreRequestContext();
			_ownedScope?.Dispose();
			throw;
		}
	}

	/// <summary>
	/// 获取本次消息处理实际使用的服务提供程序。
	/// </summary>
	internal IServiceProvider ServiceProvider { get; }

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		RestoreRequestContext();
		_ownedScope?.Dispose();
	}

	/// <inheritdoc/>
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		RestoreRequestContext();

		if (_ownedScope is IAsyncDisposable asyncScope)
		{
			await asyncScope.DisposeAsync().ConfigureAwait(false);
		}
		else
		{
			_ownedScope?.Dispose();
		}
	}

	/// <summary>
	/// 恢复本作用域修改过的请求上下文。
	/// </summary>
	private void RestoreRequestContext()
	{
		if (_requestContextInstalled)
		{
			_requestContextAccessor.Context = _inheritedRequestContext;
		}
	}

	/// <summary>
	/// 按描述符获取服务提供程序：复用调用方作用域或创建本次执行专用作用域。
	/// </summary>
	private static IServiceProvider AcquireProvider(
		IServiceProvider container,
		IServiceScopeFactory scopeFactory,
		MessageExecutionDescriptor descriptor,
		MessageContext message,
		ILogger logger,
		out IServiceScope ownedScope)
	{
		ownedScope = null;

		if (descriptor.ProviderLease == MessageProviderLease.Borrow && descriptor.BorrowedProvider != null)
		{
			if (ReferenceEquals(container, descriptor.BorrowedProvider))
			{
				logger?.LogWarning("Borrowed service provider for message {MessageId} is the root container; a new handling scope will be created.", message?.MessageId);
			}
			else
			{
				try
				{
					// 探测提供程序是否已释放：已释放的作用域在解析服务时会抛出 ObjectDisposedException。
					_ = descriptor.BorrowedProvider.GetService<IServiceScopeFactory>();
					return descriptor.BorrowedProvider;
				}
				catch (ObjectDisposedException exception)
				{
					logger?.LogWarning(exception, "Borrowed service provider for message {MessageId} has already been disposed; a new handling scope will be created.", message?.MessageId);
				}
			}
		}

		ownedScope = scopeFactory.CreateScope();
		return ownedScope.ServiceProvider;
	}

	/// <summary>
	/// 从消息信封恢复请求上下文；信封未携带可用信息时返回 <c>null</c>。
	/// </summary>
	private static RequestContext CreateFromEnvelope(IMessageContext message, IServiceProvider requestServices)
	{
		if (message == null)
		{
			return null;
		}

		var authorization = message.Authorization;
		var user = message.User as ClaimsPrincipal;
		var traceId = message.RequestTraceId;

		if (string.IsNullOrWhiteSpace(authorization) && string.IsNullOrWhiteSpace(traceId) && user == null)
		{
			return null;
		}

		return new RequestContext
		{
			TraceIdentifier = traceId,
			User = user,
			RequestServices = requestServices,
			Headers = string.IsNullOrWhiteSpace(authorization)
				          ? null
				          : new Dictionary<string, string> { [nameof(RequestContext.Authorization)] = authorization },
		};
	}
}
