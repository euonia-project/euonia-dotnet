# Euonia.Validation

Euonia 的对象校验框架：提供统一的 `Validator.Validate<T>` 入口、可绑定的 `ValidatableObject<T>`、轻量规则（泛型、正则、范围）以及 FluentValidation 自动注册集成。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Validation`
- **NuGet 包**：`Euonia.Validation`

## 安装

```bash
dotnet add package Euonia.Validation
```

## 主要功能

- **统一入口**：`Validator.Validate<T>` / `ValidateAsync<T>`，失败时抛出 `ValidationException`。
- **FluentValidation 集成**：`AddValidator(types)` 自动注册 `IValidator<T>` 实现；`ValidationModule` 启用约定扫描。
- **可绑定对象**：`ValidatableObject<T>` 实现 `INotifyPropertyChanged`，支持值变更即时校验与错误集合。
- **轻量规则**：`GenericValidator<T>`、`RegularValidator`、`RangeValidator<T>`。
- **工厂**：`ValidatorFactory.SetCurrent` / `Create`，便于手动控制验证器来源。
- **结果模型**：`ValidationResult` 与 `ValidationException`。

## 快速开始

使用 FluentValidation：

```csharp
using FluentValidation;
using Nerosoft.Euonia.Validation;

public sealed class Order
{
    public string Number { get; set; } = null!;
    public decimal Amount { get; set; }
}

public sealed class OrderValidator : FluentValidation.AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(order => order.Number).NotEmpty();
        RuleFor(order => order.Amount).GreaterThan(0);
    }
}
```

模块化应用直接依赖 `ValidationModule`，由它在应用初始化时设置全局工厂，并按约定注册 FluentValidation 验证器：

```csharp
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Validation;

[DependsOn(typeof(ValidationModule))]
public sealed class SalesModule : ModuleContextBase
{
}
```

非模块化宿主需要注册验证器并显式设置全局工厂：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Validation;

services.AddValidator(new[] { typeof(OrderValidator) });

// 或扫描程序集
services.AddValidator(() => typeof(Program).Assembly.GetExportedTypes());

var provider = services.BuildServiceProvider();
ValidatorFactory.SetCurrent(provider.GetRequiredService<IValidatorFactory>());
```

执行校验：

```csharp
try
{
    Validator.Validate(order);
}
catch (ValidationException ex)
{
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
}
```

使用可绑定对象：

```csharp
using Nerosoft.Euonia.Validation;

var number = new ValidatableObject<string>
{
    Value = "SO-001",
    ValidateOnChanged = true
};

number.UseValidator(new RegularValidator(@"^SO-\d+$", "订单号格式不正确"));
number.Validate();

if (!number.IsValid)
{
    // number.Errors
}
```

使用轻量规则：

```csharp
var amount = new ValidatableObject<decimal> { Value = 100m };
amount.UseValidator(new RangeValidator<decimal>(1m, 10_000m, "金额超出范围"));
amount.Validate();
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `Validator` | 静态校验入口 |
| `IValidator` / `DefaultValidator` | 校验契约与默认实现 |
| `IValidatorFactory` / `DefaultValidatorFactory` / `ValidatorFactory` | 验证器工厂与当前工厂 |
| `ValidatableObject<T>` | 可绑定、可校验的值对象 |
| `IObjectValidator<T>` / `AbstractValidator<T>` | 轻量规则契约与基类 |
| `GenericValidator<T>` / `RegularValidator` / `RangeValidator<T>` | 内置规则 |
| `ValidationResult` / `ValidationException` | 错误模型与异常 |
| `ValidationModule` / `AddValidator(...)` | 模块与注册扩展 |

## 选项（代码配置）

`Euonia.Validation` 没有配置节或 `*Options` 类型；验证器来源与可绑定对象行为均由代码控制：

| 配置点 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `AddValidator(IEnumerable<Type>)` | 方法 | — | 注册 FluentValidation 验证器（Singleton）与 `IValidatorFactory` |
| `AddValidator(Func<IEnumerable<Type>>)` | 方法 | — | 注册时调用工厂求值并注册验证器类型集合（非延迟到解析时） |
| `ValidatableObject<T>.ValidateOnChanged` | `bool` | `false` | 值变化时是否立即执行 `Validate()` |
| `ValidatableObject<T>.ValueChangedCallback` | `Action<T>?` | `null` | 值变化后的回调 |
| `GenericValidator<T>` | 构造函数 | — | 用“返回错误消息”或“返回 bool + 消息”的表达式构建规则 |
| `RangeValidator<T>` | 构造函数 | — | 闭区间规则，要求 `minimum < maximum` |
| `RegularValidator` | 构造函数 | — | 正则规则，模式与错误消息由调用方提供 |

## 注意事项 / 限制

- `AddValidator` 只把验证器与 `IValidatorFactory` 注册到容器，**不会**调用 `ValidatorFactory.SetCurrent`；非模块化宿主若直接使用静态 `Validator.Validate`，必须先显式设置全局工厂，否则校验会静默跳过。模块化应用应依赖 `ValidationModule`，它在 `OnApplicationInitialization` 中完成设置。
- 验证器以 Singleton 注册，必须无状态或线程安全；不要在验证器构造函数中捕获 Scoped 服务。
- `AddValidator` 只注册实现 `FluentValidation.IValidator<T>` 且 `T` 为类、非抽象、非枚举的类型；结构体/枚举验证器会被跳过。
- `ValidatableObject<T>.ValidateOnChanged` 仅在值真正变化时触发；`UseValidator` 只追加规则，不会清空已有规则，重复调用会叠加。
- `RegularValidator.Validate` 直接调用 `Regex.IsMatch(value, Pattern)`，`value` 为 `null` 时抛出异常；`RangeValidator<T>` 两端都包含，且构造时要求最小值小于最大值。
- 对实现 `IValidatableObject` 的对象，`Validator.Validate` 会把错误统一包装成 `PropertyName = "Value"` 的 `ValidationResult`；FluentValidation 路径则保留各属性的名称与消息。
- `ValidationException.Message` 在 FluentValidation 路径下为空字符串，调用方应遍历 `Errors` 而不是依赖异常消息。

## 常见问题与故障排查

### `Validator.Validate` 不抛异常，验证器像没执行
**原因**：`Validator.Validate` 通过全局 `ValidatorFactory` 创建验证器；`AddValidator` 只注册服务，不会调用 `ValidatorFactory.SetCurrent`。非模块化宿主未显式设置工厂时，`ValidatorFactory.Create()` 返回 `null`，校验被静默跳过。
**解决**：模块化应用依赖 `ValidationModule`；非模块化宿主在构建容器后执行 `ValidatorFactory.SetCurrent(provider.GetRequiredService<IValidatorFactory>())`，并在启动测试中断言工厂非空。

### `AddValidator` 注册了验证器但目标类型仍不校验
**原因**：`AddValidator` 只接受实现 `FluentValidation.IValidator<T>` 的类，且 `T` 必须是类、非抽象、非枚举；结构体/枚举验证器会被跳过。传入空集合时还会直接返回，连 `IValidatorFactory` 都不会注册。
**解决**：确认验证器是类并实现 `IValidator<T>`，目标类型是类；不要用空集合“占位”，需要工厂时显式注册 `DefaultValidatorFactory`。

### `RegularValidator` 对空值抛异常
**原因**：`RegularValidator.Validate` 直接调用 `Regex.IsMatch(value, Pattern)`，`value` 为 `null` 时 .NET 正则 API 抛 `ArgumentNullException`。
**解决**：在规则前增加空值处理，或改用 `GenericValidator<T>` 组合“非空 + 正则”判断；不要把可空字段直接交给 `RegularValidator`。

### 捕获到的 `ValidationException.Message` 是空字符串
**原因**：FluentValidation 路径使用 `new ValidationException(string.Empty, errors)` 构造异常，错误明细在 `Errors` 集合中，消息本身为空。
**解决**：遍历 `ex.Errors` 读取 `PropertyName`/`ErrorMessage`；需要单条消息时自行拼接，不要依赖 `ex.Message` 判断失败原因。

## 最佳实践

- 模块化应用统一依赖 `ValidationModule`；非模块化宿主只设置一次全局工厂，并在启动时解析一次验证器做冒烟测试。
- 验证器保持无状态、线程安全；不要在验证器构造函数中注入 Scoped 服务，因为 `AddValidator` 以 Singleton 注册。
- 在 API/消息边界执行 `Validator.Validate`，把 `ValidationException.Errors` 映射为 400 响应字段错误，而不是让异常冒泡成 500。
- 为可空字段、空集合与多条件规则写测试；`ValidatableObject<T>` 的 `ValidateOnChanged` 只在值变化时触发，测试中显式调用 `Validate()`。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`；包版本由仓库根 `project.props` 统一管理（当前 `2026.5.0`）。
- 关键依赖：FluentValidation 12.2.0（由 `Directory.Packages.props` 锁定）；没有配置节或 `*Options`。
- 已知边界：验证器以 Singleton 注册；结构体/枚举目标被跳过；全局工厂需手动设置；FluentValidation 异常的 `Message` 为空。
- 升级提示：升级 FluentValidation 后复核 `IValidator<T>` 接口实现与 `ValidationResult.Errors` 结构，并重跑空值/多条件规则测试。

## 相关包

- [`Euonia.Application`](../Euonia.Application/README.md) —— 应用层校验拦截器与管道行为
- [`Euonia.Core`](../Euonia.Core/README.md) —— 断言与基础工具
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与自动注册

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
