# Euonia.Quartz API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

`Euonia.Quartz` 把 Quartz.NET 作业调度接入 Euonia：用特性声明作业与触发器，`AddQuartz` 注册调度器、DI 作业工厂与托管服务。

- **包名**：`Euonia.Quartz`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Quartz`（`AddQuartz` 位于 `Microsoft.Extensions.DependencyInjection`）
- **完整选项/边界**：[Source/Euonia.Quartz/README.md](../../../Source/Euonia.Quartz/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `[BackgroundJob(name)]` | 声明作业名称、分组、描述、恢复/持久化选项 | `Group` 默认 `default` |
| `[SimpleSchedule(identity, milliseconds)]` | 固定间隔触发；省略 `RepeatCount` 即永久重复 | 另有 `(identity, hour, minute, second)` 重载；负数非法 |
| `[CronExpressionSchedule(identity, cron)]` | Cron 表达式触发 | `TimeZoneName` 默认 `UTC` |
| `[CalendarIntervalSchedule(identity, interval, unit)]` | 日历单位间隔，支持跨夏令时 | `IntervalUnit` 枚举 |
| `[DailyTimeIntervalSchedule(identity, interval, unit)]` | 每日时间窗口内的间隔触发 | `RepeatCount` 仅大于 0 时应用 |
| `BackgroundJobScheduleAttribute` | 调度特性基类 | `Delay`、`Priority`、`Description`、`MisfirePolicy` |
| `BackgroundBuildOptions.SchedulerName` | 调度器实例名 | `null` 使用 Quartz 默认 |
| `BackgroundBuildOptions.MisfireThreshold` | 失火阈值 | `TimeSpan?`，写入 `quartz.jobStore.misfireThreshold` |
| `BackgroundBuildOptions.WaitForJobsToComplete` / `AwaitApplicationStarted` / `StartDelay` | 关闭等待、启动后执行、启动延迟 | 默认 `false` / `true` / `null` |
| `BackgroundBuildOptions.Configure(Action<IQuartzBuilder>)` | 追加 Quartz 构建器配置 | 按添加顺序执行 |
| `BackgroundBuildOptions.AddJob<TJob>(Action<BackgroundJobOptions>)` | 手动注册作业与触发器 | `BackgroundJobOptions.Name/Group/TriggerConfigure` |
| `AddQuartz(Action<BackgroundBuildOptions>?)` | 注册 Quartz、作业、DI 工厂与托管服务 | 最常用入口 |
| `AddQuartz(Action<QuartzOptions>, Action<IQuartzBuilder>)` | 直接配置 Quartz 与构建器 | 不应用 `BackgroundBuildOptions` 上的作业集合 |
| `AddJobs(Assembly)` | 扫描程序集中所有 `IJob` 导出类型 | 每个作业必须有触发器 |
| `AddJob<TJob>(BackgroundJobOptions?)` | 注册单个作业 | 无触发器时抛异常 |
| `DefaultJobFactory` | 从 DI 创建作业，并按 `tenantId` 安装租户作用域 | 需要 `ITenantStore` |
| `UseInMemoryStore()` / `UsePersistentStore(...)` | 选择作业存储 | 测试常用内存存储 |
| `UseDefaultThreadPool(int maxConcurrency, ...)` | 配置默认线程池并发 | Quartz 4 无零大小/专用线程池 |
| `IJob.Execute(IJobExecutionContext, CancellationToken)` | Quartz 4 作业入口 | 返回 `ValueTask` |

### 配置说明

`Euonia.Quartz` 没有框架配置节；`BackgroundBuildOptions`、`BackgroundJobOptions` 全部通过 `AddQuartz` 回调代码配置，因此本页不提供 appsettings 示例。需要多租户时，租户本身由 `Euonia.Tenancy` 的 `Euonia:MultiTenancy` 配置，作业侧只负责读取 `tenantId`。

## 相关文档

- [教程](TUTORIAL.md)
- [Source README：完整调度特性、选项与 Quartz 4 边界](../../../Source/Euonia.Quartz/README.md)
- [Euonia.Modularity 用户手册](../Euonia.Modularity/API.md) —— 把调度宿主组织成模块
- [Euonia.Hosting 用户手册](../Euonia.Hosting/API.md) —— Web 宿主中的后台作业
- [Euonia.Application 用户手册](../Euonia.Application/API.md) —— 作业中复用应用服务
- [仓库根 README](../../../README.md)
