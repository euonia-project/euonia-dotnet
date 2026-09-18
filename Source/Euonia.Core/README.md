# 概述

Euonia.Core 是 Euonia 框架的基础库。它提供了一组必备的、与框架无关的基础构建模块——集合、反射辅助工具、扩展方法、线程抽象、释放原语、异常类型和通用工具类——这些模块被 Euonia 的所有其他包所复用。

# 功能特性

- **集合**：丰富的集合类型，如 `DequeCollection`、`TypeList`、`ObservableGroup`、`TreeView`、`PageableCollection` 以及只读/可比较变体。
- **反射**：类型内省、程序集加载（`AssemblyHelper`、`AssemblyLoadContextManager`）、枚举解析/辅助、属性访问缓存和高性能的 `Reflect` 工具。
- **扩展方法**：针对字符串、集合、枚举、类型、对象、异常、事件、声明、线程等的流畅扩展方法。
- **线程**：以异步为先的同步原语（`AsyncLock`、`AsyncSemaphore`、`AsyncAutoResetEvent`、`AsyncLazy`、`AsyncMonitor` 等）、`AsyncContext` 环境、有状态互斥锁、超时和任务辅助类。
- **释放**：幂等、线程安全、同步和异步的释放原语，用于安全管理资源。
- **异常**：预定义、识别 HTTP 状态的异常类型（`BadRequestException`、`NotFoundException`、`ConflictException`、`BusinessException` 等），并提供基于资源文件的异常提示。
- **安全**：核心安全异常、声明类型以及 `UserPrincipal` 模型。
- **系统工具类**：`Optional<T>`、`Clock`、`Singleton`、`ObjectPool`、`StringBuilderPool`、ID 生成器（`ObjectId`、`SnowflakeId`、`UlidGenerator`、`GuidGenerator`、`ShortUniqueId`、`RandomId`）、参数断言/结果断言等。

# 快速开始

要开始使用 Euonia.Core，请遵循以下步骤：

1. **安装包**：通过 NuGet 将 Euonia.Core 包添加到您的项目。
   ```bash
   dotnet add package Euonia.Core
   ```

2. **使用库**：该库设计为直接使用。例如，生成时间有序的唯一标识符：
   ```csharp
   using Nerosoft.Euonia.Core;

   var snowflakeId = SnowflakeId.Create();
   var objectId = ObjectId.NewObjectId();
   ```

3. **探索工具类**：在代码中使用内置的辅助方法。例如，校验无效参数：
   ```csharp
   Check.ArgumentNotNull(customer, nameof(customer));
   ```

4. **处理异常**：在服务代码中依赖内置的 HTTP 感知异常：
   ```csharp
   throw new NotFoundException("Customer not found.");
   ```

5. **运行您的应用程序**：构建并运行您的应用程序——Euonia.Core 现已可用于解决方案的各个层。

# 文档

有关更详细的信息和高级用法，请参阅 [Euonia GitHub 仓库](https://github.com/NerooftDev/Euonia) 上的官方文档。