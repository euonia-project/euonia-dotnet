# Euonia 用户手册

本目录是全部用户项目的独立手册入口。每个项目一页，页内分为：

- **教程**：从安装、接线到可运行验证的端到端场景；
- **API 参考**：关键公共类型/方法、配置项与环境变量速查。

安装、完整选项与版本信息仍以各项目 README 为准。

## 学习路径

1. **宿主与模块**：Modularity → Hosting → Application → Pipeline → Validation。
2. **消息总线**：Bus → Bus.InMemory / RabbitMq / ActiveMq → Http / Grpc → HealthChecks。
3. **数据与事务**：Repository → Repository.EfCore / Mongo → Uow → Auditing（+ EF/Mongo 适配）。
4. **缓存与并发**：Caching → Memory / Redis / Runtime → Concurrency → 各类分布式锁。
5. **平台能力**：Tenancy → Security → Telemetry → Telemetry.Otel → Osba / Quartz / Endpoint。
6. **动手实践**：Sample.Webapi → 3 个 Benchmark 项目。

## 项目手册

### 核心、宿主与应用

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Core | [教程](Euonia.Core/TUTORIAL.md) / [API](Euonia.Core/API.md) | [README](../../Source/Euonia.Core/README.md) |
| Euonia.Modularity | [教程](Euonia.Modularity/TUTORIAL.md) / [API](Euonia.Modularity/API.md) | [README](../../Source/Euonia.Modularity/README.md) |
| Euonia.Hosting | [教程](Euonia.Hosting/TUTORIAL.md) / [API](Euonia.Hosting/API.md) | [README](../../Source/Euonia.Hosting/README.md) |
| Euonia.Application | [教程](Euonia.Application/TUTORIAL.md) / [API](Euonia.Application/API.md) | [README](../../Source/Euonia.Application/README.md) |
| Euonia.Pipeline | [教程](Euonia.Pipeline/TUTORIAL.md) / [API](Euonia.Pipeline/API.md) | [README](../../Source/Euonia.Pipeline/README.md) |
| Euonia.Domain | [教程](Euonia.Domain/TUTORIAL.md) / [API](Euonia.Domain/API.md) | [README](../../Source/Euonia.Domain/README.md) |
| Euonia.Linq | [教程](Euonia.Linq/TUTORIAL.md) / [API](Euonia.Linq/API.md) | [README](../../Source/Euonia.Linq/README.md) |
| Euonia.Validation | [教程](Euonia.Validation/TUTORIAL.md) / [API](Euonia.Validation/API.md) | [README](../../Source/Euonia.Validation/README.md) |
| Euonia.Mapping | [教程](Euonia.Mapping/TUTORIAL.md) / [API](Euonia.Mapping/API.md) | [README](../../Source/Euonia.Mapping/README.md) |
| Euonia.Mapping.Automapper | [教程](Euonia.Mapping.Automapper/TUTORIAL.md) / [API](Euonia.Mapping.Automapper/API.md) | [README](../../Source/Euonia.Mapping.Automapper/README.md) |
| Euonia.Mapping.Mapster | [教程](Euonia.Mapping.Mapster/TUTORIAL.md) / [API](Euonia.Mapping.Mapster/API.md) | [README](../../Source/Euonia.Mapping.Mapster/README.md) |

### 消息总线

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Bus | [教程](Euonia.Bus/TUTORIAL.md) / [API](Euonia.Bus/API.md) | [README](../../Source/Euonia.Bus/README.md) |
| Euonia.Bus.Abstract | [教程](Euonia.Bus.Abstract/TUTORIAL.md) / [API](Euonia.Bus.Abstract/API.md) | [README](../../Source/Euonia.Bus.Abstract/README.md) |
| Euonia.Bus.InMemory | [教程](Euonia.Bus.InMemory/TUTORIAL.md) / [API](Euonia.Bus.InMemory/API.md) | [README](../../Source/Euonia.Bus.InMemory/README.md) |
| Euonia.Bus.RabbitMq | [教程](Euonia.Bus.RabbitMq/TUTORIAL.md) / [API](Euonia.Bus.RabbitMq/API.md) | [README](../../Source/Euonia.Bus.RabbitMq/README.md) |
| Euonia.Bus.ActiveMq | [教程](Euonia.Bus.ActiveMq/TUTORIAL.md) / [API](Euonia.Bus.ActiveMq/API.md) | [README](../../Source/Euonia.Bus.ActiveMq/README.md) |
| Euonia.Bus.Http | [教程](Euonia.Bus.Http/TUTORIAL.md) / [API](Euonia.Bus.Http/API.md) | [README](../../Source/Euonia.Bus.Http/README.md) |
| Euonia.Bus.Grpc | [教程](Euonia.Bus.Grpc/TUTORIAL.md) / [API](Euonia.Bus.Grpc/API.md) | [README](../../Source/Euonia.Bus.Grpc/README.md) |
| Euonia.Bus.HealthChecks | [教程](Euonia.Bus.HealthChecks/TUTORIAL.md) / [API](Euonia.Bus.HealthChecks/API.md) | [README](../../Source/Euonia.Bus.HealthChecks/README.md) |

### 数据、事务与审计

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Repository | [教程](Euonia.Repository/TUTORIAL.md) / [API](Euonia.Repository/API.md) | [README](../../Source/Euonia.Repository/README.md) |
| Euonia.Repository.EfCore | [教程](Euonia.Repository.EfCore/TUTORIAL.md) / [API](Euonia.Repository.EfCore/API.md) | [README](../../Source/Euonia.Repository.EfCore/README.md) |
| Euonia.Repository.Mongo | [教程](Euonia.Repository.Mongo/TUTORIAL.md) / [API](Euonia.Repository.Mongo/API.md) | [README](../../Source/Euonia.Repository.Mongo/README.md) |
| Euonia.Uow | [教程](Euonia.Uow/TUTORIAL.md) / [API](Euonia.Uow/API.md) | [README](../../Source/Euonia.Uow/README.md) |
| Euonia.Auditing | [教程](Euonia.Auditing/TUTORIAL.md) / [API](Euonia.Auditing/API.md) | [README](../../Source/Euonia.Auditing/README.md) |
| Euonia.Auditing.EfCore | [教程](Euonia.Auditing.EfCore/TUTORIAL.md) / [API](Euonia.Auditing.EfCore/API.md) | [README](../../Source/Euonia.Auditing.EfCore/README.md) |
| Euonia.Auditing.Mongo | [教程](Euonia.Auditing.Mongo/TUTORIAL.md) / [API](Euonia.Auditing.Mongo/API.md) | [README](../../Source/Euonia.Auditing.Mongo/README.md) |

### 缓存与并发

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Caching | [教程](Euonia.Caching/TUTORIAL.md) / [API](Euonia.Caching/API.md) | [README](../../Source/Euonia.Caching/README.md) |
| Euonia.Caching.Memory | [教程](Euonia.Caching.Memory/TUTORIAL.md) / [API](Euonia.Caching.Memory/API.md) | [README](../../Source/Euonia.Caching.Memory/README.md) |
| Euonia.Caching.Redis | [教程](Euonia.Caching.Redis/TUTORIAL.md) / [API](Euonia.Caching.Redis/API.md) | [README](../../Source/Euonia.Caching.Redis/README.md) |
| Euonia.Caching.Runtime | [教程](Euonia.Caching.Runtime/TUTORIAL.md) / [API](Euonia.Caching.Runtime/API.md) | [README](../../Source/Euonia.Caching.Runtime/README.md) |
| Euonia.Concurrency | [教程](Euonia.Concurrency/TUTORIAL.md) / [API](Euonia.Concurrency/API.md) | [README](../../Source/Euonia.Concurrency/README.md) |
| Euonia.Concurrency.Azure | [教程](Euonia.Concurrency.Azure/TUTORIAL.md) / [API](Euonia.Concurrency.Azure/API.md) | [README](../../Source/Euonia.Concurrency.Azure/README.md) |
| Euonia.Concurrency.FileSystem | [教程](Euonia.Concurrency.FileSystem/TUTORIAL.md) / [API](Euonia.Concurrency.FileSystem/API.md) | [README](../../Source/Euonia.Concurrency.FileSystem/README.md) |
| Euonia.Concurrency.Redis | [教程](Euonia.Concurrency.Redis/TUTORIAL.md) / [API](Euonia.Concurrency.Redis/API.md) | [README](../../Source/Euonia.Concurrency.Redis/README.md) |
| Euonia.Concurrency.ZooKeeper | [教程](Euonia.Concurrency.ZooKeeper/TUTORIAL.md) / [API](Euonia.Concurrency.ZooKeeper/API.md) | [README](../../Source/Euonia.Concurrency.ZooKeeper/README.md) |

### 平台能力

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Tenancy | [教程](Euonia.Tenancy/TUTORIAL.md) / [API](Euonia.Tenancy/API.md) | [README](../../Source/Euonia.Tenancy/README.md) |
| Euonia.Security | [教程](Euonia.Security/TUTORIAL.md) / [API](Euonia.Security/API.md) | [README](../../Source/Euonia.Security/README.md) |
| Euonia.Telemetry | [教程](Euonia.Telemetry/TUTORIAL.md) / [API](Euonia.Telemetry/API.md) | [README](../../Source/Euonia.Telemetry/README.md) |
| Euonia.Telemetry.Otel | [教程](Euonia.Telemetry.Otel/TUTORIAL.md) / [API](Euonia.Telemetry.Otel/API.md) | [README](../../Source/Euonia.Telemetry.Otel/README.md) |
| Euonia.Osba | [教程](Euonia.Osba/TUTORIAL.md) / [API](Euonia.Osba/API.md) | [README](../../Source/Euonia.Osba/README.md) |
| Euonia.Quartz | [教程](Euonia.Quartz/TUTORIAL.md) / [API](Euonia.Quartz/API.md) | [README](../../Source/Euonia.Quartz/README.md) |
| Euonia.Endpoint | [教程](Euonia.Endpoint/TUTORIAL.md) / [API](Euonia.Endpoint/API.md) | [README](../../Source/Euonia.Endpoint/README.md) |
| Euonia.Endpoint.Http | [教程](Euonia.Endpoint.Http/TUTORIAL.md) / [API](Euonia.Endpoint.Http/API.md) | [README](../../Source/Euonia.Endpoint.Http/README.md) |
| Euonia.Endpoint.Grpc | [教程](Euonia.Endpoint.Grpc/TUTORIAL.md) / [API](Euonia.Endpoint.Grpc/API.md) | [README](../../Source/Euonia.Endpoint.Grpc/README.md) |
| Euonia.Endpoint.SourceGenerator | [教程](Euonia.Endpoint.SourceGenerator/TUTORIAL.md) / [API](Euonia.Endpoint.SourceGenerator/API.md) | [README](../../Source/Euonia.Endpoint.SourceGenerator/README.md) |

### 示例与基准

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Sample.Webapi | [教程](Euonia.Sample.Webapi/TUTORIAL.md) / [API](Euonia.Sample.Webapi/API.md) | [README](../../Samples/Euonia.Sample.Webapi/README.md) |
| Euonia.Bus.Benchmarks | [教程](Euonia.Bus.Benchmarks/TUTORIAL.md) / [API](Euonia.Bus.Benchmarks/API.md) | [README](../../Benchmarks/Euonia.Bus.Benchmarks/README.md) |
| Euonia.Osba.Benchmarks | [教程](Euonia.Osba.Benchmarks/TUTORIAL.md) / [API](Euonia.Osba.Benchmarks/API.md) | [README](../../Benchmarks/Euonia.Osba.Benchmarks/README.md) |
| Euonia.Platform.Benchmarks | [教程](Euonia.Platform.Benchmarks/TUTORIAL.md) / [API](Euonia.Platform.Benchmarks/API.md) | [README](../../Benchmarks/Euonia.Platform.Benchmarks/README.md) |

## 约定

- 手册中的代码以当前仓库 API 为准；示例默认使用 InMemory/文件等零外部依赖实现。
- 配置项同时给出原生双下划线（Section__Key）与自定义大写单下划线（SECTION_KEY）两种环境变量写法。
- 需要完整安装、选项默认值与版本信息时，跳转对应 README。
