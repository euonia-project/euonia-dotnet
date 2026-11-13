# Euonia 开发指南

> 面向框架贡献者、维护者，以及需要新增模块、包、测试或基准的开发者。
> 目标：从干净环境开始，能够构建、测试、调试、扩展、打包并提交变更。

## 1. 适用范围

本指南适用于本仓库的 .NET 版本（Euonia for .NET），不适用于独立的 Java 版本。

- 运行时目标：.NET 10 与 .NET 11；测试与示例以 .NET 11 为主。
- Endpoint Source Generator 以 netstandard2.0 analyzer 发布，net10.0 仅用于本地 IDE 体验。
- 包版本由仓库根 project.props 统一管理；依赖版本由 Directory.Packages.props 管理。
- 仓库同时包含 45 个 Source 包、29 个测试/夹具项目、1 个示例 Web API 和 3 个 BenchmarkDotNet 项目。

## 2. 环境与前置条件

### 必需

- .NET SDK 11.0.100 或更高的 11.x（版本要求见 global.json）。
- Git、可用的终端；Windows、Linux、macOS 均可。
- 能访问 NuGet 源；首次 restore 需要下载依赖。

### 可选

- Docker：用于 RabbitMQ、Redis、MongoDB、ZooKeeper 等集成测试或本地验证。
- Azure 存储模拟器/真实账号：仅 Concurrency.Azure 的集成场景需要。
- BenchmarkDotNet 不需要额外服务，但 Release 运行时间较长。

### 快速确认

    dotnet --info
    dotnet --list-sdks

如果机器上有多个 SDK，global.json 会锁定测试运行器；不要随意删除或改写它。

## 3. 仓库结构

| 目录/文件 | 作用 |
|---|---|
| Source/ | 45 个可打包项目，每个项目一个 README |
| Tests/ | 29 个测试/夹具项目，使用 xunit.v3 + Microsoft.Testing.Platform |
| Samples/Euonia.Sample.Webapi/ | 可运行的模块化 Web API 示例 |
| Benchmarks/ | 3 个 BenchmarkDotNet 基准项目 |
| docs/ | 开发指南、用户手册、SQL/迁移说明 |
| Euonia.slnx | 全量解决方案（Source + Tests + Samples + Benchmarks） |
| Euonia.Build.slnx | 仅构建 Source 的解决方案 |
| Euonia.Test.slnx | 测试解决方案 |
| project.props | 版本、作者、公司、Nullable、LangVersion 等全局属性 |
| Directory.Packages.props | 集中包版本管理 |
| Source/common.props | Source 项目公共元数据/打包属性 |
| Tests/common.props | 测试项目公共属性（含并行与测试运行器设置） |

Source 下每个项目通常包含：项目文件、README.md、命名空间目录、可选 DESIGN/SAMPLE/PERMISSION 文档。

## 4. 构建

### 全量构建

    dotnet restore Euonia.slnx
    dotnet build Euonia.slnx -c Debug

验收以 exit code 0 且 0 error 为准。仓库常见 NU1900 警告来自本机 NuGet 漏洞缓存目录权限，与代码无关；不要为了让警告消失而改包版本。

### 只构建 Source

    dotnet build Euonia.Build.slnx -c Debug

适用于只改框架包、不跑测试的快速检查。

### 单项目快速构建

    dotnet build Source/Euonia.Modularity/Euonia.Modularity.csproj -c Debug

修改一个包时先用单项目构建缩短反馈，再跑全量构建。

### 多目标框架

大多数 Source 项目同时目标 net10.0 与 net11.0。若只安装了其中一个 SDK，可先构建单项目；全量验收仍以仓库支持的两个 TFM 为准。

## 5. 测试

### 全量测试

    dotnet test Euonia.Test.slnx -c Debug --no-restore --verbosity minimal

当前基线为 1329 total / 1316 succeeded / 13 skipped / 0 failed。13 个跳过用例来自 RabbitMQ（10）与 Redis（3）集成测试，未启动对应服务时会跳过，不是失败。

### 单项目/单类

    dotnet test Tests/Euonia.Modularity.Tests/Euonia.Modularity.Tests.csproj -c Debug
    dotnet test Tests/Euonia.Bus.Http.Tests/Euonia.Bus.Http.Tests.csproj -c Debug --no-build

测试运行器是 Microsoft.Testing.Platform（见 global.json），过滤参数与 VSTest 不完全相同；需要过滤时先执行：

    dotnet test Tests/Euonia.Modularity.Tests/Euonia.Modularity.Tests.csproj -- --help

再按当前 MTP 版本使用对应参数。不要凭记忆写 VSTest 的 --filter 语法。

### 测试隔离约定

- 每个测试使用独立临时目录；不要写仓库根目录或共享固定路径。
- 环境变量使用测试专用前缀，测试结束恢复；不要依赖开发机已有配置。
- 时间相关逻辑使用可注入的 TimeProvider/时钟；不要依赖真实等待。
- 外部服务测试必须可跳过，并在输出中说明跳过原因。
- 不修改 Tests/common.props 的全局并行设置来规避偶发失败；先定位共享状态。

## 6. 基准

三个基准项目：

| 项目 | 重点 |
|---|---|
| Benchmarks/Euonia.Bus.Benchmarks | 总线发布、管道、序列化 |
| Benchmarks/Euonia.Osba.Benchmarks | OSBA 对象生命周期、规则、工厂 |
| Benchmarks/Euonia.Platform.Benchmarks | 租户键转换、执行上下文等平台路径 |

运行示例：

    dotnet run -c Release --project Benchmarks/Euonia.Bus.Benchmarks -- --filter "*Publish*"
    dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks -- --filter "*"
    dotnet run -c Release --project Benchmarks/Euonia.Platform.Benchmarks -- --filter "*"

结果默认写入 BenchmarkDotNet.Artifacts/results。基准不是单元测试：不要在 CI 里用单次结果做硬性性能断言；需要门禁时使用固定基线、预热和多轮统计。

## 7. 架构、模块与配置

### 分层与依赖方向

    Foundation: Core, Modularity, Pipeline, Linq, Mapping, Validation, Caching, Concurrency
    Platform:   Tenancy, Auditing, Telemetry, Security, Osba
    Domain:     Domain, Application, Repository, Uow
    Messaging:  Bus.Abstract, Bus, transports, HealthChecks
    Hosting:    Hosting, Endpoint, Endpoint.Http, Endpoint.Grpc

依赖只能从上往下；Foundation 不依赖业务包。新增跨层引用前先确认是否可以通过接口/选项解耦。

### 模块生命周期

模块继承 ModuleContextBase（或实现 IModule），通过 DependsOn 声明依赖。生命周期顺序：

1. PreConfigureServices：注册前的最小准备。
2. ConfigureServices：注册服务、选项、模块。
3. PostConfigureServices：依赖其他模块注册结果的处理。
4. OnApplicationInitialization：构建容器后、应用启动前执行。
5. OnApplicationShutdown：应用关闭时释放资源。

模块入口通常在 ConfigureServices 中绑定配置节，例如：

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<MyOptions>(
            Configuration.GetSection("Euonia:My"));
    }

需要启动时执行的注册（如扫描处理器、构建缓存管理器）放在 OnApplicationInitialization，不要在 ConfigureServices 里构建最终对象图。

### 配置优先级

从低到高：

1. 代码默认值。
2. appsettings.json / appsettings.{Environment}.json。
3. 其他配置源。
4. 自定义大写单下划线环境变量（如 EUONIA_BUS_ENABLED）。
5. 原生双下划线环境变量（如 Euonia__Bus__Enabled）。

同名键同时存在时，原生双下划线优先；连接字符串使用 ConnectionStrings__Name 或 CONNECTION_STRING_NAME。

模块内绑定选项时使用 Configuration.GetSection；不要在模块构造函数里读环境变量。

## 8. 新增项目或模块

### Checklist

1. 在 Source/ 下创建项目，导入 ..\common.props。
2. 设置 PackageId、Description、GenerateDocumentationFile；需要打包时确认 IsPackable。
3. 公共类型和成员写 XML 文档注释。
4. 在 Euonia.slnx 与 Euonia.Build.slnx 中加入项目。
5. 新增 Tests/<Project>.Tests 项目，并加入 Euonia.Test.slnx；至少覆盖正常路径、边界和失败路径。
6. 写 Source/<Project>/README.md；按 docs/user-manual 结构补 TUTORIAL.md、API.md、API-FULL.md。
7. 更新 docs/user-manual/README.md 索引。
8. 如涉及性能敏感路径，新增或扩展 Benchmarks 项目。
9. 运行 dotnet build 与 dotnet test。
10. 检查 dotnet pack 产物，确认 README/XML 文档/依赖正确。

### 项目文件要点

    <Project Sdk="Microsoft.NET.Sdk">
        <Import Project="..\common.props" />
        <PropertyGroup>
            <PackageId>Euonia.MyPackage</PackageId>
            <Description>一句话说明包的能力与边界。</Description>
            <GenerateDocumentationFile>true</GenerateDocumentationFile>
        </PropertyGroup>
    </Project>

包版本不要写死在 csproj；由 project.props 统一管理。新增第三方依赖时加到 Directory.Packages.props，不要在各个 csproj 写版本号。

## 9. 编码规范

### C#

- 启用 Nullable；公共 API 不接受/返回未说明的 null。
- 使用文件作用域命名空间；异步方法以 Async 结尾并接收 CancellationToken。
- 不要把 async 方法写成 async void；事件处理器除外。
- 公共 API 必须有 XML 文档；参数、返回值、异常写清楚。
- 异常用于异常情况；业务失败用结果类型/验证结果表达。
- 日志使用结构化参数，不拼接字符串；避免记录密钥、令牌、完整连接串。
- 不修改既有公共 API 签名；必须破坏性变更时走版本与迁移说明。

### 配置与选项

- 选项类使用 get/set 或 init，并提供合理默认值。
- 配置节名定义为常量，避免散落字符串。
- 绑定后校验关键值（连接串、URL、超时）；启动期失败优于运行期空引用。
- 环境变量别名依赖 Euonia.Modularity 的配置构建；独立使用其他包时显式调用 AddEnvironmentVariables。

### 测试

- 测试名描述行为与条件，不写 Test1。
- 一个测试只验证一个行为；集成测试明确前置条件。
- 使用 FluentAssertions/xUnit 断言，避免 Thread.Sleep。
- 新增公共 API 时补回归测试；修复 bug 时先写失败用例。

## 10. 常见问题与排障

| 现象 | 可能原因 | 处理 |
|---|---|---|
| NU1900 大量出现 | 本机 NuGet 漏洞缓存目录无权限 | 忽略或设置可写 NUGET_PACKAGES；不要改包版本 |
| dotnet test 找不到测试 | 用了错误运行器/项目 | 使用 Euonia.Test.slnx，确认 global.json 的 MTP 设置 |
| 测试因 RabbitMQ/Redis 跳过 | 本地未启动服务 | 启动 Docker 服务，或接受 skip；不要改成假通过 |
| 示例端口 5240 被占用 | 上次进程未退出 | 结束进程或 dotnet run --urls http://localhost:5241 |
| 环境变量不生效 | 只设置了大写别名但未走 Modularity 配置 | 使用 Euonia__Section__Key，或确认 AddEnvironmentVariableAliases 已执行 |
| 包版本冲突 | csproj 写死版本 | 统一放 Directory.Packages.props；清理 obj/bin 后 restore |
| 测试偶发失败 | 共享文件/端口/静态状态 | 隔离临时目录、随机端口、测试集合；不要先关并行 |

## 11. 版本、打包与发布

- 版本唯一来源：project.props 的 Version（当前 2026.5.0）。
- 依赖版本唯一来源：Directory.Packages.props。
- 打包：dotnet pack Source/<Project>/<Project>.csproj -c Release -o ./artifacts。
- 检查 nupkg 内容：README、lib/<tfm>、analyzers（SourceGenerator）、依赖项。
- 发布前更新 CHANGELOG.md，说明新增、修复、破坏性变更与迁移步骤。
- 不改已发布包的公共 API 语义；日期版本升级不自动代表破坏性变更。

## 12. 文档维护

每个用户项目有三层文档：

| 文件 | 内容 |
|---|---|
| Source/<Project>/README.md | 安装、完整选项、行为、FAQ、版本兼容 |
| docs/user-manual/<Project>/TUTORIAL.md | 端到端教程与验证步骤 |
| docs/user-manual/<Project>/API.md | 关键 API、配置、环境变量 |
| docs/user-manual/<Project>/API-FULL.md | 全部公开类型与公共成员签名 |

修改公共 API 后必须同步 API.md 与 API-FULL.md；API-FULL.md 由程序集公共元数据生成，不包含继承成员、内部/私有成员和 XML 摘要。

文档索引：docs/user-manual/README.md；开发指南：docs/DEVELOPMENT.md。

## 13. PR Checklist

- dotnet build Euonia.slnx -c Debug 通过，0 error。
- dotnet test Euonia.Test.slnx -c Debug 通过，0 failed；跳过项有说明。
- 新增/修改公共 API 有 XML 文档、测试与用户手册。
- 配置项有默认值、校验和两种环境变量示例。
- 没有提交 PLAN.md、TEST_REPORT.md、REVIEW_FEEDBACK.md 等过程文件。
- 没有提交 bin/obj、TestResults、BenchmarkDotNet.Artifacts、临时数据库。
- git diff 只包含预期文件；提交信息说明动机与影响。

## 14. 相关文档

- [仓库 README](../README.md)
- [用户手册索引](user-manual/README.md)
- [各项目 README](../Source)
- [测试报告](../TEST_REPORT.md)（本地过程文档，不提交）
- [变更记录](../CHANGELOG.md)
- [SQL 与迁移](sql/README.md)

---

最后更新：2026-11-13；如命令或项目结构与仓库不一致，以仓库当前文件和 CI 结果为准。
