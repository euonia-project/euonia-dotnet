# Euonia.Validation API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

`Euonia.Validation` 提供统一的 `Validator.Validate<T>` 入口、可绑定的 `ValidatableObject<T>`、轻量规则，以及 FluentValidation 自动注册集成。

- **包名**：`Euonia.Validation`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Validation`
- **完整选项/边界**：[Source/Euonia.Validation/README.md](../../../Source/Euonia.Validation/README.md)

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

- [教程](TUTORIAL.md)
- [Source README：完整规则、工厂与异常模型](../../../Source/Euonia.Validation/README.md)
- [Euonia.Modularity 用户手册](../Euonia.Modularity/API.md) —— 模块初始化与自动注册
- [Euonia.Application 用户手册](../Euonia.Application/API.md) —— 校验拦截器与管道行为
- [Euonia.Hosting 用户手册](../Euonia.Hosting/API.md) —— 把校验异常映射为 HTTP 400
- [仓库根 README](../../../README.md)
