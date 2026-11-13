# Euonia.Bus.Benchmarks 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Bus.Benchmarks |
| 公共类型 | 3 |
| 命名空间 | 1 |

## 命名空间 Euonia.Bus.Benchmarks

### Euonia.Bus.Benchmarks.SerializationBenchmarks.BenchMessage

**类型**：class

- new Euonia.Bus.Benchmarks.SerializationBenchmarks.BenchMessage()
- System.Decimal Amount { get; set; }
- System.String Id { get; set; }
- System.String[] Items { get; set; }
- System.String Name { get; set; }

### Euonia.Bus.Benchmarks.BusDispatchBenchmarks

**类型**：class

- new Euonia.Bus.Benchmarks.BusDispatchBenchmarks()
- Task<System.Int32> CallAsync()
- void Cleanup()
- System.Object CreateEnvelope()
- System.Threading.Tasks.Task DirectTransportPublish()
- System.Threading.Tasks.Task ParallelOne()
- System.Threading.Tasks.Task PublishAsync()
- System.Object ResolvePipeline()
- System.Threading.Tasks.Task SendAsync()
- void Setup()

### Euonia.Bus.Benchmarks.SerializationBenchmarks

**类型**：class

- new Euonia.Bus.Benchmarks.SerializationBenchmarks()
- Euonia.Bus.Benchmarks.SerializationBenchmarks.BenchMessage NewtonsoftJson_Deserialize()
- System.String NewtonsoftJson_Serialize()
- void Setup()
- Euonia.Bus.Benchmarks.SerializationBenchmarks.BenchMessage SystemTextJson_Deserialize()
- System.String SystemTextJson_Serialize()
