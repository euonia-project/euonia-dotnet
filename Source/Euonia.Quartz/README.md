# Euonia.Quartz

Euonia 的 Quartz.NET 作业调度集成：用 `[BackgroundJob]` 与调度特性声明作业、分组和触发器，`AddQuartz` 统一注册调度器、作业、DI 作业工厂与托管服务。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Quartz`（`AddQuartz` 扩展位于 `Microsoft.Extensions.DependencyInjection`）
- **NuGet 包**：`Euonia.Quartz`

## 安装

```bash
dotnet add package Euonia.Quartz
```

## 快速开始

定义作业：

```csharp
using Nerosoft.Euonia.Quartz;
using Quartz;

[BackgroundJob("order-cleanup", Group = "maintenance")]
[CronExpressionSchedule("order-cleanup", "0 0 2 * * ?")]
[SimpleSchedule("order-cleanup-heartbeat", milliseconds: 60_000)]
public sealed class OrderCleanupJob : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        // 清理逻辑
        return ValueTask.CompletedTask;
    }
}
```

注册 Quartz 并扫描作业：

```csharp
using Nerosoft.Euonia.Quartz;

services.AddQuartz(options =>
{
    options.SchedulerName = "euonia-scheduler";
    options.MisfireThreshold = TimeSpan.FromSeconds(30);
    options.WaitForJobsToComplete = true;
    options.StartDelay = TimeSpan.FromSeconds(5);

    options.Configure(quartz => quartz.AddJobs(typeof(Program).Assembly));
});
```

或使用构建器重载注册单个作业：

```csharp
services.AddQuartz(_ => { }, quartz => quartz.AddJob<OrderCleanupJob>());
```

手动指定作业选项：

```csharp
using Quartz;

services.AddQuartz(options =>
{
    options.AddJob<OrderCleanupJob>(job =>
    {
        job.Name = "order-cleanup";
        job.Group = "maintenance";
        job.TriggerConfigure.Add(trigger =>
            trigger.WithCronSchedule("0 0 2 * * ?", schedule => schedule.InTimeZone(TimeZoneInfo.Utc)));
    });
});
```

## 调度特性

| 特性 | 说明 |
|------|------|
| `[SimpleSchedule(identity, milliseconds)]` | 固定间隔触发；也可用 `(identity, hour, minute, second)` 指定间隔 |
| `[CronExpressionSchedule(identity, cron)]` | Cron 表达式；`TimeZoneName` 默认 `UTC`，可设为 `LOCAL` 或系统时区名 |
| `[CalendarIntervalSchedule(identity, interval, unit)]` | 日历单位间隔，可跨夏令时 |
| `[DailyTimeIntervalSchedule(identity, interval, unit)]` | 每日时间窗口内的间隔触发 |

所有调度特性均支持 `Delay`、`Priority`、`Description` 与 `MisfirePolicy`；`SimpleSchedule` 额外支持 `RepeatCount`（省略即永久重复）。

## 核心类型

| 类型 | 作用 |
|------|------|
| `BackgroundJobAttribute` | 作业名称、分组、说明、恢复与持久化选项 |
| `BackgroundJobScheduleAttribute` | 调度特性基类，提供 `Identity`、`Delay`、`Priority`、`MisfirePolicy` |
| `BackgroundJobOptions` | 手动注册时的名称、分组、作业配置与触发器配置 |
| `BackgroundBuildOptions` | 调度器、作业集合与托管服务配置 |
| `AddQuartz(...)` | 注册 Quartz、作业、DI 作业工厂与托管服务 |
| `AddJob<TJob>` / `AddJobs(assembly)` | 注册单个/程序集内作业 |
| `DefaultJobFactory` | 从 DI 创建作业实例，并按作业数据中的 `tenantId` 安装租户作用域 |

## 选项（代码配置）

Quartz 集成没有框架自带的配置节；所有选项都通过 `AddQuartz` 的回调配置。

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `BackgroundBuildOptions.SchedulerId` | `string?` | `null` | `AUTO` / `SYS_PROP` 表示启动时生成实例 ID |
| `BackgroundBuildOptions.SchedulerName` | `string?` | `null` | 调度器实例名称 |
| `BackgroundBuildOptions.MisfireThreshold` | `TimeSpan?` | `null` | 写入 `quartz.jobStore.misfireThreshold`（毫秒） |
| `BackgroundBuildOptions.IgnoreDuplicates` | `bool` | `true` | 忽略重复调度数据；`OverWriteExistingData` 为 `true` 时不生效 |
| `BackgroundBuildOptions.OverWriteExistingData` | `bool` | `true` | 覆盖同标识的现有调度数据 |
| `BackgroundBuildOptions.WaitForJobsToComplete` | `bool` | `false` | 关闭时等待正在执行的作业完成 |
| `BackgroundBuildOptions.AwaitApplicationStarted` | `bool` | `true` | 应用启动完成后才启动作业 |
| `BackgroundBuildOptions.StartDelay` | `TimeSpan?` | `null` | 调度器启动延迟 |
| `BackgroundBuildOptions.MaxBatchSize` | `int?` | `null` | 单次批处理获取的最大触发器数 |
| `BackgroundBuildOptions.TypeLoader` | `Type?` | `null` | 仅保存类型，不会自动注册；需调用 `UseTypeLoader<T>()` |
| `BackgroundJobOptions.Name` / `Group` | `string?` | 特性值或类型名 / `default` | 作业键为 `{Name}.job` |
| `BackgroundJobOptions.TriggerConfigure` | `List<Action<ITriggerConfigurator<IJob>>>` | 空 | 每个元素追加一个触发器配置 |

## 注意事项 / 限制

- `AddQuartz(Action<BackgroundBuildOptions>)` 使用 `Singleton<BackgroundBuildOptions>` 中的共享实例；重复调用会累积配置与作业，同一作业类型后一次注册会覆盖前一次（包括其选项）。
- `AddJobs(assembly)` 只扫描程序集的导出类型中实现 `IJob` 的类型；通过它注册的作业必须至少有一个 `BackgroundJobScheduleAttribute`，否则 `AddJob` 抛出 `InvalidOperationException`；单独用 `AddJob(type, options)` 时可由 `TriggerConfigure` 提供触发器。
- `SimpleScheduleAttribute.RepeatCount` 必须为 `null`（永久重复）或非负数；传负数会在构建触发器时抛出 `InvalidOperationException`。
- `CronExpressionScheduleAttribute.TimeZoneName` 默认 `UTC`，`LOCAL` 表示本地时区；其他值必须是系统时区 ID，否则在配置触发器时抛出时区异常。
- `AddQuartz(Action<QuartzOptions>, Action<IQuartzBuilder>)` 直接使用第二个回调配置构建器，**不会**应用共享 `BackgroundBuildOptions` 上的作业、扁平属性与构建器配置集合；托管服务选项仍取自该共享实例。
- Quartz.NET 4 不再支持零大小线程池和专用线程池；`UseZeroSizeThreadPool`、`UseDedicatedThreadPool` 会直接抛出 `NotSupportedException`，请改用 `UseDefaultThreadPool` 或 `UseThreadPool<T>`。
- 作业数据中的 `tenantId` 需要容器中存在 `ITenantStore`；租户不存在时抛出 `TenantNotFoundException`。租户作用域在作业执行体内安装，而不是在作业工厂阶段。

## 常见问题与故障排查

### `AddJobs(assembly)` 启动时抛 `InvalidOperationException`
**原因**：`AddJobs` 会为程序集中每个导出 `IJob` 类型调用无选项的 `AddJob(type)`，因此完全依赖类型上的 `BackgroundJobScheduleAttribute`；若没有特性，触发器集合为空，`AddJob` 抛出异常。
**解决**：给作业加至少一个调度特性（如 `[CronExpressionSchedule]`、`[SimpleSchedule]`），或改用 `options.AddJob<TJob>(job => job.TriggerConfigure.Add(...))` 显式配置触发器。

### `SimpleSchedule` 配置 `RepeatCount = -1` 时抛异常
**原因**：`SimpleScheduleAttribute.Configure()` 只接受 `null`（永久重复）或非负的 `RepeatCount`；负数会在构建触发器时抛出 `InvalidOperationException`。
**解决**：要永久重复就省略 `RepeatCount`；要有限次数就填 `0` 或正整数（总触发次数为值加 1）。

### 作业被注册两次或重复执行
**原因**：`AddQuartz` 使用共享的 `Singleton<BackgroundBuildOptions>`；每次调用都会把共享字典中的作业再次注册到调度器，同一类型后一次 `AddJob` 会覆盖字典项，但之前的托管服务/触发器可能已经注册。多个模块各自调用 `AddQuartz` 时尤其容易重复。
**解决**：整个宿主只保留一处 `AddQuartz`；作业集中在一个模块里通过 `options.Configure(quartz => quartz.AddJobs(...))` 注册，避免同时在多个模块调用 `AddJob<TJob>`。

### 作业带 `tenantId` 时抛 `TenantNotFoundException`
**原因**：`DefaultJobFactory` 在作业数据中读到 `tenantId` 后会通过 `ITenantStore` 查询租户；未注册租户存储或查不到对应租户都会抛出该异常。
**解决**：调用 `AddTenancy(...)` 并注册 `ITenantStore`（如 `AddInMemoryTenantStore`）；确认 `JobDataMap` 的键名是 `tenantId`，值来自真实租户 ID。

## 最佳实践

- 集中注册：一个宿主只调用一次 `AddQuartz`，用 `options.Configure(quartz => quartz.AddJobs(typeof(Program).Assembly))` 统一扫描，避免分散注册导致顺序与重复问题。
- 启动与关闭：生产环境保留 `AwaitApplicationStarted = true`，需要优雅停机时设置 `WaitForJobsToComplete = true`，并用 `StartDelay` 错开启动高峰。
- 测试替身：作业测试使用 `UseInMemoryStore()`，通过 `ISchedulerFactory.GetScheduler()` 手动 `ScheduleJob`，不要依赖真实持久化存储。
- 时区显式化：Cron 作业显式设置 `TimeZoneName`（默认 `UTC`）；跨时区部署时避免使用 `LOCAL`，并在测试中覆盖夏令时边界。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`；包版本由仓库根 `project.props` 统一管理（当前 `2026.5.0`）。
- 关键依赖：Quartz.NET 4.7.3（由 `Directory.Packages.props` 锁定）；`BackgroundBuildOptions` 与 `BackgroundJobOptions` 都是代码配置。
- 已知边界：Quartz 4 的 `IJob.Execute` 返回 `ValueTask` 并接收 `CancellationToken`；不再支持零大小/专用线程池；租户作用域在作业执行体内安装。
- 升级提示：从 Quartz 3 升级时先改 `IJob` 签名，再移除 `UseZeroSizeThreadPool`/`UseDedicatedThreadPool` 调用，并复核 `MisfireThreshold` 与持久化存储配置。

## 相关包

- [`Euonia.Core`](../Euonia.Core/README.md) —— `Singleton`、集合工具
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块化宿主与依赖注入
- [`Euonia.Tenancy`](../Euonia.Tenancy/README.md) —— 作业租户作用域
- [`Euonia.Hosting`](../Euonia.Hosting/README.md) —— ASP.NET Core 宿主

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
