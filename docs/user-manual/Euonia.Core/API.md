# Euonia.Core API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

Euonia 的基础库：断言、ID 生成、异步协调原语、对象池、释放模式、反射/集合工具与常用扩展方法。本页是 API 速查；安装、完整类型清单与注意事项见 [项目 README](../../../Source/Euonia.Core/README.md)。

- **NuGet 包**：`Euonia.Core`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.*`；`Check`、`ObjectId`、`Extensions` 在**全局命名空间**，`Optional<T>`、`ObjectPool<T>`、`Singleton<T>` 在 `System`
- **第三方依赖**：无（仅 BCL）
- **完整参考**：[Source/Euonia.Core/README.md](../../../Source/Euonia.Core/README.md)

## API 参考

### 断言与校验

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `Check.Ensure(bool, string)` | 状态断言，失败抛 `InvalidOperationException` | 第二参数为消息模板 |
| `Check.EnsureNotNull<T>(T, string)` | 泛型空值断言，失败抛 `ArgumentNullException` | 第二参数为参数名 |
| `Check.EnsureNotNull(string, string, int maxLength, int minLength)` | 字符串非 null + 长度范围，失败抛 `ArgumentException` | `maxLength = int.MaxValue`、`minLength = 0` |
| `Check.EnsureNotNullOrEmpty(string, string)` | 字符串非 null/空，失败抛 `ArgumentException` | 仅两个参数 |
| `Check.EnsureIsMatch(string, string, string, Action<string>, RegexOptions)` | 正则断言 | 第四参数是失败回调，第五参数默认 `RegexOptions.None` |
| `CheckResult<TValue>` | 携带值的校验结果，可链式 `Success` / `Failure` | `IsValid`、隐式转 `TValue` |
| `Invariant.Require(bool, string?)` | 不变式断言（`[Conditional("DEBUG")]`，仅 Debug 生效） | 失败抛 `InvalidOperationException` |

### ID 与时间

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `ObjectId.Snowflake()` | 雪花 ID（long） | 机器位固定 0，单进程用 |
| `ObjectId.Ulid()` | 26 位 ULID 字符串 | 按时间有序，推荐分布式使用 |
| `ObjectId.Guid(GuidType)` | 生成指定风格的 Guid 并包装为 `ObjectId` | `GuidType.SequentialAtEnd` 等 5 种 |
| `ObjectId.Random()` | 随机字符串 ID | 基于时间戳种子 |
| `ObjectId.NewGuid(GuidType)` | 只返回 `Guid` | 与 `Guid(GuidType)` 对应 |
| `ObjectId<T>` | 泛型变体（`Guid` / `long` 等） | 与基础类型双向隐式转换 |
| `Clock.GetUnixTimestampMillis()` | Unix 毫秒时间戳 | 另有 `GetUnixTimestampTicks`、`ToUnixTimestampMillis` |

### 异步与生命周期

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AsyncLock.LockAsync(CancellationToken)` | 异步互斥锁，**不可重入** | 返回 `Task<IDisposable>`，配合 `using` |
| `AsyncSemaphore.WaitAsync(CancellationToken)` | 异步信号量 | 构造时指定初始许可数 |
| `AsyncLazy<T>` | 异步惰性初始化 | 工厂只执行一次 |
| `ObjectPool<T>(IObjectPoolPolicy<T>, int? maxItems)` | 基于策略的对象池 | `maxItems` 默认不限 |
| `IObjectPoolPolicy<T>.CreateNew()` / `Return(T)` | 池化策略 | `Return` 返回 `false` 表示丢弃 |
| `DisposableObject` | 幂等释放基类，重写 `Dispose(bool)` | 提供 `Disposed` 事件 |
| `Singleton<T>.Get(Func<T>)` / `Instance` | 进程级单例容器 | 未初始化直接读 `Instance` 抛 `KeyNotFoundException` |

### 集合、反射与扩展

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `DequeCollection<T>` | 环形缓冲双端队列，两端 O(1) | `AddToFront` / `AddToBack` / `RemoveFromFront` |
| `TypeList<T>` | 类型列表（用于程序集/类型集合） | 实现 `ITypeList<T>` |
| `PageableCollection<T>` | 分页集合 | 页码从 1 开始 |
| `Reflect.GetValue(object, Type, string)` | 按路径反射读值 | 支持 `"Address.City"` |
| `PropertyAccessorCache<T>` | 属性访问器缓存 | 避免重复反射 |
| `"PayWeb".ToKebabCase()` | 命名风格转换 | 另有 `ToCamelCase` / `ToSnakeCase` |
| `dictionary.GetOrAdd(key, factory)` | 字典取值或创建 | 两个工厂重载 |
| `items.ForEach(action)` | 集合遍历 | 返回 `void` |
| `Extensions.GetValue<TValue>` | 按点/斜杠路径读取 `JsonNode` | 路径不存在返回 `default` |

### 权限契约

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `PermissionAttribute(string permission, params string[] roles)` | 声明权限要求；`permission` 为任意非空字符串、单一维度 | 可多次；类型级 / 方法级 |
| `PermissionRequirements.For(Type)` / `For(MethodInfo)` / `For(Type, MethodInfo?)` | 读声明（带缓存、裁剪、去重） | `method` 为 `null` 时只读类型级 |
| `PermissionRequirements.Merge(params IEnumerable<PermissionAttribute>[])` | 多来源并集 | 按「码 + 角色」去重 |
| `IPermissionRequirementSource.RequirementsFor(Type, MethodInfo?, string)` | 要求来源（推荐，可选） | 不注册也能工作 |
| `IPermissionChecker` / `IObjectScopeAuthorizer` / `IObjectOperationResolver` | 操作权限判定 / 行级数据权限判定 / 对象状态 → 操作 | 跨边界契约 |
| `BusinessOperation` | **数据权限**的授权标识常量 | `read` / `create` / `update` / `delete` / `execute` |

已废弃成员：

| 成员 | 现状 | 替代 |
|---|---|---|
| `IPermissionCodeSource` | 保留，标 `[Obsolete]` | `IPermissionRequirementSource` |
| `PermissionRequirements.Adapt` | 保留，标在契约兼容路径 | 直接使用 `PermissionRequirements` |

### 选项（代码配置）

`Euonia.Core` **没有配置节**，也不读取 `IConfiguration`：静态工具直接调用，可实例化的类型直接 `new`，权限契约由使用方实现；无需 `appsettings` 与环境变量。

## 密码学

命名空间：`Nerosoft.Euonia.Security`。

| 类型 | 关键成员 | 说明 |
|---|---|---|
| `Cryptography.AES` | `Encrypt(string?, string/byte[])`、`Decrypt(string?, string/byte[])` | ECB + PKCS7，兼容既有数据 |
| `Cryptography.DES` | `Encrypt(string, string/byte[])`、`Decrypt(string, byte[])` | 字符串密钥走 DES；32 字节密钥走 AES；ECB + PKCS7 |
| `Cryptography.MD5` | `Encrypt(string)` | 小写十六进制 MD5 摘要 |
| `Cryptography.SHA` | `Encrypt(string)` | SHA-256，Base64 输出 |
| `Cryptography.Base64` | `Encode` / `Decode` | UTF-8 与 Base64 互转 |
| `Cryptography.RSA` | 构造、`Encrypt`、`Decrypt`、`Dispose` | PEM / PKCS#1 / PKCS#8 / SPKI；PKCS#1 v1.5 填充 |
| `PasswordComplexity` | Flags 枚举 | 小写/大写/数字/符号 |
| `PasswordGenerator` | `GeneratePassword(complexity, minLength, maxLength)` | 保证每种要求的字符至少出现一次 |
| `RandomUtility` | `GenerateRandomString` / `GenerateRandomDigit` / `GenerateRandomBytes` | CSPRNG；`Random` 的安全实现 |

行为与安全约束：

- AES 密钥长度必须为 16/24/32 字节；空输入返回 `null`；无参重载使用固定默认盐值。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Core/README.md) —— 完整类型清单、注意事项、版本与兼容
- [Euonia.Mapping 用户手册](../Euonia.Mapping/API.md) —— 对象映射抽象
- [仓库根 README](../../../README.md)
