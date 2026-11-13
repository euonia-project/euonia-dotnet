# Euonia.Osba.Benchmarks 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Osba.Benchmarks |
| 公共类型 | 7 |
| 命名空间 | 1 |

## 命名空间 Euonia.Osba.Benchmarks

### Euonia.Osba.Benchmarks.DomainBenchObject

**类型**：class
基类 EditableObject<Euonia.Osba.Benchmarks.DomainBenchObject>；接口 Nerosoft.Euonia.Osba.IBusinessObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IHasRuleCheck、System.IDisposable、Nerosoft.Euonia.Osba.IOperableProperty、Nerosoft.Euonia.Osba.IEditableObject、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.ISavable、ISavable<Euonia.Osba.Benchmarks.DomainBenchObject>

- new Euonia.Osba.Benchmarks.DomainBenchObject()
- System.String Name { get; set; }
- static readonly PropertyInfo<System.String> NameProperty

### Euonia.Osba.Benchmarks.DomainControlBenchmarks

**类型**：class

- new Euonia.Osba.Benchmarks.DomainControlBenchmarks()
- System.Object GetOriginalValue()
- void MarkAsChangedThenAccept()
- void RejectChanges()
- void Setup()
- void Write()

### Euonia.Osba.Benchmarks.ObjectLifecycleBenchmarks

**类型**：class

- new Euonia.Osba.Benchmarks.ObjectLifecycleBenchmarks()
- Euonia.Osba.Benchmarks.SmallObject Create()
- System.String CreateAndFirstRead()
- System.String ReadExisting()
- void Setup()
- void WriteExisting()
- void WriteSameValue()

### Euonia.Osba.Benchmarks.PlainObject

**类型**：class
基类 EditableObject<Euonia.Osba.Benchmarks.PlainObject>；接口 Nerosoft.Euonia.Osba.IBusinessObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IHasRuleCheck、System.IDisposable、Nerosoft.Euonia.Osba.IOperableProperty、Nerosoft.Euonia.Osba.IEditableObject、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.ISavable、ISavable<Euonia.Osba.Benchmarks.PlainObject>

- new Euonia.Osba.Benchmarks.PlainObject()
- System.String Name { get; set; }
- static readonly PropertyInfo<System.String> NameProperty

### Euonia.Osba.Benchmarks.RuleAndFactoryBenchmarks

**类型**：class

- new Euonia.Osba.Benchmarks.RuleAndFactoryBenchmarks()
- void Cleanup()
- Task<Euonia.Osba.Benchmarks.PlainObject> FetchActuator()
- Task<Euonia.Osba.Benchmarks.PlainObject> FetchFactory()
- System.Threading.Tasks.Task SavePlain()
- void Setup()
- Task<System.Boolean> ValidatePlain()
- void WritePlain()
- void WriteWithPropertyRule()

### Euonia.Osba.Benchmarks.RuledObject

**类型**：class
基类 EditableObject<Euonia.Osba.Benchmarks.RuledObject>；接口 Nerosoft.Euonia.Osba.IBusinessObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IHasRuleCheck、System.IDisposable、Nerosoft.Euonia.Osba.IOperableProperty、Nerosoft.Euonia.Osba.IEditableObject、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.ISavable、ISavable<Euonia.Osba.Benchmarks.RuledObject>

- new Euonia.Osba.Benchmarks.RuledObject()
- System.String Name { get; set; }
- static readonly PropertyInfo<System.String> NameProperty

### Euonia.Osba.Benchmarks.SmallObject

**类型**：class
基类 EditableObject<Euonia.Osba.Benchmarks.SmallObject>；接口 Nerosoft.Euonia.Osba.IBusinessObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IHasRuleCheck、System.IDisposable、Nerosoft.Euonia.Osba.IOperableProperty、Nerosoft.Euonia.Osba.IEditableObject、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.ISavable、ISavable<Euonia.Osba.Benchmarks.SmallObject>

- new Euonia.Osba.Benchmarks.SmallObject()
- System.String Name { get; set; }
- static readonly PropertyInfo<System.String> NameProperty
