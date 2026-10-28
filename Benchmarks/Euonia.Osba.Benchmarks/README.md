# Euonia.Osba.Benchmarks

`Euonia.Osba` 的对象生命周期、规则与保存路径基线。

```bash
# 全部
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks

# 只跑某一组
dotnet run -c Release --project Benchmarks/Euonia.Osba.Benchmarks -- --filter '*ObjectLifecycle*'
```

建议在 CI 中把 `ObjectLifecycleBenchmarks` 的 `WriteExisting`、`RuledObject.WriteWithPropertyRule` 与 `SavePlain` 作为门禁：
回归超过 20% 或分配翻倍即失败。具体预算见
[`Source/Euonia.Osba/ARCHITECTURE-REVIEW.md`](../../Source/Euonia.Osba/ARCHITECTURE-REVIEW.md) §4.3。
