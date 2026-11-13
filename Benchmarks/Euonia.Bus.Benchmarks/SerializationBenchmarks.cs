using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus;

namespace Euonia.Bus.Benchmarks;

/// <summary>
/// 对比内置 System.Text.Json 与 Newtonsoft.Json 序列化器的消息往返开销。
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SerializationBenchmarks
{
	private readonly BenchMessage _message = new()
	{
		Id = "order-1",
		Name = "sample",
		Amount = 99.95m,
		Items = ["a", "b", "c"],
	};

	private SystemTextJsonSerializer _systemTextJson = null!;
	private NewtonsoftJsonSerializer _newtonsoftJson = null!;
	private string _systemTextJsonPayload = null!;
	private string _newtonsoftJsonPayload = null!;

	/// <summary>
	/// 创建两个序列化器实例并预先生成两种格式的消息载荷，供后续基准复用。
	/// </summary>
	[GlobalSetup]
	public void Setup()
	{
		var options = Options.Create(new MessageSerializerOptions());
		_systemTextJson = new SystemTextJsonSerializer(options);
		_newtonsoftJson = new NewtonsoftJsonSerializer(options);
		_systemTextJsonPayload = _systemTextJson.Serialize(_message);
		_newtonsoftJsonPayload = _newtonsoftJson.Serialize(_message);
	}

	/// <summary>
	/// 测量使用 System.Text.Json 序列化 <see cref="BenchMessage"/> 的耗时，作为对比基线。
	/// </summary>
	[Benchmark(Baseline = true)]
	public string SystemTextJson_Serialize()
	{
		return _systemTextJson.Serialize(_message);
	}

	/// <summary>
	/// 测量使用 System.Text.Json 将预生成载荷反序列化为 <see cref="BenchMessage"/> 的耗时。
	/// </summary>
	[Benchmark]
	public BenchMessage SystemTextJson_Deserialize()
	{
		return _systemTextJson.Deserialize<BenchMessage>(_systemTextJsonPayload);
	}

	/// <summary>
	/// 测量使用 Newtonsoft.Json 序列化 <see cref="BenchMessage"/> 的耗时。
	/// </summary>
	[Benchmark]
	public string NewtonsoftJson_Serialize()
	{
		return _newtonsoftJson.Serialize(_message);
	}

	/// <summary>
	/// 测量使用 Newtonsoft.Json 将预生成载荷反序列化为 <see cref="BenchMessage"/> 的耗时。
	/// </summary>
	[Benchmark]
	public BenchMessage NewtonsoftJson_Deserialize()
	{
		return _newtonsoftJson.Deserialize<BenchMessage>(_newtonsoftJsonPayload);
	}

	/// <summary>
	/// 序列化基准使用的示例消息，包含常见数据类型以覆盖不同序列化路径。
	/// </summary>
	public sealed class BenchMessage
	{
		/// <summary>
		/// 获取或设置消息标识。
		/// </summary>
		public string Id { get; set; } = string.Empty;

		/// <summary>
		/// 获取或设置消息名称。
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// 获取或设置金额，用于验证十进制数的序列化精度。
		/// </summary>
		public decimal Amount { get; set; }

		/// <summary>
		/// 获取或设置字符串集合，用于验证数组的序列化处理。
		/// </summary>
		public string[] Items { get; set; } = [];
	}
}
