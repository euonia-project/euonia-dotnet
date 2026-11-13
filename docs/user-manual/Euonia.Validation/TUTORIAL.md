# Euonia.Validation 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

`Euonia.Validation` 提供统一的 `Validator.Validate<T>` 入口、可绑定的 `ValidatableObject<T>`、轻量规则，以及 FluentValidation 自动注册集成。

- **包名**：`Euonia.Validation`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Validation`
- **完整选项/边界**：[Source/Euonia.Validation/README.md](../../../Source/Euonia.Validation/README.md)

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

## 相关文档

- [API 参考](API.md)
- [Source README：完整规则、工厂与异常模型](../../../Source/Euonia.Validation/README.md)
- [Euonia.Modularity 用户手册](../Euonia.Modularity/TUTORIAL.md) —— 模块初始化与自动注册
- [Euonia.Application 用户手册](../Euonia.Application/TUTORIAL.md) —— 校验拦截器与管道行为
- [Euonia.Hosting 用户手册](../Euonia.Hosting/TUTORIAL.md) —— 把校验异常映射为 HTTP 400
- [仓库根 README](../../../README.md)
