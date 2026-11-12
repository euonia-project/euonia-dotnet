# Euonia.Validation 用户手册

`Euonia.Validation` 提供统一的 `Validator.Validate<T>` 入口、可绑定的 `ValidatableObject<T>`、轻量规则，以及 FluentValidation 自动注册集成。

- **包名**：`Euonia.Validation`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Validation`
- **完整选项/边界**：[Source/Euonia.Validation/README.md](../../Source/Euonia.Validation/README.md)

## 教程：在模块化应用里校验订单输入

这个场景用 `ValidationModule` 自动注册 FluentValidation 验证器并设置全局工厂，然后校验一个非法订单，打印字段级错误。

### 前置条件

- .NET 10 或 .NET 11 SDK。
- 不要求 ASP.NET Core；控制台宿主即可。

### 1. 创建项目并安装

```bash
dotnet new console -n OrderValidation
cd OrderValidation
dotnet add package Euonia.Validation
```

### 2. 定义对象与 FluentValidation 验证器

```csharp
using FluentValidation;

public sealed class Order
{
    public string Number { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(order => order.Number).NotEmpty().Matches(@"^SO-\d+$");
        RuleFor(order => order.Amount).GreaterThan(0);
    }
}
```

### 3. 依赖 ValidationModule

```csharp
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Validation;

[DependsOn(typeof(ValidationModule))]
public sealed class AppModule : ModuleContextBase
{
}
```

### 4. 初始化并执行校验

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Validation;

var services = new ServiceCollection();
var configuration = ConfigurationHelper.BuildConfiguration(new ConfigurationBuilderOptions
{
    FileName = "appsettings",
    EnvironmentName = "Development"
});

using var application = ApplicationFactory.Create<AppModule>(services, configuration);
using var provider = services.BuildServiceProvider();

// ValidationModule 在初始化阶段调用 ValidatorFactory.SetCurrent。
application.Initialize(provider);

try
{
    Validator.Validate(new Order { Number = "bad", Amount = 0 });
}
catch (ValidationException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
}
```

### 5. 运行与验证

```bash
dotnet run
```

预期输出包含两个字段错误：

```text
Number: 'Number' is not in the correct format.
Amount: 'Amount' must be greater than '0'.
```

默认错误文本可能随 FluentValidation 版本或区域设置变化，断言时优先匹配 `PropertyName`。

把 `Order` 改成 `Number = "SO-001", Amount = 100`，`Validator.Validate` 不再抛异常，说明验证器已生效。

### 常见失败点

- **校验完全不执行**：`AddValidator` 不会调用 `ValidatorFactory.SetCurrent`；模块化应用必须加载 `ValidationModule`，非模块化宿主需手动设置全局工厂。
- **验证器没注册**：验证器必须是实现 `FluentValidation.IValidator<T>` 的类，且 `T` 是类/非抽象/非枚举；结构体或枚举验证器会被跳过。
- **空值正则抛异常**：`RegularValidator` 直接调用 `Regex.IsMatch(value, Pattern)`，`null` 会抛 `ArgumentNullException`；先做非空判断。
- **异常消息为空**：FluentValidation 路径的 `ValidationException.Message` 是空字符串，错误在 `Errors` 集合中。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `Validator.Validate<T>(item)` | 静态同步校验入口 | `T : class`；失败抛 `ValidationException` |
| `Validator.ValidateAsync<T>(item)` | 异步校验入口 | 返回 `Task` |
| `ValidationModule` | 注册验证器工厂并启用约定扫描 | 通过 `[DependsOn]` 引入 |
| `AddValidator(IEnumerable<Type> types)` | 注册 FluentValidation 验证器与工厂 | 空集合不注册工厂 |
| `AddValidator(Func<IEnumerable<Type>> factory)` | 延迟提供验证器类型 | 工厂返回空集合时不注册 |
| `IValidator` / `DefaultValidator` | 校验契约与默认实现 | 从容器解析 `IValidator<T>` |
| `IValidatorFactory` / `DefaultValidatorFactory` | 创建 `IValidator` | `DefaultValidatorFactory` 依赖 `IServiceProvider` |
| `ValidatorFactory.SetCurrent(factory)` | 设置全局工厂 | 非模块化宿主必须调用 |
| `ValidatableObject<T>` | 可绑定、可校验的值对象 | `Value`、`IsValid`、`Errors` |
| `ValidatableObject<T>.ValidateOnChanged` | 值变化时自动校验 | 默认 `false` |
| `ValidatableObject<T>.UseValidator(rule)` | 追加轻量规则 | 规则不会自动清空 |
| `IObjectValidator<T>` / `AbstractValidator<T>` | 轻量规则契约/基类 | `Validate(TValue)` |
| `GenericValidator<T>` | 用表达式构造规则 | 返回错误消息或 `bool + message` |
| `RegularValidator` | 正则规则 | 模式 + 错误消息；空值不安全 |
| `RangeValidator<T>` | 闭区间规则 | 要求 `minimum < maximum` |
| `ValidationResult` | 字段级错误模型 | `PropertyName`、`ErrorMessage` |
| `ValidationException` | 携带错误集合的异常 | `Errors`；FluentValidation 路径 `Message` 为空 |
| `ValidationRegistrar` | `ValidationModule` 使用的约定注册器 | 生命周期默认 `Transient` |

### 配置说明

`Euonia.Validation` 没有配置节或 `*Options`；验证器来源、生命周期和可绑定对象行为都由代码控制，因此本页不提供 appsettings 示例。

## 相关文档

- [Source README：完整规则、工厂与异常模型](../../Source/Euonia.Validation/README.md)
- [Euonia.Modularity 用户手册](./Euonia.Modularity.md) —— 模块初始化与自动注册
- [Euonia.Application 用户手册](./Euonia.Application.md) —— 校验拦截器与管道行为
- [Euonia.Hosting 用户手册](./Euonia.Hosting.md) —— 把校验异常映射为 HTTP 400
- [仓库根 README](../../README.md)
