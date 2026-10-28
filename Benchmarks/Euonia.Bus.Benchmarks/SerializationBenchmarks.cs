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

	private SystemTextJsonSerializer _systemTextJson;
	private NewtonsoftJsonSerializer _newtonsoftJson;
	private string _systemTextJsonPayload;
	private string _newtonsoftJsonPayload;

	[GlobalSetup]
	public void Setup()
	{
		var options = Options.Create(new MessageSerializerOptions());
		_systemTextJson = new SystemTextJsonSerializer(options);
		_newtonsoftJson = new NewtonsoftJsonSerializer(options);
		_systemTextJsonPayload = _systemTextJson.Serialize(_message);
		_newtonsoftJsonPayload = _newtonsoftJson.Serialize(_message);
	}

	[Benchmark(Baseline = true)]
	public string SystemTextJson_Serialize()
	{
		return _systemTextJson.Serialize(_message);
	}

	[Benchmark]
	public BenchMessage SystemTextJson_Deserialize()
	{
		return _systemTextJson.Deserialize<BenchMessage>(_systemTextJsonPayload);
	}

	[Benchmark]
	public string NewtonsoftJson_Serialize()
	{
		return _newtonsoftJson.Serialize(_message);
	}

	[Benchmark]
	public BenchMessage NewtonsoftJson_Deserialize()
	{
		return _newtonsoftJson.Deserialize<BenchMessage>(_newtonsoftJsonPayload);
	}

	public sealed class BenchMessage
	{
		public string Id { get; set; }

		public string Name { get; set; }

		public decimal Amount { get; set; }

		public string[] Items { get; set; }
	}
}
