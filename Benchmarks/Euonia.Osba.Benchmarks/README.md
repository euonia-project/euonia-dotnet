# Euonia.Osba.Benchmarks

`Euonia.Osba` 的对象生命周期、规则、保存、领域控制与读取执行器基线。

```bash
# 全部
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks

# 只跑某一组
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks -- --filter '*ObjectLifecycle*'
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks -- --filter '*RuleAndFactory*'
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks -- --filter '*DomainControl*'

# 快速冒烟（ShortRun，不作为正式门禁）
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks -- --filter '*DomainControl*' --job short
```

建议在 CI 中把以下指标作为门禁：

- `ObjectLifecycleBenchmarks.WriteExisting`
- `RuleAndFactoryBenchmarks.WriteWithPropertyRule`
- `RuleAndFactoryBenchmarks.SavePlain`
- `RuleAndFactoryBenchmarks.FetchFactory`
- `RuleAndFactoryBenchmarks.FetchActuator`
- `DomainControlBenchmarks.RejectChanges`

回归超过 20% 或分配翻倍即失败。具体预算见
[`Source/Euonia.Osba/ARCHITECTURE-REVIEW.md`](../../Source/Euonia.Osba/ARCHITECTURE-REVIEW.md) §4.3。
