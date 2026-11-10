using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Endpoint;
using Nerosoft.Euonia.Endpoint.Http;

namespace Nerosoft.Euonia.Endpoint.Http.Tests;

/// <summary>
/// 验证 HTTP 端点映射、参数绑定、扫描范围与认证元数据。
/// </summary>
public class HttpEndpointTests
{
	/// <summary>验证端点元数据会继承实现类与实现方法的认证特性。</summary>
	[Fact]
	public void Metadata_InheritsImplementationClassAndMethodAuthorization()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<IAuthEndpointService, AuthEndpointService>();
		services.AddDynamicHttpEndpoint(options =>
		{
			options.EnableExposeApplicationServiceAsWebApi = true;
			options.ThrowOnUnregisteredService = false;
		}, typeof(AuthEndpointService).Assembly);
		using var provider = services.BuildServiceProvider();
		var builder = new TestEndpointRouteBuilder(provider);

		builder.MapDynamicHttpEndpoint();

		var classEndpoint = GetEndpoint(builder, "api/auth/class", "GET");
		Assert.Contains(classEndpoint.Metadata.OfType<AuthorizeAttribute>(), attribute => attribute.Roles == "admin");

		var methodEndpoint = GetEndpoint(builder, "api/auth/method", "GET");
		Assert.Contains(methodEndpoint.Metadata.OfType<AuthorizeAttribute>(), attribute => attribute.Policy == "method-policy");
		Assert.Contains(methodEndpoint.Metadata.OfType<AuthorizeAttribute>(), attribute => attribute.Roles == "admin");
	}

	/// <summary>验证只映射标记了 <c>ExposeWebApiAttribute</c> 的方法。</summary>
	[Fact]
	public void Map_AddsOnlyMarkedEndpoints()
	{
		using var provider = CreateProvider();
		var builder = new TestEndpointRouteBuilder(provider);
		builder.MapDynamicHttpEndpoint();
		var endpoints = GetEndpoints(builder);
		Assert.Equal(4, endpoints.Count);
		Assert.Contains(endpoints, endpoint => endpoint.RoutePattern.RawText == "api/test/{id}" && HasMethod(endpoint, "GET"));
		Assert.Contains(endpoints, endpoint => endpoint.RoutePattern.RawText == "api/test/create" && HasMethod(endpoint, "POST"));
	}

	/// <summary>验证 GET 请求的路由、查询字符串与取消令牌绑定。</summary>
	[Fact]
	public async Task Get_BindsRouteQueryAndCancellation()
	{
		using var provider = CreateProvider();
		var builder = new TestEndpointRouteBuilder(provider);
		builder.MapDynamicHttpEndpoint();
		var endpoint = GetEndpoint(builder, "api/test/{id}", "GET");
		using var scope = provider.CreateScope();
		var context = CreateContext(scope.ServiceProvider, "GET", "/api/test/abc");
		context.Request.RouteValues["id"] = "abc";
		context.Request.QueryString = new QueryString("?value=42");
		await endpoint.RequestDelegate!(context);
		Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
		Assert.Equal("\"abc:42\"", await ReadBodyAsync(context));
	}

	/// <summary>验证 POST 请求的 JSON 请求体绑定。</summary>
	[Fact]
	public async Task Post_BindsJsonBody()
	{
		using var provider = CreateProvider();
		var builder = new TestEndpointRouteBuilder(provider);
		builder.MapDynamicHttpEndpoint();
		var endpoint = GetEndpoint(builder, "api/test/create", "POST");
		using var scope = provider.CreateScope();
		var context = CreateContext(scope.ServiceProvider, "POST", "/api/test/create");
		var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TestInput { Name = "hello" }));
		context.Request.ContentType = "application/json";
		context.Request.Body = new MemoryStream(payload);
		context.Request.ContentLength = payload.Length;
		await endpoint.RequestDelegate!(context);
		Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
		Assert.Equal("\"hello\"", await ReadBodyAsync(context));
	}

	/// <summary>验证生成端点对空请求体返回 400。</summary>
	[Fact]
	public async Task Post_EmptyBody_ReturnsBadRequest()
	{
		using var provider = CreateProvider();
		var builder = new TestEndpointRouteBuilder(provider);
		builder.MapDynamicHttpEndpoint();
		var endpoint = GetEndpoint(builder, "api/test/create", "POST");
		using var scope = provider.CreateScope();
		var context = CreateContext(scope.ServiceProvider, "POST", "/api/test/create");
		context.Request.ContentLength = 0;

		await endpoint.RequestDelegate!(context);

		Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
	}

	/// <summary>验证生成端点对 JSON <c>null</c> 请求体返回 400。</summary>
	[Fact]
	public async Task Post_NullBody_ReturnsBadRequest()
	{
		using var provider = CreateProvider();
		var builder = new TestEndpointRouteBuilder(provider);
		builder.MapDynamicHttpEndpoint();
		var endpoint = GetEndpoint(builder, "api/test/create", "POST");
		using var scope = provider.CreateScope();
		var context = CreateContext(scope.ServiceProvider, "POST", "/api/test/create");
		var payload = Encoding.UTF8.GetBytes("null");
		context.Request.ContentType = "application/json";
		context.Request.Body = new MemoryStream(payload);
		context.Request.ContentLength = payload.Length;

		await endpoint.RequestDelegate!(context);

		Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
	}

	/// <summary>验证生成端点对非 JSON Content-Type 返回 400。</summary>
	[Fact]
	public async Task Post_NonJsonContentType_ReturnsBadRequest()
	{
		using var provider = CreateProvider();
		var builder = new TestEndpointRouteBuilder(provider);
		builder.MapDynamicHttpEndpoint();
		var endpoint = GetEndpoint(builder, "api/test/create", "POST");
		using var scope = provider.CreateScope();
		var context = CreateContext(scope.ServiceProvider, "POST", "/api/test/create");
		var payload = Encoding.UTF8.GetBytes("{}");
		context.Request.ContentType = "text/plain";
		context.Request.Body = new MemoryStream(payload);
		context.Request.ContentLength = payload.Length;

		await endpoint.RequestDelegate!(context);

		Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
	}

	/// <summary>验证 <c>ScanTypes</c> 同样过滤生成路径。</summary>
	[Fact]
	public void ScanTypes_FiltersGeneratedEndpoints()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<ITestEndpointService, TestEndpointService>();
		services.AddScoped<IAuthEndpointService, AuthEndpointService>();
		services.AddDynamicHttpEndpoint(options =>
		{
			options.EnableExposeApplicationServiceAsWebApi = true;
			options.ThrowOnUnregisteredService = false;
			options.ScanTypes = new[] { typeof(ITestEndpointService).FullName! };
		}, typeof(TestEndpointService).Assembly);
		using var provider = services.BuildServiceProvider();
		var builder = new TestEndpointRouteBuilder(provider);

		builder.MapDynamicHttpEndpoint();

		var endpoints = GetEndpoints(builder);
		Assert.Equal(4, endpoints.Count);
		Assert.DoesNotContain(endpoints, endpoint => endpoint.RoutePattern.RawText!.StartsWith("api/auth", StringComparison.Ordinal));
	}

	/// <summary>验证生成路径与运行时兜底的路由预留语义。</summary>
	[Fact]
	public void Registry_DistinguishesGeneratedAndRuntimeReservations()
	{
		var generatedFirst = new HttpRegistry();
		Assert.Equal(HttpReservation.Reserved, generatedFirst.TryReserve("api/registry", new[] { "GET" }, true));
		Assert.Equal(HttpReservation.AlreadyGenerated, generatedFirst.TryReserve("api/registry", new[] { "GET" }, false));

		var runtimeConflict = new HttpRegistry();
		Assert.Equal(HttpReservation.Reserved, runtimeConflict.TryReserve("api/registry", new[] { "GET" }, false));
		Assert.Equal(HttpReservation.Conflict, runtimeConflict.TryReserve("api/registry", new[] { "GET" }, false));

		var partial = new HttpRegistry();
		Assert.Equal(HttpReservation.Reserved, partial.TryReserve("api/partial", new[] { "PUT" }, true));
		Assert.Equal(HttpReservation.Reserved, partial.TryReserve("api/partial", new[] { "PUT", "PATCH" }, false, out var available));
		Assert.Equal(new[] { "PATCH" }, available);
	}

	/// <summary>验证必填引用类型参数在空请求体时绑定失败。</summary>
	[Fact]
	public async Task Binder_EmptyRequiredBody_ReturnsFailure()
	{
		using var provider = CreateProvider();
		var exposed = new WebApiMethodScanner().Scan(new[] { typeof(ITestEndpointService) })
			.Single(item => item.Method.Name == nameof(ITestEndpointService.CreateAsync));
		var descriptor = new WebApiActionDescriptorBuilder(new HttpExposureOptions()).Build(exposed);
		var body = descriptor.Parameters.Single(parameter => parameter.Source == WebApiParameterSource.Body);
		using var scope = provider.CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
		context.Request.ContentLength = 0;

		var result = await WebApiParameterBinder.BindAsync(context, body, new JsonSerializerOptions(), CancellationToken.None);

		Assert.False(result.Succeeded);
		Assert.False(result.IsInternal);
	}

	/// <summary>验证必填引用类型参数在 JSON <c>null</c> 时绑定失败。</summary>
	[Fact]
	public async Task Binder_NullRequiredBody_ReturnsFailure()
	{
		using var provider = CreateProvider();
		var exposed = new WebApiMethodScanner().Scan(new[] { typeof(ITestEndpointService) })
			.Single(item => item.Method.Name == nameof(ITestEndpointService.CreateAsync));
		var descriptor = new WebApiActionDescriptorBuilder(new HttpExposureOptions()).Build(exposed);
		var body = descriptor.Parameters.Single(parameter => parameter.Source == WebApiParameterSource.Body);
		using var scope = provider.CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
		var payload = Encoding.UTF8.GetBytes("null");
		context.Request.ContentType = "application/json";
		context.Request.Body = new MemoryStream(payload);
		context.Request.ContentLength = payload.Length;

		var result = await WebApiParameterBinder.BindAsync(context, body, new JsonSerializerOptions(), CancellationToken.None);

		Assert.False(result.Succeeded);
		Assert.False(result.IsInternal);
	}

	/// <summary>验证运行时兜底请求委托可以完成 body 绑定与响应。</summary>
	[Fact]
	public async Task RuntimeFactory_BindsBodyAndReturnsOk()
	{
		using var provider = CreateProvider();
		var exposed = new WebApiMethodScanner().Scan(new[] { typeof(ITestEndpointService) })
			.Single(item => item.Method.Name == nameof(ITestEndpointService.CreateAsync));
		var descriptor = new WebApiActionDescriptorBuilder(new HttpExposureOptions()).Build(exposed);
		var handler = WebApiEndpointFactory.Create(descriptor, provider);
		using var scope = provider.CreateScope();
		var context = CreateContext(scope.ServiceProvider, "POST", "/api/test/create");
		var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new TestInput { Name = "runtime" }));
		context.Request.ContentType = "application/json";
		context.Request.Body = new MemoryStream(payload);
		context.Request.ContentLength = payload.Length;

		await handler(context);

		Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
		Assert.Equal("\"runtime\"", await ReadBodyAsync(context));
	}

	/// <summary>验证多实现时不合并实现类授权，避免 AllowAnonymous 误放开。</summary>
	[Fact]
	public void Metadata_DoesNotMergeAuthorizationAcrossMultipleImplementations()
	{
		var registry = new HttpMetadataRegistry();
		registry.Build(new[] { typeof(MultiAuthImplementationOne).Assembly });

		var metadata = registry.GetMetadata(
			typeof(IMultiAuthService),
			typeof(IMultiAuthService).GetMethod(nameof(IMultiAuthService.GetAsync))!);

		Assert.DoesNotContain(metadata, item => item is AllowAnonymousAttribute);
		Assert.DoesNotContain(metadata, item => item is AuthorizeAttribute);
	}

	/// <summary>验证缺失的可选集合参数返回空集合而不是 null。</summary>
	[Fact]
	public async Task Binder_MissingOptionalCollection_ReturnsEmptyCollection()
	{
		using var provider = CreateProvider();
		var exposed = new WebApiMethodScanner().Scan(new[] { typeof(IOptionalCollectionService) })
			.Single(item => item.Method.Name == nameof(IOptionalCollectionService.CountAsync));
		var descriptor = new WebApiActionDescriptorBuilder(new HttpExposureOptions()).Build(exposed);
		var parameter = descriptor.Parameters.Single();
		using var scope = provider.CreateScope();
		var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

		var result = await WebApiParameterBinder.BindAsync(context, parameter, new JsonSerializerOptions(), CancellationToken.None);

		Assert.True(result.Succeeded);
		var values = Assert.IsType<int[]>(result.Value);
		Assert.Empty(values);
	}

	/// <summary>验证 nint 参数与复杂 body 参数可以同时生成并正常调用。</summary>
	[Fact]
	public async Task Post_WithNintParameter_BindsBodyAndQuery()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<INintEndpointService, NintEndpointService>();
		services.AddDynamicHttpEndpoint(options =>
		{
			options.EnableExposeApplicationServiceAsWebApi = true;
			options.ThrowOnUnregisteredService = false;
		}, typeof(NintEndpointService).Assembly);
		using var provider = services.BuildServiceProvider();
		var builder = new TestEndpointRouteBuilder(provider);
		builder.MapDynamicHttpEndpoint();
		var endpoint = GetEndpoint(builder, "api/nint/mix", "POST");
		using var scope = provider.CreateScope();
		var context = CreateContext(scope.ServiceProvider, "POST", "/api/nint/mix");
		var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new NintInput { Name = "hello" }));
		context.Request.ContentType = "application/json";
		context.Request.Body = new MemoryStream(payload);
		context.Request.ContentLength = payload.Length;
		context.Request.QueryString = new QueryString("?offset=2");

		await endpoint.RequestDelegate!(context);

		Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
		Assert.Equal("\"hello\"", await ReadBodyAsync(context));
	}

	/// <summary>验证生成路径把简单集合绑定为 query。</summary>
	[Fact]
	public async Task Get_BindsSimpleCollectionFromQuery()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<IArrayEndpointService, ArrayEndpointService>();
		services.AddDynamicHttpEndpoint(options =>
		{
			options.EnableExposeApplicationServiceAsWebApi = true;
			options.ThrowOnUnregisteredService = false;
		}, typeof(ArrayEndpointService).Assembly);
		using var provider = services.BuildServiceProvider();
		var builder = new TestEndpointRouteBuilder(provider);
		builder.MapDynamicHttpEndpoint();
		var endpoint = GetEndpoint(builder, "api/array/count", "GET");
		using var scope = provider.CreateScope();
		var context = CreateContext(scope.ServiceProvider, "GET", "/api/array/count");
		context.Request.QueryString = new QueryString("?ids=1&ids=2");

		await endpoint.RequestDelegate!(context);

		Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
		Assert.Equal("2", await ReadBodyAsync(context));
	}

	/// <summary>验证配置的扫描程序集范围生效。</summary>
	[Fact]
	public void ConfigurationScanScope_IsUsed()
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["Euonia:Endpoint:Http:EnableExposeApplicationServiceAsWebApi"] = "true",
				["Euonia:Endpoint:Http:ThrowOnUnregisteredService"] = "false",
				["Euonia:Endpoint:Http:ScanEntryAssembly"] = "false",
				["Euonia:Endpoint:Http:ScanAssemblies:0"] = typeof(TestEndpointService).Assembly.GetName().Name
			})
			.Build();

		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<ITestEndpointService, TestEndpointService>();
		services.AddDynamicHttpEndpoint(configuration);
		using var provider = services.BuildServiceProvider();
		var builder = new TestEndpointRouteBuilder(provider);

		builder.MapDynamicHttpEndpoint();

		Assert.NotEmpty(GetEndpoints(builder));
	}

	/// <summary>验证关闭入口程序集扫描后不产生端点。</summary>
	[Fact]
	public void DisabledEntryAssembly_AddsNoEndpoints()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<ITestEndpointService, TestEndpointService>();
		services.AddDynamicHttpEndpoint(options =>
		{
			options.EnableExposeApplicationServiceAsWebApi = true;
			options.ThrowOnUnregisteredService = false;
			options.ScanEntryAssembly = false;
		});
		using var provider = services.BuildServiceProvider();
		var builder = new TestEndpointRouteBuilder(provider);

		builder.MapDynamicHttpEndpoint();

		Assert.Empty(GetEndpoints(builder));
	}

	/// <summary>验证功能开关关闭时不映射端点。</summary>
	[Fact]
	public void DisabledToggle_AddsNoEndpoints()
	{
		using var provider = CreateProvider(options => options.EnableExposeApplicationServiceAsWebApi = false);
		var builder = new TestEndpointRouteBuilder(provider);
		builder.MapDynamicHttpEndpoint();
		Assert.Empty(GetEndpoints(builder));
	}

	/// <summary>验证接口方法上的授权元数据会复制到端点。</summary>
	[Fact]
	public void AuthorizeMetadata_IsCopied()
	{
		using var provider = CreateProvider();
		var builder = new TestEndpointRouteBuilder(provider);
		builder.MapDynamicHttpEndpoint();
		var endpoint = GetEndpoint(builder, "api/test/{id}", "GET");
		Assert.Contains(endpoint.Metadata, metadata => metadata is AuthorizeAttribute);
	}

	private static ServiceProvider CreateProvider(Action<HttpExposureOptions>? configure = null)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<ITestEndpointService, TestEndpointService>();
		services.AddDynamicHttpEndpoint(options =>
		{
			options.EnableExposeApplicationServiceAsWebApi = true;
			options.ThrowOnUnregisteredService = false;
			configure?.Invoke(options);
		}, typeof(TestEndpointService).Assembly);
		return services.BuildServiceProvider();
	}

	private static RouteEndpoint GetEndpoint(IEndpointRouteBuilder builder, string route, string method)
	{
		return GetEndpoints(builder).Single(endpoint => endpoint.RoutePattern.RawText == route && HasMethod(endpoint, method));
	}

	private static bool HasMethod(RouteEndpoint endpoint, string method)
	{
		return endpoint.Metadata.OfType<HttpMethodMetadata>().Any(metadata => metadata.HttpMethods.Any(value => string.Equals(value, method, StringComparison.OrdinalIgnoreCase)));
	}

	private static IReadOnlyList<RouteEndpoint> GetEndpoints(IEndpointRouteBuilder builder)
	{
		return builder.DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().ToArray();
	}

	private static DefaultHttpContext CreateContext(IServiceProvider provider, string method, string path)
	{
		return new DefaultHttpContext
		{
			RequestServices = provider,
			Request = { Method = method, Path = path },
			Response = { Body = new MemoryStream() }
		};
	}

	private static async Task<string> ReadBodyAsync(HttpContext context)
	{
		context.Response.Body.Position = 0;
		return await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
	}

	private sealed class TestEndpointRouteBuilder(IServiceProvider provider) : IEndpointRouteBuilder
	{
		public IServiceProvider ServiceProvider { get; } = provider;
		public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();
		public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
	}
}

/// <summary>
/// 测试用输入 DTO。
/// </summary>
public sealed class TestInput
{
	/// <summary>获取或设置名称。</summary>
	public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 测试用 HTTP 端点服务契约。
/// </summary>
public interface ITestEndpointService
{
	/// <summary>读取测试值。</summary>
	/// <param name="id">路由标识。</param>
	/// <param name="value">查询值。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>拼接后的字符串。</returns>
	[ExposeWebApi(Prefix = "api/test", Route = "{id}", Method = new[] { "GET" })]
	[Authorize]
	Task<string> GetAsync(string id, int value, CancellationToken cancellationToken = default);

	/// <summary>创建测试值。</summary>
	/// <param name="input">输入 DTO。</param>
	/// <returns>输入名称。</returns>
	[ExposeWebApi(Prefix = "api/test", Method = new[] { "POST" })]
	Task<string> CreateAsync(TestInput input);

	/// <summary>更新测试值。</summary>
	/// <param name="id">路由标识。</param>
	/// <param name="input">输入 DTO。</param>
	/// <returns>表示异步操作的任务。</returns>
	[ExposeWebApi(Prefix = "api/test", Route = "{id}", Method = new[] { "PUT", "PATCH" })]
	Task UpdateAsync(string id, TestInput input);

	/// <summary>删除测试值。</summary>
	/// <returns>表示异步操作的任务。</returns>
	[ExposeWebApi(Prefix = "api/test", Route = "delete", Method = new[] { "DELETE" })]
	Task DeleteAsync();
}

/// <summary>
/// <see cref="ITestEndpointService"/> 的测试实现。
/// </summary>
public sealed class TestEndpointService : ITestEndpointService
{
	/// <inheritdoc/>
	public Task<string> GetAsync(string id, int value, CancellationToken cancellationToken = default) => Task.FromResult($"{id}:{value}");
	/// <inheritdoc/>
	public Task<string> CreateAsync(TestInput input) => Task.FromResult(input.Name);
	/// <inheritdoc/>
	public Task UpdateAsync(string id, TestInput input) => Task.CompletedTask;
	/// <inheritdoc/>
	public Task DeleteAsync() => Task.CompletedTask;
}

/// <summary>
/// 测试用认证端点服务契约。
/// </summary>
public interface IAuthEndpointService
{
	/// <summary>读取类级授权结果。</summary>
	/// <returns>固定字符串。</returns>
	[ExposeWebApi(Prefix = "api/auth", Route = "class", Method = new[] { "GET" })]
	Task<string> ClassAuthAsync();

	/// <summary>读取方法级授权结果。</summary>
	/// <returns>固定字符串。</returns>
	[ExposeWebApi(Prefix = "api/auth", Route = "method", Method = new[] { "GET" })]
	Task<string> MethodAuthAsync();
}

/// <summary>
/// 测试用保留前缀参数名接口。
/// </summary>
public interface IReservedNameEndpointService
{
	/// <summary>回显查询值。</summary>
	/// <param name="__http">以保留前缀开头的参数名。</param>
	/// <returns>输入值。</returns>
	[ExposeWebApi(Prefix = "api/reserved", Route = "echo", Method = new[] { "GET" })]
	Task<string> EchoAsync(string __http);
}

/// <summary>
/// 测试用 nint 接口。
/// </summary>
public interface INintEndpointService
{
	/// <summary>混合复杂 body 与 nint 参数。</summary>
	/// <param name="input">输入 DTO。</param>
	/// <param name="offset">原生整数偏移。</param>
	/// <returns>输入名称。</returns>
	[ExposeWebApi(Prefix = "api/nint", Route = "mix", Method = new[] { "POST" })]
	Task<string> MixAsync(NintInput input, nint offset);
}

/// <summary>
/// <see cref="INintEndpointService"/> 的测试实现。
/// </summary>
public sealed class NintEndpointService : INintEndpointService
{
	/// <inheritdoc/>
	public Task<string> MixAsync(NintInput input, nint offset) => Task.FromResult(input.Name);
}

/// <summary>
/// 测试用 nint 输入 DTO。
/// </summary>
public sealed class NintInput
{
	/// <summary>获取或设置名称。</summary>
	public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 测试用可选集合接口。
/// </summary>
public interface IOptionalCollectionService
{
	/// <summary>统计数组长度。</summary>
	/// <param name="ids">可选数组参数。</param>
	/// <returns>数组长度。</returns>
	[ExposeWebApi(Prefix = "api/optional", Route = "count", Method = new[] { "GET" })]
	Task<int> CountAsync(int[]? ids = null);
}

/// <summary>
/// 测试用简单集合接口。
/// </summary>
public interface IArrayEndpointService
{
	/// <summary>统计数组长度。</summary>
	/// <param name="ids">查询字符串中的数组。</param>
	/// <returns>数组长度。</returns>
	[ExposeWebApi(Prefix = "api/array", Route = "count", Method = new[] { "GET" })]
	Task<int> CountAsync(int[] ids);
}

/// <summary>
/// <see cref="IArrayEndpointService"/> 的测试实现。
/// </summary>
public sealed class ArrayEndpointService : IArrayEndpointService
{
	/// <inheritdoc/>
	public Task<int> CountAsync(int[] ids) => Task.FromResult(ids.Length);
}

/// <summary>
/// 测试用多实现接口。
/// </summary>
public interface IMultiAuthService
{
	/// <summary>读取测试值。</summary>
	/// <returns>固定字符串。</returns>
	Task<string> GetAsync();
}

/// <summary>
/// <see cref="IMultiAuthService"/> 的第一个实现，要求 admin 角色。
/// </summary>
[Authorize(Roles = "admin")]
public sealed class MultiAuthImplementationOne : IMultiAuthService
{
	/// <inheritdoc/>
	public Task<string> GetAsync() => Task.FromResult("one");
}

/// <summary>
/// <see cref="IMultiAuthService"/> 的第二个实现，允许匿名。
/// </summary>
[AllowAnonymous]
public sealed class MultiAuthImplementationTwo : IMultiAuthService
{
	/// <inheritdoc/>
	public Task<string> GetAsync() => Task.FromResult("two");
}

/// <summary>
/// <see cref="IAuthEndpointService"/> 的测试实现，类级要求 admin 角色。
/// </summary>
[Authorize(Roles = "admin")]
public sealed class AuthEndpointService : IAuthEndpointService
{
	/// <inheritdoc/>
	public Task<string> ClassAuthAsync() => Task.FromResult("class");

	/// <inheritdoc/>
	[Authorize(Policy = "method-policy")]
	public Task<string> MethodAuthAsync() => Task.FromResult("method");
}
