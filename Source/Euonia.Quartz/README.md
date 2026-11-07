# Euonia.Quartz

Euonia 的 Quartz 后台作业集成：以 `[BackgroundJob]` 与调度特性 declaratively 定义作业、分组与触发器，`AddQuartz` 统一注册调度器、作业与托管服务。

Quartz background job integration for Euonia: declare jobs, groups and triggers with `[BackgroundJob]` and schedule attributes, and register scheduler, jobs and hosted service through `AddQuartz`.

- **目标框架**：`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Quartz`
- **NuGet 包**：`Euonia.Quartz`

## 安装

```bash
dotnet add package Euonia.Quartz
```

## 主要功能

- **声明式作业**：`[BackgroundJob("name")]` 指定名称、分组、说明与恢复策略。
- **多种触发器**：简单间隔、Cron、日历间隔、每日时间间隔。
- **自动扫描**：`AddJobs(assembly)` 注册程序集中所有 `IJob` 实现。
- **统一配置**：`BackgroundBuildOptions` 配置调度器标识、失火阈值、重复作业处理与启动延迟。
- **托管运行**：自动注册 Quartz 托管服务，并支持 `WaitForJobsToComplete` 等选项。

## 快速开始

定义作业：

```csharp
using Nerosoft.Euonia.Quartz;
using Quartz;

[BackgroundJob("order-cleanup", Group = "maintenance")]
[CronExpressionSchedule("order-cleanup", "0 0 2 * * ?")]
[SimpleSchedule("order-cleanup-heartbeat", milliseconds: 60_000, RepeatCount = -1)]
public sealed class OrderCleanupJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        // 清理逻辑
        return Task.CompletedTask;
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
services.AddQuartz(options =>
{
    options.AddJob<OrderCleanupJob>(job =>
    {
        job.Name = "order-cleanup";
        job.Group = "maintenance";
        job.TriggerConfigure.Add(trigger => trigger.WithCronSchedule("0 0 2 * * ?"));
    });
});
```

## 调度特性

| 特性 | 说明 |
|------|------|
| `[SimpleSchedule(identity, milliseconds)]` | 固定间隔触发；也可指定时/分/秒 |
| `[CronExpressionSchedule(identity, cron)]` | Cron 表达式（`TimeZoneName` 默认 `UTC`） |
| `[CalendarIntervalSchedule(identity, interval, unit)]` | 日历单位间隔，可跨夏令时 |
| `[DailyTimeIntervalSchedule(identity, interval, unit)]` | 每日时间窗口内的间隔触发 |

所有调度特性均支持 `Delay`、`Priority`、`Description` 与 `MisfirePolicy`。

## 核心类型

| 类型 | 作用 |
|------|------|
| `BackgroundJobAttribute` | 作业名称、分组、说明、恢复与持久化选项 |
| `BackgroundJobScheduleAttribute` | 调度特性基类 |
| `BackgroundJobOptions` | 手动注册时的名称、分组与触发器配置 |
| `BackgroundBuildOptions` | 调度器与托管服务配置 |
| `AddQuartz(...)` | 注册 Quartz、作业与托管服务 |
| `AddJob<TJob>` / `AddJobs(assembly)` | 注册单个/程序集内作业 |
| `DefaultJobFactory` | 从 DI 创建作业实例 |

## 相关包

- [`Euonia.Core`](../Euonia.Core/README.md) —— `Singleton`、集合工具
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块化宿主
- [`Euonia.Hosting`](../Euonia.Hosting/README.md) —— ASP.NET Core 宿主

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
