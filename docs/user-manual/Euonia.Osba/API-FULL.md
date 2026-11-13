# Euonia.Osba 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Osba |
| 公共类型 | 104 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddBusinessObject(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Reflection.Assembly[] assemblies)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddBusinessObject(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Osba.OsbaOptions> configure, System.Reflection.Assembly[] assemblies)

## 命名空间 Nerosoft.Euonia.Osba

### ActuatorBase<TTarget>

**类型**：class

- ActuatorBase<TTarget> BypassRule<TRule>()
- ActuatorBase<TTarget> BypassRule(System.String ruleName)
- ActuatorBase<TTarget> BypassRuleChecks()
- Task<TTarget> ExecuteAsync(System.Threading.CancellationToken cancellationToken = default)
- ActuatorBase<TTarget> Handle(Func<TTarget, System.Threading.Tasks.Task> action)
- ActuatorBase<TTarget> Handle(Action<TTarget> action)
- ActuatorBase<TTarget> WithRule(Nerosoft.Euonia.Osba.IRuleBase rule)
- ActuatorBase<TTarget> WithRule<TRule>()
- ActuatorBase<TTarget> WithRule<TRule>(System.IServiceProvider provider)
- ActuatorBase<TTarget> WithRuleChecksOnDelete(System.Boolean check = true)
- ActuatorBase<TTarget> WithRules(IEnumerable<Nerosoft.Euonia.Osba.IRuleBase> rules)

### ActuatorBuilder<TTarget>

**类型**：class

- new ActuatorBuilder<TTarget>(Nerosoft.Euonia.Osba.IObjectFactory factory, IPipeline<TTarget, TTarget> pipeline, System.Boolean isDefaultPipeline = false)
- ActuatorBuilder<TTarget> Behavior(Action<IPipeline<TTarget, TTarget>> behavior)

### Nerosoft.Euonia.Osba.ActuatorBuilderExtensions

**类型**：static class

- static CreateActuator<TTarget> Create<TTarget>(ActuatorBuilder<TTarget> builder, System.Object[] criteria)
- static DeleteActuator<TTarget> Delete<TTarget>(ActuatorBuilder<TTarget> builder, System.Object[] criteria)
- static ExecuteActuator<TTarget> Execute<TTarget>(ActuatorBuilder<TTarget> builder, System.Object[] criteria)
- static FetchActuator<TTarget> Fetch<TTarget>(ActuatorBuilder<TTarget> builder, System.Object[] criteria)
- static UpdateActuator<TTarget> Update<TTarget>(ActuatorBuilder<TTarget> builder, System.Object[] criteria)

### Nerosoft.Euonia.Osba.ActuatorExtensions

**类型**：static class

- static System.Threading.Tasks.Task NextAsync<TTarget, TResult>(Task<TTarget> source, Func<TTarget, TResult> selector, Action<TResult> action)
- static System.Threading.Tasks.Task NextAsync<TTarget>(Task<TTarget> source, Action<TTarget> action)
- static Task<TResult> ReturnAsync<TTarget, TResult>(Task<TTarget> source, Func<TTarget, TResult> selector)

### Nerosoft.Euonia.Osba.BrokenRule

**类型**：class

- new Nerosoft.Euonia.Osba.BrokenRule()
- System.String Description { get; set; }
- System.String Property { get; set; }
- Nerosoft.Euonia.Osba.RuleSeverity Severity { get; set; }

### Nerosoft.Euonia.Osba.BrokenRuleCollection

**类型**：class
基类 ObservableCollection<Nerosoft.Euonia.Osba.BrokenRule>；接口 IList<Nerosoft.Euonia.Osba.BrokenRule>、ICollection<Nerosoft.Euonia.Osba.BrokenRule>、IEnumerable<Nerosoft.Euonia.Osba.BrokenRule>、System.Collections.IEnumerable、System.Collections.IList、System.Collections.ICollection、IReadOnlyList<Nerosoft.Euonia.Osba.BrokenRule>、IReadOnlyCollection<Nerosoft.Euonia.Osba.BrokenRule>、System.Collections.Specialized.INotifyCollectionChanged、System.ComponentModel.INotifyPropertyChanged

- new Nerosoft.Euonia.Osba.BrokenRuleCollection()
- System.Int32 ErrorCount { get; set; }
- System.Int32 InformationCount { get; set; }
- System.Int32 WarningCount { get; set; }

### Nerosoft.Euonia.Osba.BusinessContext

**类型**：class

- new Nerosoft.Euonia.Osba.BusinessContext(Nerosoft.Euonia.Osba.BusinessContextAccessor contextAccessor)
- System.IServiceProvider CurrentServiceProvider { get; }
- Nerosoft.Euonia.Osba.IObjectFactory ObjectFactory { get; }
- System.Security.Claims.ClaimsPrincipal Principal { get; }
### Nerosoft.Euonia.Osba.BusinessContextAccessor

**类型**：class

- new Nerosoft.Euonia.Osba.BusinessContextAccessor(System.IServiceProvider provider)
- static System.IServiceProvider Current { get; }
- static void Clear()
- static void SetCurrent(System.IServiceProvider provider)

### BusinessObject<T>

**类型**：class
基类 ObjectBase<T>；接口 Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、Nerosoft.Euonia.Osba.IHasRuleCheck、Nerosoft.Euonia.Osba.IObjectLifecycle、System.IDisposable、Nerosoft.Euonia.Osba.IBusinessObject、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging

- IReadOnlyList<Nerosoft.Euonia.Osba.IPropertyInfo> ChangedProperties { get; }
- System.Boolean HasChangedProperties { get; }
- System.Boolean IsDirty { get; }
- void AcceptChanges()
- IReadOnlyDictionary<Nerosoft.Euonia.Osba.IPropertyInfo, System.Object> GetChangedValues()
- System.Object GetOriginalValue(Nerosoft.Euonia.Osba.IPropertyInfo property)
- TValue GetOriginalValue<TValue>(PropertyInfo<TValue> property)
- IReadOnlyDictionary<Nerosoft.Euonia.Osba.IPropertyInfo, System.Object> GetOriginalValues()
- System.Object GetProperty(Nerosoft.Euonia.Osba.IPropertyInfo propertyInfo)
- void RefreshChangedProperties()
- void RejectChanges()
- void SetProperty(Nerosoft.Euonia.Osba.IPropertyInfo propertyInfo, System.Object newValue)

### Nerosoft.Euonia.Osba.BusinessObjectFactory

**类型**：class
接口 Nerosoft.Euonia.Osba.IObjectFactory

- new Nerosoft.Euonia.Osba.BusinessObjectFactory(System.IServiceProvider provider)
- new Nerosoft.Euonia.Osba.BusinessObjectFactory(System.IServiceProvider provider, Nerosoft.Euonia.Osba.IObjectActivator activator)
- TTarget Create<TTarget>(System.Object[] criteria)
- Task<TTarget> CreateAsync<TTarget>(System.Object[] criteria)
- System.Threading.Tasks.Task DeleteAsync<TTarget>(System.Object[] criteria)
- Task<TTarget> ExecuteAsync<TTarget>(TTarget target, System.Threading.CancellationToken cancellationToken = default)
- Task<TTarget> ExecuteAsync<TTarget>(System.Object[] criteria)
- TTarget Fetch<TTarget>(System.Object[] criteria)
- Task<TTarget> FetchAsync<TTarget>(System.Object[] criteria)
- Task<TTarget> InsertAsync<TTarget>(System.Object[] criteria)
- Task<TTarget> SaveAsync<TTarget>(TTarget target, System.Threading.CancellationToken cancellationToken = default)
- Task<TTarget> UpdateAsync<TTarget>(System.Object[] criteria)

### Nerosoft.Euonia.Osba.BusyChangedEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Osba.BusyChangedEventArgs(System.String propertyName, System.Boolean isBusy)
- System.Boolean IsBusy { get; set; }
- System.String PropertyName { get; set; }

### Nerosoft.Euonia.Osba.BusyChangedEventHandler

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Osba.BusyChangedEventHandler(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(System.Object sender, Nerosoft.Euonia.Osba.BusyChangedEventArgs e, System.AsyncCallback callback, System.Object object)
- void EndInvoke(System.IAsyncResult result)
- void Invoke(System.Object sender, Nerosoft.Euonia.Osba.BusyChangedEventArgs e)

### Nerosoft.Euonia.Osba.CommonRule

**类型**：class

- new Nerosoft.Euonia.Osba.CommonRule()

### Nerosoft.Euonia.Osba.CommonRule.CommonRuleBase

**类型**：class
基类 Nerosoft.Euonia.Osba.PropertyRuleBase；接口 Nerosoft.Euonia.Osba.IRuleBase、Nerosoft.Euonia.Osba.IPropertyRule

_无公共成员。_

### CreateActuator<TTarget>

**类型**：class
基类 ActuatorBase<TTarget>

- new CreateActuator<TTarget>(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)

### Nerosoft.Euonia.Osba.DataAnnotationRule

**类型**：class
基类 Nerosoft.Euonia.Osba.PropertyRuleBase；接口 Nerosoft.Euonia.Osba.IRuleBase、Nerosoft.Euonia.Osba.IPropertyRule

- new Nerosoft.Euonia.Osba.DataAnnotationRule(Nerosoft.Euonia.Osba.IPropertyInfo property, System.ComponentModel.DataAnnotations.ValidationAttribute attribute)
- System.ComponentModel.DataAnnotations.ValidationAttribute Attribute { get; }
- System.Threading.Tasks.Task ExecuteAsync(Nerosoft.Euonia.Osba.IRuleContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Osba.DefaultObjectActivator

**类型**：class
接口 Nerosoft.Euonia.Osba.IObjectActivator

- new Nerosoft.Euonia.Osba.DefaultObjectActivator()
- void FinalizeInstance(System.Object obj)
- void InitializeInstance(System.Object obj)

### DeleteActuator<TTarget>

**类型**：class
基类 EditableActuator<TTarget>

- new DeleteActuator<TTarget>(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)

### Nerosoft.Euonia.Osba.DictionaryChangedAction

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Add |
| Remove |
| Update |
| Clear |


### DictionaryChangedEventArgs<TKey, TValue>

**类型**：class
基类 System.EventArgs

- new DictionaryChangedEventArgs<TKey, TValue>(TKey key, Nerosoft.Euonia.Osba.DictionaryChangedAction action, TValue oldValue, TValue newValue)
- Nerosoft.Euonia.Osba.DictionaryChangedAction Action { get; }
- TKey Key { get; }
- TValue NewValue { get; }
- TValue OldValue { get; }

### DictionaryChangedEventHandler<TKey, TValue>

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new DictionaryChangedEventHandler<TKey, TValue>(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(System.Object sender, DictionaryChangedEventArgs<TKey, TValue> args, System.AsyncCallback callback, System.Object object)
- void EndInvoke(System.IAsyncResult result)
- void Invoke(System.Object sender, DictionaryChangedEventArgs<TKey, TValue> args)

### EditableActuator<TTarget>

**类型**：class
基类 ActuatorBase<TTarget>

_无公共成员。_

### EditableObject<T>

**类型**：class
基类 ObservableObject<T>；接口 Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、Nerosoft.Euonia.Osba.IHasRuleCheck、Nerosoft.Euonia.Osba.IObjectLifecycle、System.IDisposable、Nerosoft.Euonia.Osba.IBusinessObject、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IOperableProperty、Nerosoft.Euonia.Osba.IEditableObject、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.ISavable、ISavable<T>

- event EventHandler<Nerosoft.Euonia.Osba.SavedEventArgs> Saved
- Task<T> SaveAsync(System.Boolean forceUpdate = false, System.Threading.CancellationToken cancellationToken = default)

### ExecuteActuator<TTarget>

**类型**：class
基类 ActuatorBase<TTarget>

- new ExecuteActuator<TTarget>(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)

### Nerosoft.Euonia.Osba.ExecuteOnStateAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Osba.ExecuteOnStateAttribute(Nerosoft.Euonia.Osba.ObjectEditState[] states)
- IEnumerable<Nerosoft.Euonia.Osba.ObjectEditState> States { get; }

### Nerosoft.Euonia.Osba.FactoryCreateAttribute

**类型**：class
基类 Nerosoft.Euonia.Osba.FactoryMethodAttribute

- new Nerosoft.Euonia.Osba.FactoryCreateAttribute()

### Nerosoft.Euonia.Osba.FactoryDeleteAttribute

**类型**：class
基类 Nerosoft.Euonia.Osba.FactoryMethodAttribute

- new Nerosoft.Euonia.Osba.FactoryDeleteAttribute()

### Nerosoft.Euonia.Osba.FactoryExecuteAttribute

**类型**：class
基类 Nerosoft.Euonia.Osba.FactoryMethodAttribute

- new Nerosoft.Euonia.Osba.FactoryExecuteAttribute()

### Nerosoft.Euonia.Osba.FactoryFetchAttribute

**类型**：class
基类 Nerosoft.Euonia.Osba.FactoryMethodAttribute

- new Nerosoft.Euonia.Osba.FactoryFetchAttribute()

### Nerosoft.Euonia.Osba.FactoryInsertAttribute

**类型**：class
基类 Nerosoft.Euonia.Osba.FactoryMethodAttribute

- new Nerosoft.Euonia.Osba.FactoryInsertAttribute()

### Nerosoft.Euonia.Osba.FactoryMethodAttribute

**类型**：class
基类 System.Attribute

_无公共成员。_

### Nerosoft.Euonia.Osba.FactoryUpdateAttribute

**类型**：class
基类 Nerosoft.Euonia.Osba.FactoryMethodAttribute

- new Nerosoft.Euonia.Osba.FactoryUpdateAttribute()

### FetchActuator<TTarget>

**类型**：class
基类 ActuatorBase<TTarget>

- new FetchActuator<TTarget>(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)

### FieldData<T>

**类型**：class
接口 IFieldData<T>、Nerosoft.Euonia.Osba.IFieldData、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy

- new FieldData<T>()
- new FieldData<T>(System.String name)
- System.Int32 HistoryDepth { get; set; }
- System.Boolean IsBusy { get; }
- System.Boolean IsChanged { get; }
- System.Boolean IsDeleted { get; }
- System.Boolean IsNew { get; }
- System.Boolean IsSavable { get; }
- System.Boolean IsValid { get; }
- System.String Name { get; }
- IObservable<T> ObservableValue { get; }
- T OriginalValue { get; }
- T Value { get; set; }
- void MarkAsUnchanged()
- void SetValue(T value, System.Boolean trackHistory)
- void Undo()

### Nerosoft.Euonia.Osba.FieldDataManager

**类型**：class

- new Nerosoft.Euonia.Osba.FieldDataManager()
- new Nerosoft.Euonia.Osba.FieldDataManager(System.Type businessObjectType)
- System.Boolean FieldExists(Nerosoft.Euonia.Osba.IPropertyInfo property)
- static void ForceStaticFieldInit(System.Type type)
- System.Object GetOriginalValue(Nerosoft.Euonia.Osba.IPropertyInfo property)
- TValue GetOriginalValue<TValue>(PropertyInfo<TValue> property)
- List<Nerosoft.Euonia.Osba.IPropertyInfo> GetRegisteredProperties()
- Nerosoft.Euonia.Osba.IPropertyInfo GetRegisteredProperty(System.String propertyName)

### Nerosoft.Euonia.Osba.IActuator

**类型**：interface

- ActuatorBuilder<TTarget> For<TTarget>()

### IActuatorBehavior<TTarget>

**类型**：interface
接口 IPipelineBehavior<TTarget, TTarget>

_无公共成员。_

### Nerosoft.Euonia.Osba.IBusinessObject

**类型**：interface
接口 Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging

_无公共成员。_

### Nerosoft.Euonia.Osba.IChildGraphNode

**类型**：interface

- void AddParent(Nerosoft.Euonia.Osba.ObjectBase parent, Nerosoft.Euonia.Osba.IPropertyInfo property)
- void RemoveParent(Nerosoft.Euonia.Osba.ObjectBase parent, Nerosoft.Euonia.Osba.IPropertyInfo property)

### Nerosoft.Euonia.Osba.IEditableObject

**类型**：interface
接口 Nerosoft.Euonia.Osba.IBusinessObject、Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy

- System.Boolean CheckObjectRulesOnDelete { get; }
- Nerosoft.Euonia.Osba.ObjectEditState State { get; }
- void MarkAsChanged()
- void MarkAsDeleted(System.Boolean checkObjectRules = false)
- void MarkAsNew()

### Nerosoft.Euonia.Osba.IFieldData

**类型**：interface
接口 Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy

- System.String Name { get; }
- System.Object OriginalValue { get; }
- System.Object Value { get; set; }
- void MarkAsUnchanged()
- void Undo()

### IFieldData<T>

**类型**：interface
接口 Nerosoft.Euonia.Osba.IFieldData、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy

- T OriginalValue { get; }
- T Value { get; set; }

### Nerosoft.Euonia.Osba.IHasRuleCheck

**类型**：interface

- System.Boolean IsValid { get; }
- void AllRulesComplete()
- Nerosoft.Euonia.Osba.BrokenRuleCollection GetBrokenRules()
- void ResumeRuleChecking()
- void RuleCheckComplete(Nerosoft.Euonia.Osba.IPropertyInfo property)
- void RuleCheckComplete(System.String property)
- void SuspendRuleChecking()

### Nerosoft.Euonia.Osba.IMemberInfo

**类型**：interface

- System.String Name { get; }

### Nerosoft.Euonia.Osba.INotifyBusy

**类型**：interface

- System.Boolean IsBusy { get; }
- System.Boolean IsSelfBusy { get; }
- event Nerosoft.Euonia.Osba.BusyChangedEventHandler BusyChanged

### Nerosoft.Euonia.Osba.IObjectActivator

**类型**：interface

- void FinalizeInstance(System.Object obj)
- void InitializeInstance(System.Object obj)

### Nerosoft.Euonia.Osba.IObjectFactory

**类型**：interface

- TTarget Create<TTarget>(System.Object[] criteria)
- Task<TTarget> CreateAsync<TTarget>(System.Object[] criteria)
- System.Threading.Tasks.Task DeleteAsync<TTarget>(System.Object[] criteria)
- Task<TTarget> ExecuteAsync<TTarget>(TTarget target, System.Threading.CancellationToken cancellationToken = default)
- Task<TTarget> ExecuteAsync<TTarget>(System.Object[] criteria)
- TTarget Fetch<TTarget>(System.Object[] criteria)
- Task<TTarget> FetchAsync<TTarget>(System.Object[] criteria)
- Task<TTarget> InsertAsync<TTarget>(System.Object[] criteria)
- Task<TTarget> SaveAsync<TTarget>(TTarget target, System.Threading.CancellationToken cancellationToken = default)
- Task<TTarget> UpdateAsync<TTarget>(System.Object[] criteria)

### Nerosoft.Euonia.Osba.IObjectLifecycle

**类型**：interface

- System.Threading.Tasks.Task OnExecutedAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task OnExecutingAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task OnSavedAsync(System.Object result, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task OnSavingAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Osba.IObjectRule

**类型**：interface
接口 Nerosoft.Euonia.Osba.IRuleBase

_无公共成员。_

### Nerosoft.Euonia.Osba.IOperableProperty

**类型**：interface

- System.Object GetProperty(Nerosoft.Euonia.Osba.IPropertyInfo propertyInfo)
- void SetProperty(Nerosoft.Euonia.Osba.IPropertyInfo propertyInfo, System.Object newValue)

### Nerosoft.Euonia.Osba.IOsbaObject

**类型**：interface
接口 Nerosoft.Euonia.Osba.IUseBusinessContext

- Nerosoft.Euonia.Osba.FieldDataManager FieldManager { get; }
- System.Boolean FieldExists(Nerosoft.Euonia.Osba.IPropertyInfo property)
- void LoadProperty(Nerosoft.Euonia.Osba.IPropertyInfo propertyInfo, System.Object newValue)
- void LoadProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue newValue)
- System.Object ReadProperty(Nerosoft.Euonia.Osba.IPropertyInfo propertyInfo)
- System.Object ReadProperty(System.String propertyName)
- TValue ReadProperty<TValue>(PropertyInfo<TValue> propertyInfo)
- TValue ReadProperty<TValue>(System.String propertyName)

### Nerosoft.Euonia.Osba.IPropertyInfo

**类型**：interface
接口 Nerosoft.Euonia.Osba.IMemberInfo、System.IComparable

- System.Object DefaultValue { get; }
- System.String FriendlyName { get; }
- System.Boolean IsChild { get; }
- System.Type Type { get; }
- System.Reflection.PropertyInfo GetPropertyInfo()
- Nerosoft.Euonia.Osba.IFieldData NewFieldData(System.String name)

### Nerosoft.Euonia.Osba.IPropertyRule

**类型**：interface
接口 Nerosoft.Euonia.Osba.IRuleBase

- Nerosoft.Euonia.Osba.IPropertyInfo Property { get; }

### Nerosoft.Euonia.Osba.IReadOnlyObject

**类型**：interface
接口 Nerosoft.Euonia.Osba.IBusinessObject、Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging

_无公共成员。_

### Nerosoft.Euonia.Osba.IRuleBase

**类型**：interface

- System.String Name { get; }
- System.Int32 Priority { get; }
- List<Nerosoft.Euonia.Osba.IPropertyInfo> RelatedProperties { get; }
- System.Threading.Tasks.Task ExecuteAsync(Nerosoft.Euonia.Osba.IRuleContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Osba.IRuleContext

**类型**：interface

- IReadOnlyList<Nerosoft.Euonia.Osba.RuleResult> Results { get; }
- Nerosoft.Euonia.Osba.IRuleBase Rule { get; }
- System.IServiceProvider ServiceProvider { get; }
- System.Object Target { get; }
- void AddErrorResult(System.String description)
- void AddInformationResult(System.String description)
- void AddSuccessResult()
- void AddWarningResult(System.String description)
- void Complete()
- T GetService<T>()

### Nerosoft.Euonia.Osba.IRuleRegistry

**类型**：interface

- void CleanRules(System.Type type)
- Nerosoft.Euonia.Osba.RuleManager GetRules(System.Type type)

### Nerosoft.Euonia.Osba.IRules

**类型**：interface

- System.Object Target { get; }

### Nerosoft.Euonia.Osba.ISavable

**类型**：interface

- event EventHandler<Nerosoft.Euonia.Osba.SavedEventArgs> Saved
- Task<System.Object> SaveAsync(System.Boolean forceUpdate = false, System.Threading.CancellationToken cancellationToken = default)
- void SaveComplete(System.Object newObject)

### ISavable<T>

**类型**：interface

- event EventHandler<Nerosoft.Euonia.Osba.SavedEventArgs> Saved
- Task<T> SaveAsync(System.Boolean forceUpdate = false, System.Threading.CancellationToken cancellationToken = default)
- void SaveComplete(T newObject)

### Nerosoft.Euonia.Osba.IServiceObject

**类型**：interface
接口 Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、Nerosoft.Euonia.Osba.IHasRuleCheck

_无公共成员。_

### Nerosoft.Euonia.Osba.ITrackableObject

**类型**：interface
接口 Nerosoft.Euonia.Osba.INotifyBusy

- System.Boolean IsChanged { get; }
- System.Boolean IsDeleted { get; }
- System.Boolean IsNew { get; }
- System.Boolean IsSavable { get; }
- System.Boolean IsValid { get; }

### Nerosoft.Euonia.Osba.IUseBusinessContext

**类型**：interface

- Nerosoft.Euonia.Osba.BusinessContext BusinessContext { get; set; }

### Nerosoft.Euonia.Osba.CommonRule.Lambda

**类型**：class
基类 Nerosoft.Euonia.Osba.CommonRule.CommonRuleBase；接口 Nerosoft.Euonia.Osba.IRuleBase、Nerosoft.Euonia.Osba.IPropertyRule

- new Nerosoft.Euonia.Osba.CommonRule.Lambda(Nerosoft.Euonia.Osba.IPropertyInfo property, Func<System.Object, Nerosoft.Euonia.Osba.IRuleContext, Task<System.Boolean>> handler, System.String message)
- new Nerosoft.Euonia.Osba.CommonRule.Lambda(Nerosoft.Euonia.Osba.IPropertyInfo property, Func<System.Object, Nerosoft.Euonia.Osba.IRuleContext, Task<System.Boolean>> handler, Func<System.String> messageFactory)
- System.Threading.Tasks.Task ExecuteAsync(Nerosoft.Euonia.Osba.IRuleContext context, System.Threading.CancellationToken cancellationToken = default)

### Lambda<T>

**类型**：class
基类 Nerosoft.Euonia.Osba.CommonRule.CommonRuleBase；接口 Nerosoft.Euonia.Osba.IRuleBase、Nerosoft.Euonia.Osba.IPropertyRule

- new Lambda<T>(PropertyInfo<T> property, Func<T, Nerosoft.Euonia.Osba.IRuleContext, System.Boolean> handler, System.String message)
- new Lambda<T>(PropertyInfo<T> property, Func<T, Nerosoft.Euonia.Osba.IRuleContext, System.Boolean> handler, Func<System.String> messageFactory)
- System.Threading.Tasks.Task ExecuteAsync(Nerosoft.Euonia.Osba.IRuleContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Osba.ObjectBase

**类型**：class
接口 Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、Nerosoft.Euonia.Osba.IHasRuleCheck、Nerosoft.Euonia.Osba.IObjectLifecycle、System.IDisposable

- Nerosoft.Euonia.Osba.BusinessContext BusinessContext { get; set; }
- Nerosoft.Euonia.Osba.FieldDataManager FieldManager { get; }
- System.Boolean IsValid { get; }
- Nerosoft.Euonia.Osba.ValidationState ValidationState { get; }
- event System.ComponentModel.PropertyChangedEventHandler PropertyChanged
- event System.ComponentModel.PropertyChangingEventHandler PropertyChanging
- event System.EventHandler ValidationComplete
- void AcceptChanges()
- void AllRulesComplete()
- System.Boolean CanPerformOperation(System.String operation)
- System.Boolean CanReadProperty(Nerosoft.Euonia.Osba.IPropertyInfo property)
- System.Boolean CanReadProperty(Nerosoft.Euonia.Osba.IPropertyInfo property, System.Boolean throwOnFalse)
- System.Boolean CanReadProperty(System.String propertyName)
- System.Boolean CanWriteProperty(Nerosoft.Euonia.Osba.IPropertyInfo property)
- System.Boolean CanWriteProperty(Nerosoft.Euonia.Osba.IPropertyInfo property, System.Boolean throwOnFalse)
- System.Boolean CanWriteProperty(System.String propertyName)
- void Dispose()
- System.Threading.Tasks.Task EnsureValidAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Boolean FieldExists(Nerosoft.Euonia.Osba.IPropertyInfo property)
- Nerosoft.Euonia.Osba.BrokenRuleCollection GetBrokenRules()
- void LoadProperty<TValue>(PropertyInfo<TValue> propertyInfo, TValue newValue)
- void LoadProperty(Nerosoft.Euonia.Osba.IPropertyInfo propertyInfo, System.Object newValue)
- TValue ReadProperty<TValue>(PropertyInfo<TValue> propertyInfo)
- System.Object ReadProperty(Nerosoft.Euonia.Osba.IPropertyInfo propertyInfo)
- System.Object ReadProperty(System.String propertyName)
- TValue ReadProperty<TValue>(System.String propertyName)
- void ResumeRuleChecking()
- void RuleCheckComplete(Nerosoft.Euonia.Osba.IPropertyInfo property)
- void RuleCheckComplete(System.String property)
- void SuspendRuleChecking()
- Task<System.Boolean> ValidateAsync(System.Boolean cascade = true, System.Threading.CancellationToken cancellationToken = default)

### ObjectBase<T>

**类型**：class
基类 Nerosoft.Euonia.Osba.ObjectBase；接口 Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、Nerosoft.Euonia.Osba.IHasRuleCheck、Nerosoft.Euonia.Osba.IObjectLifecycle、System.IDisposable

_无公共成员。_

### Nerosoft.Euonia.Osba.ObjectChangedEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Osba.ObjectChangedEventArgs(System.Object changedObject, System.ComponentModel.PropertyChangedEventArgs propertyChangedArgs)
- new Nerosoft.Euonia.Osba.ObjectChangedEventArgs(System.Object changedObject, System.ComponentModel.PropertyChangedEventArgs propertyChangedArgs, System.Collections.Specialized.NotifyCollectionChangedEventArgs collectionChangedArgs)
- new Nerosoft.Euonia.Osba.ObjectChangedEventArgs(System.Object changedObject, System.ComponentModel.PropertyChangedEventArgs propertyChangedArgs, System.ComponentModel.ListChangedEventArgs listChangedArgs)
- System.Object ChangedObject { get; }
- System.Collections.Specialized.NotifyCollectionChangedEventArgs CollectionChangedArgs { get; }
- System.ComponentModel.ListChangedEventArgs ListChangedArgs { get; }
- System.ComponentModel.PropertyChangedEventArgs PropertyChangedArgs { get; }

### Nerosoft.Euonia.Osba.ObjectEditState

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| None |
| New |
| Changed |
| Deleted |


### Nerosoft.Euonia.Osba.ObjectOperationResolver

**类型**：class
### Nerosoft.Euonia.Osba.ObjectPermissionCodeSource

**类型**：class
### Nerosoft.Euonia.Osba.ObjectReflector

**类型**：class

- new Nerosoft.Euonia.Osba.ObjectReflector()
- static System.Reflection.MethodInfo FindMatchedMethod<TTarget>(System.Type attributeType, IReadOnlyList<System.Type> parameterTypes)

### Nerosoft.Euonia.Osba.ObjectRuleBase

**类型**：class
基类 Nerosoft.Euonia.Osba.RuleBase；接口 Nerosoft.Euonia.Osba.IRuleBase、Nerosoft.Euonia.Osba.IObjectRule

_无公共成员。_

### Nerosoft.Euonia.Osba.ObjectStateChangedEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Osba.ObjectStateChangedEventArgs(Nerosoft.Euonia.Osba.ObjectEditState oldState, Nerosoft.Euonia.Osba.ObjectEditState newState)
- Nerosoft.Euonia.Osba.ObjectEditState NewState { get; }
- Nerosoft.Euonia.Osba.ObjectEditState OldState { get; }

### ObservableDictionary<TKey, TValue>

**类型**：class
基类 Dictionary<TKey, TValue>；接口 IDictionary<TKey, TValue>、ICollection<KeyValuePair<TKey, TValue>>、IEnumerable<KeyValuePair<TKey, TValue>>、System.Collections.IEnumerable、System.Collections.IDictionary、System.Collections.ICollection、IReadOnlyDictionary<TKey, TValue>、IReadOnlyCollection<KeyValuePair<TKey, TValue>>、System.Runtime.Serialization.ISerializable、System.Runtime.Serialization.IDeserializationCallback、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.IChildGraphNode

- new ObservableDictionary<TKey, TValue>()
- System.Boolean IsBusy { get; }
- System.Boolean IsSelfBusy { get; }
- TValue this[TKey key] { get; set; }
- System.Boolean RaiseItemChangedEvents { get; set; }
- System.IDisposable SuppressItemChangedEvents { get; }
- event Nerosoft.Euonia.Osba.BusyChangedEventHandler BusyChanged
- event DictionaryChangedEventHandler<TKey, TValue> ItemChanged
- void Add(TKey key, TValue value)
- void AddParent(Nerosoft.Euonia.Osba.ObjectBase parent, Nerosoft.Euonia.Osba.IPropertyInfo property)
- void Clear()
- TValue GetOrAdd(TKey key, TValue value)
- TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
- System.Boolean Remove(TKey key)
- System.Boolean Remove(TKey key, out TValue value)
- void RemoveParent(Nerosoft.Euonia.Osba.ObjectBase parent, Nerosoft.Euonia.Osba.IPropertyInfo property)
- System.Boolean TryAdd(TKey key, TValue value)

### ObservableList<TItem>

**类型**：class
基类 ObservableCollection<TItem>；接口 IList<TItem>、ICollection<TItem>、IEnumerable<TItem>、System.Collections.IEnumerable、System.Collections.IList、System.Collections.ICollection、IReadOnlyList<TItem>、IReadOnlyCollection<TItem>、System.Collections.Specialized.INotifyCollectionChanged、System.ComponentModel.INotifyPropertyChanged、Nerosoft.Euonia.Osba.INotifyBusy、Nerosoft.Euonia.Osba.IChildGraphNode

- new ObservableList<TItem>()
- new ObservableList<TItem>(IEnumerable<TItem> collection)
- System.Boolean IsBusy { get; }
- System.Boolean IsSelfBusy { get; }
- System.Boolean RaiseListChangedEvents { get; set; }
- System.IDisposable SuppressListChangedEvents { get; }
- event Nerosoft.Euonia.Osba.BusyChangedEventHandler BusyChanged
- event EventHandler<Nerosoft.Euonia.Osba.ObjectChangedEventArgs> ChildChanged
- void AddParent(Nerosoft.Euonia.Osba.ObjectBase parent, Nerosoft.Euonia.Osba.IPropertyInfo property)
- void AddRange(IEnumerable<TItem> items)
- void RemoveParent(Nerosoft.Euonia.Osba.ObjectBase parent, Nerosoft.Euonia.Osba.IPropertyInfo property)

### ObservableObject<T>

**类型**：class
基类 BusinessObject<T>；接口 Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、Nerosoft.Euonia.Osba.IHasRuleCheck、Nerosoft.Euonia.Osba.IObjectLifecycle、System.IDisposable、Nerosoft.Euonia.Osba.IBusinessObject、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IOperableProperty、Nerosoft.Euonia.Osba.IEditableObject、Nerosoft.Euonia.Osba.ITrackableObject、Nerosoft.Euonia.Osba.INotifyBusy

- System.Boolean CheckObjectRulesOnDelete { get; set; }
- System.Boolean IsBusy { get; }
- System.Boolean IsChanged { get; }
- System.Boolean IsDeleted { get; }
- System.Boolean IsNew { get; }
- System.Boolean IsSavable { get; }
- System.Boolean IsSelfBusy { get; }
- Nerosoft.Euonia.Osba.ObjectEditState State { get; set; }
- event Nerosoft.Euonia.Osba.BusyChangedEventHandler BusyChanged
- event EventHandler<Nerosoft.Euonia.Osba.ObjectStateChangedEventArgs> StateChanged
- void AcceptChanges()
- void MarkAsChanged()
- void MarkAsClean()
- void MarkAsDeleted(System.Boolean checkObjectRules = false)
- void MarkAsNew()

### Nerosoft.Euonia.Osba.OsbaConfigurationException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Osba.OsbaConfigurationException(IReadOnlyList<Nerosoft.Euonia.Osba.OsbaDiagnostic> diagnostics)
- IReadOnlyList<Nerosoft.Euonia.Osba.OsbaDiagnostic> Diagnostics { get; }

### Nerosoft.Euonia.Osba.OsbaDiagnostic

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Osba.OsbaDiagnostic>

- new Nerosoft.Euonia.Osba.OsbaDiagnostic(Nerosoft.Euonia.Osba.OsbaDiagnosticSeverity Severity, System.String Code, System.String Message, System.Type Type = null)
- System.String Code { get; set; }
- System.String Message { get; set; }
- Nerosoft.Euonia.Osba.OsbaDiagnosticSeverity Severity { get; set; }
- System.Type Type { get; set; }

### Nerosoft.Euonia.Osba.OsbaDiagnosticLevel

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Trace |
| Debug |
| Information |
| Warning |
| Error |
| None |


### Nerosoft.Euonia.Osba.OsbaDiagnosticSeverity

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Information |
| Warning |
| Error |


### Nerosoft.Euonia.Osba.OsbaOptions

**类型**：class

- new Nerosoft.Euonia.Osba.OsbaOptions()
- static Nerosoft.Euonia.Osba.OsbaOptions Default { get; }
- Nerosoft.Euonia.Osba.OsbaDiagnosticLevel DiagnosticLevel { get; set; }
- System.Boolean EnableDiagnostics { get; set; }
- System.Boolean StrictFactoryMethods { get; set; }
- System.Boolean ValidateOnStartup { get; set; }

### Nerosoft.Euonia.Osba.OsbaStartupValidator

**类型**：static class

- static void ThrowIfErrors(IReadOnlyList<Nerosoft.Euonia.Osba.OsbaDiagnostic> diagnostics)
- static IReadOnlyList<Nerosoft.Euonia.Osba.OsbaDiagnostic> Validate(IEnumerable<System.Reflection.Assembly> assemblies)

### PropertyInfo<T>

**类型**：class
接口 Nerosoft.Euonia.Osba.IPropertyInfo、Nerosoft.Euonia.Osba.IMemberInfo、System.IComparable

- new PropertyInfo<T>(System.String name)
- new PropertyInfo<T>(System.String name, System.String friendlyName, T defaultValue)
- new PropertyInfo<T>(System.String name, System.String friendlyName, System.Type objectType)
- new PropertyInfo<T>(System.String name, System.String friendlyName, System.Type objectType, T defaultValue)
- T DefaultValue { get; }
- System.String FriendlyName { get; }
- System.Boolean IsChild { get; }
- System.String Name { get; }
- System.Type Type { get; }
- System.Int32 CompareTo(System.Object obj)
- static T GetDefaultValue()
- System.Reflection.PropertyInfo GetPropertyInfo()

### Nerosoft.Euonia.Osba.PropertyInfoList

**类型**：class
基类 List<Nerosoft.Euonia.Osba.IPropertyInfo>；接口 IList<Nerosoft.Euonia.Osba.IPropertyInfo>、ICollection<Nerosoft.Euonia.Osba.IPropertyInfo>、IEnumerable<Nerosoft.Euonia.Osba.IPropertyInfo>、System.Collections.IEnumerable、System.Collections.IList、System.Collections.ICollection、IReadOnlyList<Nerosoft.Euonia.Osba.IPropertyInfo>、IReadOnlyCollection<Nerosoft.Euonia.Osba.IPropertyInfo>

- new Nerosoft.Euonia.Osba.PropertyInfoList()
- new Nerosoft.Euonia.Osba.PropertyInfoList(IEnumerable<Nerosoft.Euonia.Osba.IPropertyInfo> collection)
- System.Boolean IsLocked { get; set; }

### Nerosoft.Euonia.Osba.PropertyInfoManager

**类型**：static class

- static Nerosoft.Euonia.Osba.PropertyInfoList GetRegisteredProperties(System.Type objectType)
- static Nerosoft.Euonia.Osba.IPropertyInfo GetRegisteredProperty(System.Type objectType, System.String propertyName)

### Nerosoft.Euonia.Osba.PropertyRuleBase

**类型**：class
基类 Nerosoft.Euonia.Osba.RuleBase；接口 Nerosoft.Euonia.Osba.IRuleBase、Nerosoft.Euonia.Osba.IPropertyRule

- Nerosoft.Euonia.Osba.IPropertyInfo Property { get; }

### ReadOnlyObject<T>

**类型**：class
基类 BusinessObject<T>；接口 Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、Nerosoft.Euonia.Osba.IHasRuleCheck、Nerosoft.Euonia.Osba.IObjectLifecycle、System.IDisposable、Nerosoft.Euonia.Osba.IBusinessObject、System.ComponentModel.INotifyPropertyChanged、System.ComponentModel.INotifyPropertyChanging、Nerosoft.Euonia.Osba.IReadOnlyObject、Nerosoft.Euonia.Osba.IOperableProperty

- new ReadOnlyObject<T>()
- System.Boolean CanWriteProperty(Nerosoft.Euonia.Osba.IPropertyInfo property)

### Nerosoft.Euonia.Osba.CommonRule.Regular

**类型**：class
基类 Nerosoft.Euonia.Osba.CommonRule.CommonRuleBase；接口 Nerosoft.Euonia.Osba.IRuleBase、Nerosoft.Euonia.Osba.IPropertyRule

- new Nerosoft.Euonia.Osba.CommonRule.Regular(Nerosoft.Euonia.Osba.IPropertyInfo property, System.String expression, System.String message)
- new Nerosoft.Euonia.Osba.CommonRule.Regular(Nerosoft.Euonia.Osba.IPropertyInfo property, System.String expression, Func<System.String> messageFactory)
- System.String Expression { get; }
- System.Boolean IgnoreNullValue { get; set; }
- System.Threading.Tasks.Task ExecuteAsync(Nerosoft.Euonia.Osba.IRuleContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Osba.CommonRule.Required

**类型**：class
基类 Nerosoft.Euonia.Osba.CommonRule.CommonRuleBase；接口 Nerosoft.Euonia.Osba.IRuleBase、Nerosoft.Euonia.Osba.IPropertyRule

- new Nerosoft.Euonia.Osba.CommonRule.Required(Nerosoft.Euonia.Osba.IPropertyInfo property, System.String message)
- new Nerosoft.Euonia.Osba.CommonRule.Required(Nerosoft.Euonia.Osba.IPropertyInfo property, Func<System.String> messageFactory)
- System.Threading.Tasks.Task ExecuteAsync(Nerosoft.Euonia.Osba.IRuleContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Osba.RuleBase

**类型**：class
接口 Nerosoft.Euonia.Osba.IRuleBase

- System.String Name { get; }
- System.Int32 Priority { get; set; }
- List<Nerosoft.Euonia.Osba.IPropertyInfo> RelatedProperties { get; }
- System.Threading.Tasks.Task ExecuteAsync(Nerosoft.Euonia.Osba.IRuleContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Osba.RuleContext

**类型**：class
接口 Nerosoft.Euonia.Osba.IRuleContext

- System.String PropertyName { get; set; }
- IReadOnlyList<Nerosoft.Euonia.Osba.RuleResult> Results { get; }
- Nerosoft.Euonia.Osba.IRuleBase Rule { get; set; }
- System.Object Target { get; set; }
- void AddErrorResult(System.String description)
- void AddInformationResult(System.String description)
- void AddSuccessResult()
- void AddWarningResult(System.String description)
- void Complete()

### Nerosoft.Euonia.Osba.RuleManager

**类型**：class

- System.Boolean Initialized { get; set; }
- List<Nerosoft.Euonia.Osba.IRuleBase> Rules { get; }
- void AddRule(Nerosoft.Euonia.Osba.IRuleBase rule)
- static void CleanRules(System.Type type)
- static Nerosoft.Euonia.Osba.RuleManager GetRules<T>()
- static Nerosoft.Euonia.Osba.RuleManager GetRules(System.Type type)

### Nerosoft.Euonia.Osba.RuleRegistry

**类型**：class
接口 Nerosoft.Euonia.Osba.IRuleRegistry

- new Nerosoft.Euonia.Osba.RuleRegistry()
- void CleanRules(System.Type type)
- Nerosoft.Euonia.Osba.RuleManager GetRules(System.Type type)

### Nerosoft.Euonia.Osba.RuleResult

**类型**：class

- new Nerosoft.Euonia.Osba.RuleResult(System.String ruleName)
- new Nerosoft.Euonia.Osba.RuleResult(System.String ruleName, System.String description, Nerosoft.Euonia.Osba.RuleSeverity severity)
- System.String Description { get; set; }
- IList<Nerosoft.Euonia.Osba.IPropertyInfo> Properties { get; set; }
- System.String RuleName { get; set; }
- Nerosoft.Euonia.Osba.RuleSeverity Severity { get; set; }
- System.Boolean Success { get; set; }

### Nerosoft.Euonia.Osba.Rules

**类型**：class
接口 Nerosoft.Euonia.Osba.IRules

- System.Boolean HasRunningRules { get; }
- System.Boolean IsValid { get; }
- System.Boolean SuppressRuleChecking { get; set; }
- System.Object Target { get; }
- void AddDataAnnotations()
- void AddInstanceRule(Nerosoft.Euonia.Osba.IRuleBase rule)
- void AddInstanceRules(IEnumerable<Nerosoft.Euonia.Osba.IRuleBase> rules)
- void AddRule(Nerosoft.Euonia.Osba.IRuleBase rule)
- void AddRule<TRule>()
- void AddRule<TRule>(System.IServiceProvider provider)
- List<System.String> CheckObjectRules(System.Boolean cascade)
- Task<List<System.String>> CheckObjectRulesAsync(System.Boolean cascade, System.Threading.CancellationToken cancellationToken = default)
- List<System.String> CheckRules(Nerosoft.Euonia.Osba.IPropertyInfo property)

### Nerosoft.Euonia.Osba.RuleSeverity

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Error |
| Warning |
| Information |
| Success |


### Nerosoft.Euonia.Osba.RulesExtensions

**类型**：static class

- static void AddRule<T>(Nerosoft.Euonia.Osba.Rules rules, Nerosoft.Euonia.Osba.IPropertyInfo property, Func<T, Task<System.Boolean>> handler, System.String message)
- static void AddRule<T>(Nerosoft.Euonia.Osba.Rules rules, Nerosoft.Euonia.Osba.IPropertyInfo property, Func<T, Task<System.Boolean>> handler, Func<System.String> message)
- static void AddRule<T>(Nerosoft.Euonia.Osba.Rules rules, Nerosoft.Euonia.Osba.IPropertyInfo property, Func<T, System.Boolean> handler, System.String message)
- static void AddRule<T>(Nerosoft.Euonia.Osba.Rules rules, Nerosoft.Euonia.Osba.IPropertyInfo property, Func<T, System.Boolean> handler, Func<System.String> message)

### Nerosoft.Euonia.Osba.SavedEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Osba.SavedEventArgs(System.Object newObject)
- new Nerosoft.Euonia.Osba.SavedEventArgs(System.Object newObject, System.Exception error, System.Object userState)
- System.Exception Error { get; }
- System.Object NewObject { get; }
- System.Object UserState { get; }

### Nerosoft.Euonia.Osba.ScopeOperationMap

**类型**：static class

- static System.String FromEditState(Nerosoft.Euonia.Osba.ObjectEditState state)
- static System.String Resolve(System.Object target)
- static System.Boolean TryResolve(System.Object target, out System.String operation)

### ServiceObject<T>

**类型**：class
基类 ObjectBase<T>；接口 Nerosoft.Euonia.Osba.IOsbaObject、Nerosoft.Euonia.Osba.IUseBusinessContext、Nerosoft.Euonia.Osba.IHasRuleCheck、Nerosoft.Euonia.Osba.IObjectLifecycle、System.IDisposable、Nerosoft.Euonia.Osba.IServiceObject

_无公共成员。_

### UpdateActuator<TTarget>

**类型**：class
基类 EditableActuator<TTarget>

- new UpdateActuator<TTarget>(ActuatorBuilder<TTarget> builder, Func<Task<TTarget>> factory)

### Nerosoft.Euonia.Osba.ValidationState

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| NotValidated |
| Valid |
| Invalid |
