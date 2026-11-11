using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;

namespace Nerosoft.Euonia.Application;

/// <inheritdoc />
public class ApplicationModule : ModuleContextBase
{
	/// <inheritdoc />
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		// 未调用 AddAuditing() 时也注册空记录器：拦截器只做一次 IsEnabled 判断后放行。
		context.Services.TryAddSingleton<IAuditingRecorder>(NullAuditingRecorder.Instance);
		context.Services.AddTransient<IInterceptor, AuditingInterceptor>();
		context.Services.AddTransient<IInterceptor, LoggingInterceptor>();
		context.Services.AddTransient<IInterceptor, AuthorizationInterceptor>();
		context.Services.AddTransient<IInterceptor, ValidationInterceptor>();
		context.Services.AddTransient<IInterceptor, TracingInterceptor>();
		context.Services.AddTransient<IInterceptor, LockInterceptor>();
		context.Services.AddTransient<IInterceptor, TimingInterceptor>();
		context.Services.AddTransient<IInterceptor, CacheInterceptor>();
		context.Services.AddTransient<IInterceptor, CacheEvictionInterceptor>();
		context.Services.AddTransient<IInterceptor, IdempotentInterceptor>();
		context.Services.AddTransient<IInterceptor, RetryInterceptor>();
		context.Services.AddTransient<IInterceptor, CircuitBreakerInterceptor>();
		context.Services.AddSingleton<ICacheGroupManager, CacheGroupManager>();
		context.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
		context.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UserContextBehavior<,>));
		context.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CorrelationIdBehavior<,>));
		context.Services.AddTransient(typeof(IUseCasePresenter<>), typeof(DefaultUseCasePresenter<>));
		context.Services.AddTransient<IUseCaseExecutor, UseCaseExecutor>();
	}
}