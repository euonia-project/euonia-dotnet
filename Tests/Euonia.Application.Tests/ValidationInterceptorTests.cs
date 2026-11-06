using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 验证拦截器测试使用的服务契约，覆盖普通参数、<c>[NotNull]</c> 参数与 <c>[Validation]</c> 参数。
/// </summary>
public interface IValidationTestService
{
	/// <summary>
	/// 保存字符串值，用于覆盖无验证特性的普通方法。
	/// </summary>
	/// <param name="value">待保存的值。</param>
	void Save(string value);

	/// <summary>
	/// 异步返回传入的字符串；<paramref name="value"/> 标注 <c>[NotNull]</c>，用于验证空参数拦截。
	/// </summary>
	/// <param name="value">待处理的值。</param>
	/// <returns>原样返回的 <paramref name="value"/>。</returns>
	Task<string> ProcessAsync([NotNull] string value);

	/// <summary>
	/// 返回载荷名称；<paramref name="payload"/> 标注 <c>[Validation]</c>，用于验证对象校验拦截。
	/// </summary>
	/// <param name="payload">待校验的载荷。</param>
	/// <returns>载荷名称。</returns>
	Task<string> ProcessValidatable([Validation] ValidatablePayload payload);
}

/// <summary>
/// 实现 <see cref="IValidatableObject"/> 的测试载荷，名称为空时记录校验错误。
/// </summary>
public class ValidatablePayload : IValidatableObject
{
	/// <summary>
	/// 载荷名称，空值视为无效。
	/// </summary>
	public string Name { get; set; } = null!;

	/// <summary>
	/// 校验错误集合。
	/// </summary>
	public ObservableCollection<string> Errors { get; } = [];

	/// <summary>
	/// 指示当前是否没有校验错误。
	/// </summary>
	public bool IsValid => Errors.Count == 0;

	/// <summary>
	/// 检查名称是否为空，为空时向 <see cref="Errors"/> 添加错误。
	/// </summary>
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
	/// <summary>
	/// 空实现，用于验证普通方法可被拦截器代理。
	/// </summary>
	/// <param name="value">待保存的值。</param>
	public virtual void Save(string value)
	{
	}

	/// <summary>
	/// 异步返回传入的字符串，用于验证 <c>[NotNull]</c> 参数检查。
	/// </summary>
	/// <param name="value">待处理的值。</param>
	/// <returns>原样返回的 <paramref name="value"/>。</returns>
	public virtual async Task<string> ProcessAsync([NotNull] string value)
	{
		await Task.Yield();
		return value;
	}

	/// <summary>
	/// 返回载荷名称，用于验证 <c>[Validation]</c> 对象校验。
	/// </summary>
	/// <param name="payload">待校验的载荷。</param>
	/// <returns>载荷名称。</returns>
	public virtual Task<string> ProcessValidatable([Validation] ValidatablePayload payload)
	{
		return Task.FromResult(payload.Name);
	}
}

/// <summary>
/// 针对 <see cref="ValidationInterceptor"/> 参数与对象校验拦截行为的回归测试。
/// </summary>
[Collection("AppTests")]
public class ValidationInterceptorTests
{
	/// <summary>
	/// 向 <c>[NotNull]</c> 参数传入 <see langword="null"/> 应抛出 <see cref="ValidationException"/>。
	/// </summary>
	[Fact]
	public async Task NotNullParameter_WithNullArgument_ShouldThrow()
	{
		// 回归测试：旧的参数类型检查 `ParameterType.IsInstanceOfType(argument)` 对 null 恒返回 false，
		// null 实参在 NotNull 检查之前就被 continue 跳过，[NotNull] 形同虚设。
		// 修复后 null 实参必须抛出 ValidationException。
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IValidationTestService>();

		await Assert.ThrowsAsync<ValidationException>(() => svc.ProcessAsync(null!));
	}

	/// <summary>
	/// 向 <c>[NotNull]</c> 参数传入非空值应正常返回该值。
	/// </summary>
	[Fact]
	public async Task NotNullParameter_WithValueArgument_ShouldPass()
	{
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IValidationTestService>();

		var result = await svc.ProcessAsync("ok");

		Assert.Equal("ok", result);
	}

	/// <summary>
	/// 传入校验不通过的 <c>[Validation]</c> 载荷应抛出 <see cref="ValidationException"/>。
	/// </summary>
	[Fact]
	public async Task ValidationAttribute_WithInvalidPayload_ShouldThrow()
	{
		using var provider = CreateProvider();
		using var scope = provider.CreateScope();
		var svc = scope.ServiceProvider.GetRequiredService<IValidationTestService>();

		await Assert.ThrowsAsync<ValidationException>(
			() => svc.ProcessValidatable(new ValidatablePayload { Name = null! }));
	}

	/// <summary>
	/// 传入合法 <c>[Validation]</c> 载荷应正常返回载荷名称。
	/// </summary>
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