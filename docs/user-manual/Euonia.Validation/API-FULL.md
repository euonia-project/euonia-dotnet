# Euonia.Validation 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Validation |
| 公共类型 | 19 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static void AddValidator(Microsoft.Extensions.DependencyInjection.IServiceCollection services, IEnumerable<System.Type> types)
- static void AddValidator(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Func<IEnumerable<System.Type>> factory)

## 命名空间 Nerosoft.Euonia.Validation

### AbstractValidator<TValue>

**类型**：class
接口 IObjectValidator<TValue>

- System.String Message { get; set; }
- System.Boolean Validate(TValue value)

### Nerosoft.Euonia.Validation.DefaultValidator

**类型**：class
接口 Nerosoft.Euonia.Validation.IValidator

- new Nerosoft.Euonia.Validation.DefaultValidator(System.IServiceProvider provider)
- void Validate<T>(T item)
- System.Threading.Tasks.Task ValidateAsync<T>(T item)

### Nerosoft.Euonia.Validation.DefaultValidatorFactory

**类型**：class
接口 Nerosoft.Euonia.Validation.IValidatorFactory

- new Nerosoft.Euonia.Validation.DefaultValidatorFactory(System.IServiceProvider serviceProvider)
- Nerosoft.Euonia.Validation.IValidator Create()

### GenericValidator<TValue>

**类型**：class
基类 AbstractValidator<TValue>；接口 IObjectValidator<TValue>

- new GenericValidator<TValue>(Expression<Func<TValue, System.String>> expression)
- new GenericValidator<TValue>(Expression<Func<TValue, System.Boolean>> expression, System.String message)
- System.Boolean Validate(TValue value)

### IObjectValidator<TValue>

**类型**：interface

- System.String Message { get; set; }
- System.Boolean Validate(TValue value)

### Nerosoft.Euonia.Validation.IValidatableObject

**类型**：interface

- ObservableCollection<System.String> Errors { get; }
- System.Boolean IsValid { get; }
- void Validate()

### Nerosoft.Euonia.Validation.IValidator

**类型**：interface

- void Validate<TObject>(TObject item)
- System.Threading.Tasks.Task ValidateAsync<TObject>(TObject item)

### Nerosoft.Euonia.Validation.IValidatorFactory

**类型**：interface

- Nerosoft.Euonia.Validation.IValidator Create()

### RangeValidator<TValue>

**类型**：class
基类 AbstractValidator<TValue>；接口 IObjectValidator<TValue>

- new RangeValidator<TValue>(TValue minimum, TValue maximum, System.String message)
- TValue Maximum { get; }
- TValue Minimum { get; }
- System.Boolean Validate(TValue value)

### Nerosoft.Euonia.Validation.RegularValidator

**类型**：class
基类 AbstractValidator<System.String>；接口 IObjectValidator<System.String>

- new Nerosoft.Euonia.Validation.RegularValidator(System.String pattern, System.String message)
- System.String Pattern { get; }
- System.Boolean Validate(System.String value)

### ValidatableObject<TValue>

**类型**：class
接口 Nerosoft.Euonia.Validation.IValidatableObject、System.ComponentModel.INotifyPropertyChanged

- new ValidatableObject<TValue>()
- ObservableCollection<System.String> Errors { get; }
- System.Boolean IsValid { get; set; }
- IReadOnlyList<IObjectValidator<TValue>> Rules { get; }
- System.Boolean ValidateOnChanged { get; set; }
- TValue Value { get; set; }
- Action<TValue> ValueChangedCallback { get; set; }
- event System.ComponentModel.PropertyChangedEventHandler PropertyChanged
- ValidatableObject<TValue> UseValidator(IObjectValidator<TValue> rule)
- void Validate()

### Nerosoft.Euonia.Validation.ValidationAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Validation.ValidationAttribute()
- new Nerosoft.Euonia.Validation.ValidationAttribute(System.Type validatorType)
- System.Type ValidatorType { get; }

### Nerosoft.Euonia.Validation.ValidationException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Validation.ValidationException(System.String message, Nerosoft.Euonia.Validation.ValidationResult[] results)
- new Nerosoft.Euonia.Validation.ValidationException(System.String message, IEnumerable<Nerosoft.Euonia.Validation.ValidationResult> errors)
- new Nerosoft.Euonia.Validation.ValidationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
- IEnumerable<Nerosoft.Euonia.Validation.ValidationResult> Errors { get; }
- void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)

### Nerosoft.Euonia.Validation.ValidationModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Validation.ValidationModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void OnApplicationInitialization(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)

### Nerosoft.Euonia.Validation.ValidationRegistrar

**类型**：class
基类 Nerosoft.Euonia.Modularity.DefaultAutomaticRegistration；接口 Nerosoft.Euonia.Modularity.IAutomaticRegistration

- new Nerosoft.Euonia.Validation.ValidationRegistrar()

### Nerosoft.Euonia.Validation.ValidationResult

**类型**：class

- new Nerosoft.Euonia.Validation.ValidationResult()
- new Nerosoft.Euonia.Validation.ValidationResult(System.String propertyName, System.String errorMessage)
- System.String ErrorMessage { get; set; }
- System.String PropertyName { get; set; }

### Nerosoft.Euonia.Validation.Validator

**类型**：class

- new Nerosoft.Euonia.Validation.Validator()
- static void Validate<T>(T item)
- static System.Threading.Tasks.Task ValidateAsync<T>(T item)

### Nerosoft.Euonia.Validation.ValidatorFactory

**类型**：class

- new Nerosoft.Euonia.Validation.ValidatorFactory()
- static Nerosoft.Euonia.Validation.IValidator Create()
- static void SetCurrent(Nerosoft.Euonia.Validation.IValidatorFactory factory)
