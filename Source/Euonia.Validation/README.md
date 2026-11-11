# Euonia.Validation

Euonia 的对象校验框架：提供统一的 `Validator.Validate<T>` 入口、可绑定的 `ValidatableObject<T>`、轻量规则（泛型、正则、范围）以及 FluentValidation 自动注册集成。

Object validation framework for Euonia: a unified `Validator.Validate<T>` entry point, bindable `ValidatableObject<T>`, lightweight rules (generic, regex, range) and automatic FluentValidation registration.

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

注册验证器：

```csharp
using Nerosoft.Euonia.Validation;

services.AddValidator(new[] { typeof(OrderValidator) });

// 或扫描程序集
services.AddValidator(() => typeof(Program).Assembly.GetExportedTypes());
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

## 相关包

- [`Euonia.Application`](../Euonia.Application/README.md) —— 应用层校验拦截器与管道行为
- [`Euonia.Core`](../Euonia.Core/README.md) —— 断言与基础工具
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与自动注册

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
