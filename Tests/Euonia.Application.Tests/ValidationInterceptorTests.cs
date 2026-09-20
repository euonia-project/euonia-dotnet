using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Application.Tests;

public interface IValidationTestService
{
	void Save(string value);

	Task<string> ProcessAsync([NotNull] string value);

	Task<string> ProcessValidatable([Validation] ValidatablePayload payload);
}

public class ValidatablePayload : IValidatableObject
{
	public string Name { get; set; }

	public ObservableCollection<string> Errors { get; } = [];

	public bool IsValid => Errors.Count == 0;

	public void Validate()
	{
		if (string.IsNullOrEmpty(Name))
		{
			Errors.Add("Name is required.");
		}
	}
}

/// <summary>
/// 验证拦截器回归测试。
/// [NotNull] 特性标注在实现类方法参数上（接口代理下必须能命中）。
/// </summary>
public class ValidationTestService : BaseApplicationService, IValidationTestService
{
	public virtual void Save(string value)
	{
	}

	public virtual async Task<string> ProcessAsync([NotNull] string value)
	{
		await Task.Yield();
		return value;
	}

	public virtual Task<string> ProcessValidatable([Validation] ValidatablePayload payload)
	{
		return Task.FromResult(payload.Name);
	}
}

[Collection("AppTests")]
public class ValidationInterceptorTests
{
	[Fact]
	public async Task NotNullParameter_WithNullArgument_ShouldThrow()
	{
		// 回归测试：旧的参数类型检查 `ParameterType.IsInstanceOfType(argument)` 对 null 恒返回 false，
		// null 实参在 NotNull 检查之前就被 continue 跳过，[NotNull] 形同虚设。
		// 修复后 null 实参必须抛出 ValidationException。
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IValidationTestService>();

		await Assert.ThrowsAsync<ValidationException>(() => svc.ProcessAsync(null));
	}

	[Fact]
	public async Task NotNullParameter_WithValueArgument_ShouldPass()
	{
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IValidationTestService>();

		var result = await svc.ProcessAsync("ok");

		Assert.Equal("ok", result);
	}

	[Fact]
	public async Task ValidationAttribute_WithInvalidPayload_ShouldThrow()
	{
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IValidationTestService>();

		await Assert.ThrowsAsync<ValidationException>(
			() => svc.ProcessValidatable(new ValidatablePayload { Name = null }));
	}

	[Fact]
	public async Task ValidationAttribute_WithValidPayload_ShouldPass()
	{
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IValidationTestService>();

		var result = await svc.ProcessValidatable(new ValidatablePayload { Name = "ok" });

		Assert.Equal("ok", result);
	}

	private static ServiceProvider CreateProvider()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<Castle.DynamicProxy.ProxyGenerator>();
		services.AddTransient<IInterceptor, ValidationInterceptor>();
		services.AddApplicationService(typeof(ValidationTestService).Assembly, ServiceLifetime.Scoped);
		return services.BuildServiceProvider();
	}
}