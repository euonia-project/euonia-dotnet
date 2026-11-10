using System.Runtime.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Endpoint;
using Nerosoft.Euonia.Endpoint.Grpc;

namespace Nerosoft.Euonia.Endpoint.Grpc.Tests;

/// <summary>
/// gRPC 测试请求 DTO。
/// </summary>
[DataContract]
public sealed class EchoRequest
{
	/// <summary>获取或设置请求值。</summary>
	[DataMember(Order = 1)]
	public string Value { get; set; } = string.Empty;
}

/// <summary>
/// gRPC 测试响应 DTO。
/// </summary>
[DataContract]
public sealed class EchoReply
{
	/// <summary>获取或设置响应值。</summary>
	[DataMember(Order = 1)]
	public string Value { get; set; } = string.Empty;
}

/// <summary>
/// gRPC 测试服务契约。
/// </summary>
public interface IEchoService
{
	/// <summary>回显请求。</summary>
	/// <param name="request">请求 DTO。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>回显响应。</returns>
	[ExposeGrpc(Prefix = "test.EchoService", Route = "Echo", Method = GrpcMethod.Unary)]
	Task<EchoReply> EchoAsync(EchoRequest request, CancellationToken cancellationToken = default);

	/// <summary>服务端流式回显。</summary>
	/// <param name="request">请求 DTO。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>响应流。</returns>
	[ExposeGrpc(Prefix = "test.EchoService", Route = "Watch", Method = GrpcMethod.ServerStreaming)]
	IAsyncEnumerable<EchoReply> WatchAsync(EchoRequest request, CancellationToken cancellationToken = default);

	/// <summary>客户端流式上传。</summary>
	/// <param name="request">请求流。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>响应 DTO。</returns>
	[ExposeGrpc(Prefix = "test.EchoService", Route = "Upload", Method = GrpcMethod.ClientStreaming)]
	Task<EchoReply> UploadAsync(IAsyncEnumerable<EchoRequest> request, CancellationToken cancellationToken = default);

	/// <summary>双向流式回显。</summary>
	/// <param name="request">请求流。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>响应流。</returns>
	[ExposeGrpc(Prefix = "test.EchoService", Route = "Chat", Method = GrpcMethod.DuplexStreaming)]
	IAsyncEnumerable<EchoReply> ChatAsync(IAsyncEnumerable<EchoRequest> request, CancellationToken cancellationToken = default);
}

/// <summary>
/// <see cref="IEchoService"/> 的测试实现。
/// </summary>
[Authorize(Roles = "grpc-admin")]
public sealed class EchoService : IEchoService
{
	/// <inheritdoc/>
	[Authorize(Policy = "grpc-method-policy")]
	public Task<EchoReply> EchoAsync(EchoRequest request, CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new EchoReply { Value = request.Value });
	}

	/// <inheritdoc/>
	public async IAsyncEnumerable<EchoReply> WatchAsync(EchoRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		yield return new EchoReply { Value = request.Value };
		await Task.CompletedTask;
	}

	/// <inheritdoc/>
	public async Task<EchoReply> UploadAsync(IAsyncEnumerable<EchoRequest> request, CancellationToken cancellationToken = default)
	{
		await foreach (var item in request.WithCancellation(cancellationToken))
		{
			return new EchoReply { Value = item.Value };
		}
		return new EchoReply();
	}

	/// <inheritdoc/>
	public async IAsyncEnumerable<EchoReply> ChatAsync(IAsyncEnumerable<EchoRequest> request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await foreach (var item in request.WithCancellation(cancellationToken))
		{
			yield return new EchoReply { Value = item.Value };
		}
	}
}

/// <summary>
/// 验证 gRPC 契约/适配器生成与 proto 导出。
/// </summary>
public class GrpcGenerationTests
{
	/// <summary>验证生成的适配器会调用应用服务。</summary>
	[Fact]
	public async Task GeneratedAdapter_ShouldCallApplicationService()
	{
		var adapter = new Nerosoft.Euonia.Endpoint.Generated.EchoServiceGrpcAdapter0(new EchoService());
		var request = new Nerosoft.Euonia.Endpoint.Generated.IEchoServiceGrpcContract0_EchoRequest
		{
			Request = new EchoRequest { Value = "hello" }
		};

		var reply = await adapter.EchoAsync(request);

		Assert.Equal("hello", reply.Value);
	}

	/// <summary>验证 1970 年之前的 TimestampValue 仍保持正确的小数秒。</summary>
	[Fact]
	public void TimestampValue_HandlesPreEpochFraction()
	{
		var value = new DateTime(1969, 12, 31, 23, 59, 59, 500, DateTimeKind.Utc);

		TimestampValue timestamp = value;

		Assert.Equal(-1, timestamp.Seconds);
		Assert.Equal(500_000_000, timestamp.Nanos);
		Assert.Equal(value, (DateTime)timestamp);
	}

	/// <summary>验证负 DurationValue 使用 floor 语义并保持正 Nanos。</summary>
	[Fact]
	public void DurationValue_HandlesNegativeFraction()
	{
		var value = TimeSpan.FromMilliseconds(-500);

		DurationValue duration = value;

		Assert.Equal(-1, duration.Seconds);
		Assert.Equal(500_000_000, duration.Nanos);
		Assert.Equal(value, (TimeSpan)duration);
	}

	/// <summary>验证 gRPC 注册会添加方法授权拦截器与描述符。</summary>
	[Fact]
	public void AddDynamicGrpcEndpoint_RegistersMethodAuthorizationInterceptor()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddDynamicGrpcEndpoint(options => options.EnableExposeApplicationServiceAsGrpc = true);

		using var provider = services.BuildServiceProvider();

		Assert.NotNull(provider.GetService<GrpcMethodAuthorizationInterceptor>());
		Assert.Contains(provider.GetServices<GrpcMethodAuthorizationDescriptor>(), descriptor => descriptor.MethodPath.EndsWith("/Echo", StringComparison.Ordinal));
	}

	/// <summary>验证 gRPC 元数据注册表读取实现方法授权特性。</summary>
	[Fact]
	public void MetadataRegistry_ReadsImplementationMethodAuthorization()
	{
		var registry = new GrpcMetadataRegistry();
		registry.Build(new[] { typeof(EchoService).Assembly });

		var metadata = registry.GetMethodMetadata(typeof(IEchoService), nameof(IEchoService.EchoAsync));

		Assert.Contains(metadata.OfType<AuthorizeAttribute>(), attribute => attribute.Policy == "grpc-method-policy");
	}

	/// <summary>验证 gRPC 元数据注册表读取实现类授权特性。</summary>
	[Fact]
	public void MetadataRegistry_ReadsImplementationAuthorization()
	{
		var registry = new GrpcMetadataRegistry();
		registry.Build(new[] { typeof(EchoService).Assembly });

		var metadata = registry.GetServiceMetadata(typeof(IEchoService));

		Assert.Contains(metadata.OfType<AuthorizeAttribute>(), attribute => attribute.Roles == "grpc-admin");
	}

	/// <summary>验证 proto 导出会生成 schema 文件。</summary>
	[Fact]
	public void ProtoExporter_ShouldGenerateSchema()
	{
		var directory = Path.Combine(Path.GetTempPath(), "euonia-endpoint-proto-" + Guid.NewGuid().ToString("N"));
		try
		{
			var files = EndpointProtoExporter.Export(typeof(GrpcGenerationTests).Assembly, directory);
			Assert.NotEmpty(files);
			Assert.Contains(files, file => File.ReadAllText(file).Contains("service"));
		}
		finally
		{
			if (Directory.Exists(directory))
			{
				Directory.Delete(directory, true);
			}
		}
	}
}
