# Euonia 用户手册

本目录是全部用户项目的独立手册入口。每个项目三页：

- **教程**：从安装、接线到可运行验证的端到端场景；
- **API 参考**：关键公共类型/方法、配置项与环境变量速查；
- **完整 API**：由程序集公共元数据生成的全部公开类型/成员签名。

安装、完整选项与版本信息仍以各项目 README 为准。开发与扩展流程见 [开发指南](../DEVELOPMENT.md)。

## 文档层次

| 文件 | 内容 |
|---|---|
| TUTORIAL.md | 端到端教程：前置条件、接线、完整代码、运行验证、常见失败点 |
| API.md | 关键公共 API：常用类型/方法、配置项、环境变量与行为说明 |
| API-FULL.md | 完整 API 签名：由程序集公共元数据生成的全部公开类型与公共成员 |

说明：API-FULL.md 只列公开类型与当前类型的公共声明成员（不含继承成员、内部/私有成员和 XML 摘要）；成员语义、参数约束与配置用法以 API.md 和项目 README 为准。

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
| Euonia.Core | [教程](Euonia.Core/TUTORIAL.md) / [API](Euonia.Core/API.md) / [完整 API](Euonia.Core/API-FULL.md) | [README](../../Source/Euonia.Core/README.md) |
| Euonia.Modularity | [教程](Euonia.Modularity/TUTORIAL.md) / [API](Euonia.Modularity/API.md) / [完整 API](Euonia.Modularity/API-FULL.md) | [README](../../Source/Euonia.Modularity/README.md) |
| Euonia.Hosting | [教程](Euonia.Hosting/TUTORIAL.md) / [API](Euonia.Hosting/API.md) / [完整 API](Euonia.Hosting/API-FULL.md) | [README](../../Source/Euonia.Hosting/README.md) |
| Euonia.Application | [教程](Euonia.Application/TUTORIAL.md) / [API](Euonia.Application/API.md) / [完整 API](Euonia.Application/API-FULL.md) | [README](../../Source/Euonia.Application/README.md) |
| Euonia.Pipeline | [教程](Euonia.Pipeline/TUTORIAL.md) / [API](Euonia.Pipeline/API.md) / [完整 API](Euonia.Pipeline/API-FULL.md) | [README](../../Source/Euonia.Pipeline/README.md) |
| Euonia.Domain | [教程](Euonia.Domain/TUTORIAL.md) / [API](Euonia.Domain/API.md) / [完整 API](Euonia.Domain/API-FULL.md) | [README](../../Source/Euonia.Domain/README.md) |
| Euonia.Linq | [教程](Euonia.Linq/TUTORIAL.md) / [API](Euonia.Linq/API.md) / [完整 API](Euonia.Linq/API-FULL.md) | [README](../../Source/Euonia.Linq/README.md) |
| Euonia.Validation | [教程](Euonia.Validation/TUTORIAL.md) / [API](Euonia.Validation/API.md) / [完整 API](Euonia.Validation/API-FULL.md) | [README](../../Source/Euonia.Validation/README.md) |
| Euonia.Mapping | [教程](Euonia.Mapping/TUTORIAL.md) / [API](Euonia.Mapping/API.md) / [完整 API](Euonia.Mapping/API-FULL.md) | [README](../../Source/Euonia.Mapping/README.md) |
| Euonia.Mapping.Automapper | [教程](Euonia.Mapping.Automapper/TUTORIAL.md) / [API](Euonia.Mapping.Automapper/API.md) / [完整 API](Euonia.Mapping.Automapper/API-FULL.md) | [README](../../Source/Euonia.Mapping.Automapper/README.md) |
| Euonia.Mapping.Mapster | [教程](Euonia.Mapping.Mapster/TUTORIAL.md) / [API](Euonia.Mapping.Mapster/API.md) / [完整 API](Euonia.Mapping.Mapster/API-FULL.md) | [README](../../Source/Euonia.Mapping.Mapster/README.md) |

### 消息总线

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Bus | [教程](Euonia.Bus/TUTORIAL.md) / [API](Euonia.Bus/API.md) / [完整 API](Euonia.Bus/API-FULL.md) | [README](../../Source/Euonia.Bus/README.md) |
| Euonia.Bus.Abstract | [教程](Euonia.Bus.Abstract/TUTORIAL.md) / [API](Euonia.Bus.Abstract/API.md) / [完整 API](Euonia.Bus.Abstract/API-FULL.md) | [README](../../Source/Euonia.Bus.Abstract/README.md) |
| Euonia.Bus.InMemory | [教程](Euonia.Bus.InMemory/TUTORIAL.md) / [API](Euonia.Bus.InMemory/API.md) / [完整 API](Euonia.Bus.InMemory/API-FULL.md) | [README](../../Source/Euonia.Bus.InMemory/README.md) |
| Euonia.Bus.RabbitMq | [教程](Euonia.Bus.RabbitMq/TUTORIAL.md) / [API](Euonia.Bus.RabbitMq/API.md) / [完整 API](Euonia.Bus.RabbitMq/API-FULL.md) | [README](../../Source/Euonia.Bus.RabbitMq/README.md) |
| Euonia.Bus.ActiveMq | [教程](Euonia.Bus.ActiveMq/TUTORIAL.md) / [API](Euonia.Bus.ActiveMq/API.md) / [完整 API](Euonia.Bus.ActiveMq/API-FULL.md) | [README](../../Source/Euonia.Bus.ActiveMq/README.md) |
| Euonia.Bus.Http | [教程](Euonia.Bus.Http/TUTORIAL.md) / [API](Euonia.Bus.Http/API.md) / [完整 API](Euonia.Bus.Http/API-FULL.md) | [README](../../Source/Euonia.Bus.Http/README.md) |
| Euonia.Bus.Grpc | [教程](Euonia.Bus.Grpc/TUTORIAL.md) / [API](Euonia.Bus.Grpc/API.md) / [完整 API](Euonia.Bus.Grpc/API-FULL.md) | [README](../../Source/Euonia.Bus.Grpc/README.md) |
| Euonia.Bus.HealthChecks | [教程](Euonia.Bus.HealthChecks/TUTORIAL.md) / [API](Euonia.Bus.HealthChecks/API.md) / [完整 API](Euonia.Bus.HealthChecks/API-FULL.md) | [README](../../Source/Euonia.Bus.HealthChecks/README.md) |

### 数据、事务与审计

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Repository | [教程](Euonia.Repository/TUTORIAL.md) / [API](Euonia.Repository/API.md) / [完整 API](Euonia.Repository/API-FULL.md) | [README](../../Source/Euonia.Repository/README.md) |
| Euonia.Repository.EfCore | [教程](Euonia.Repository.EfCore/TUTORIAL.md) / [API](Euonia.Repository.EfCore/API.md) / [完整 API](Euonia.Repository.EfCore/API-FULL.md) | [README](../../Source/Euonia.Repository.EfCore/README.md) |
| Euonia.Repository.Mongo | [教程](Euonia.Repository.Mongo/TUTORIAL.md) / [API](Euonia.Repository.Mongo/API.md) / [完整 API](Euonia.Repository.Mongo/API-FULL.md) | [README](../../Source/Euonia.Repository.Mongo/README.md) |
| Euonia.Uow | [教程](Euonia.Uow/TUTORIAL.md) / [API](Euonia.Uow/API.md) / [完整 API](Euonia.Uow/API-FULL.md) | [README](../../Source/Euonia.Uow/README.md) |
| Euonia.Auditing | [教程](Euonia.Auditing/TUTORIAL.md) / [API](Euonia.Auditing/API.md) / [完整 API](Euonia.Auditing/API-FULL.md) | [README](../../Source/Euonia.Auditing/README.md) |
| Euonia.Auditing.EfCore | [教程](Euonia.Auditing.EfCore/TUTORIAL.md) / [API](Euonia.Auditing.EfCore/API.md) / [完整 API](Euonia.Auditing.EfCore/API-FULL.md) | [README](../../Source/Euonia.Auditing.EfCore/README.md) |
| Euonia.Auditing.Mongo | [教程](Euonia.Auditing.Mongo/TUTORIAL.md) / [API](Euonia.Auditing.Mongo/API.md) / [完整 API](Euonia.Auditing.Mongo/API-FULL.md) | [README](../../Source/Euonia.Auditing.Mongo/README.md) |

### 缓存与并发

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Caching | [教程](Euonia.Caching/TUTORIAL.md) / [API](Euonia.Caching/API.md) / [完整 API](Euonia.Caching/API-FULL.md) | [README](../../Source/Euonia.Caching/README.md) |
| Euonia.Caching.Memory | [教程](Euonia.Caching.Memory/TUTORIAL.md) / [API](Euonia.Caching.Memory/API.md) / [完整 API](Euonia.Caching.Memory/API-FULL.md) | [README](../../Source/Euonia.Caching.Memory/README.md) |
| Euonia.Caching.Redis | [教程](Euonia.Caching.Redis/TUTORIAL.md) / [API](Euonia.Caching.Redis/API.md) / [完整 API](Euonia.Caching.Redis/API-FULL.md) | [README](../../Source/Euonia.Caching.Redis/README.md) |
| Euonia.Caching.Runtime | [教程](Euonia.Caching.Runtime/TUTORIAL.md) / [API](Euonia.Caching.Runtime/API.md) / [完整 API](Euonia.Caching.Runtime/API-FULL.md) | [README](../../Source/Euonia.Caching.Runtime/README.md) |
| Euonia.Concurrency | [教程](Euonia.Concurrency/TUTORIAL.md) / [API](Euonia.Concurrency/API.md) / [完整 API](Euonia.Concurrency/API-FULL.md) | [README](../../Source/Euonia.Concurrency/README.md) |
| Euonia.Concurrency.Azure | [教程](Euonia.Concurrency.Azure/TUTORIAL.md) / [API](Euonia.Concurrency.Azure/API.md) / [完整 API](Euonia.Concurrency.Azure/API-FULL.md) | [README](../../Source/Euonia.Concurrency.Azure/README.md) |
| Euonia.Concurrency.FileSystem | [教程](Euonia.Concurrency.FileSystem/TUTORIAL.md) / [API](Euonia.Concurrency.FileSystem/API.md) / [完整 API](Euonia.Concurrency.FileSystem/API-FULL.md) | [README](../../Source/Euonia.Concurrency.FileSystem/README.md) |
| Euonia.Concurrency.Redis | [教程](Euonia.Concurrency.Redis/TUTORIAL.md) / [API](Euonia.Concurrency.Redis/API.md) / [完整 API](Euonia.Concurrency.Redis/API-FULL.md) | [README](../../Source/Euonia.Concurrency.Redis/README.md) |
| Euonia.Concurrency.ZooKeeper | [教程](Euonia.Concurrency.ZooKeeper/TUTORIAL.md) / [API](Euonia.Concurrency.ZooKeeper/API.md) / [完整 API](Euonia.Concurrency.ZooKeeper/API-FULL.md) | [README](../../Source/Euonia.Concurrency.ZooKeeper/README.md) |

### 平台能力

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Tenancy | [教程](Euonia.Tenancy/TUTORIAL.md) / [API](Euonia.Tenancy/API.md) / [完整 API](Euonia.Tenancy/API-FULL.md) | [README](../../Source/Euonia.Tenancy/README.md) |
| Euonia.Security | [教程](Euonia.Security/TUTORIAL.md) / [API](Euonia.Security/API.md) / [完整 API](Euonia.Security/API-FULL.md) | [README](../../Source/Euonia.Security/README.md) |
| Euonia.Telemetry | [教程](Euonia.Telemetry/TUTORIAL.md) / [API](Euonia.Telemetry/API.md) / [完整 API](Euonia.Telemetry/API-FULL.md) | [README](../../Source/Euonia.Telemetry/README.md) |
| Euonia.Telemetry.Otel | [教程](Euonia.Telemetry.Otel/TUTORIAL.md) / [API](Euonia.Telemetry.Otel/API.md) / [完整 API](Euonia.Telemetry.Otel/API-FULL.md) | [README](../../Source/Euonia.Telemetry.Otel/README.md) |
| Euonia.Osba | [教程](Euonia.Osba/TUTORIAL.md) / [API](Euonia.Osba/API.md) / [完整 API](Euonia.Osba/API-FULL.md) | [README](../../Source/Euonia.Osba/README.md) |
| Euonia.Quartz | [教程](Euonia.Quartz/TUTORIAL.md) / [API](Euonia.Quartz/API.md) / [完整 API](Euonia.Quartz/API-FULL.md) | [README](../../Source/Euonia.Quartz/README.md) |
| Euonia.Endpoint | [教程](Euonia.Endpoint/TUTORIAL.md) / [API](Euonia.Endpoint/API.md) / [完整 API](Euonia.Endpoint/API-FULL.md) | [README](../../Source/Euonia.Endpoint/README.md) |
| Euonia.Endpoint.Http | [教程](Euonia.Endpoint.Http/TUTORIAL.md) / [API](Euonia.Endpoint.Http/API.md) / [完整 API](Euonia.Endpoint.Http/API-FULL.md) | [README](../../Source/Euonia.Endpoint.Http/README.md) |
| Euonia.Endpoint.Grpc | [教程](Euonia.Endpoint.Grpc/TUTORIAL.md) / [API](Euonia.Endpoint.Grpc/API.md) / [完整 API](Euonia.Endpoint.Grpc/API-FULL.md) | [README](../../Source/Euonia.Endpoint.Grpc/README.md) |
| Euonia.Endpoint.SourceGenerator | [教程](Euonia.Endpoint.SourceGenerator/TUTORIAL.md) / [API](Euonia.Endpoint.SourceGenerator/API.md) / [完整 API](Euonia.Endpoint.SourceGenerator/API-FULL.md) | [README](../../Source/Euonia.Endpoint.SourceGenerator/README.md) |

### 示例与基准

| 项目 | 手册 | README |
|---|---|---|
| Euonia.Sample.Webapi | [教程](Euonia.Sample.Webapi/TUTORIAL.md) / [API](Euonia.Sample.Webapi/API.md) / [完整 API](Euonia.Sample.Webapi/API-FULL.md) | [README](../../Samples/Euonia.Sample.Webapi/README.md) |
| Euonia.Bus.Benchmarks | [教程](Euonia.Bus.Benchmarks/TUTORIAL.md) / [API](Euonia.Bus.Benchmarks/API.md) / [完整 API](Euonia.Bus.Benchmarks/API-FULL.md) | [README](../../Benchmarks/Euonia.Bus.Benchmarks/README.md) |
| Euonia.Osba.Benchmarks | [教程](Euonia.Osba.Benchmarks/TUTORIAL.md) / [API](Euonia.Osba.Benchmarks/API.md) / [完整 API](Euonia.Osba.Benchmarks/API-FULL.md) | [README](../../Benchmarks/Euonia.Osba.Benchmarks/README.md) |
| Euonia.Platform.Benchmarks | [教程](Euonia.Platform.Benchmarks/TUTORIAL.md) / [API](Euonia.Platform.Benchmarks/API.md) / [完整 API](Euonia.Platform.Benchmarks/API-FULL.md) | [README](../../Benchmarks/Euonia.Platform.Benchmarks/README.md) |

## 约定

- 手册中的代码以当前仓库 API 为准；示例默认使用 InMemory/文件等零外部依赖实现。
- 配置项同时给出原生双下划线（Section__Key）与自定义大写单下划线（SECTION_KEY）两种环境变量写法。
- 需要完整安装、选项默认值与版本信息时，跳转对应 README。
