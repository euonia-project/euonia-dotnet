# Overview

Euonia.Core is the foundation library of the Euonia framework. It provides a set of essential, framework-agnostic building blocks—collections, reflection helpers, extension methods, threading abstractions, disposal primitives, exception types, and common utilities—that are reused across all other Euonia packages.

# Features

- **Collections**: Rich collection types such as `DequeCollection`, `TypeList`, `ObservableGroup`, `TreeView`, `PageableCollection`, and read-only/equatable variants.
- **Reflection**: Helpers for type introspection, assembly loading (`AssemblyHelper`, `AssemblyLoadContextManager`), enum parsing/helpers, property access caching, and a high-performance `Reflect` utility.
- **Extensions**: Fluent extension methods for strings, collections, enums, types, objects, exceptions, events, claims, threading, and more.
- **Threading**: Async-first coordination primitives (`AsyncLock`, `AsyncSemaphore`, `AsyncAutoResetEvent`, `AsyncLazy`, `AsyncMonitor`, ...), an `AsyncContext` environment, stateful mutexes, timeouts, and task helpers.
- **Disposing**: Idempotent, thread-safe synchronous and asynchronous disposable primitives for safe resource management.
- **Exceptions**: Predefined, HTTP-status-aware exception types (`BadRequestException`, `NotFoundException`, `ConflictException`, `BusinessException`, ...) with resource-driven exception prompts.
- **Security**: Core security exceptions, claim types, and the `UserPrincipal` model.
- **System Utilities**: `Optional<T>`, `Clock`, `Singleton`, `ObjectPool`, `StringBuilderPool`, ID generators (`ObjectId`, `SnowflakeId`, `UlidGenerator`, `GuidGenerator`, `ShortUniqueId`, `RandomId`), argument/result assertions, and more.

# Getting Started

To get started with Euonia.Core, follow these steps:

1. **Install the Package**: Add the Euonia.Core package to your project via NuGet.
   ```bash
   dotnet add package Euonia.Core
   ```

2. **Use the Library**: The library is designed to be used directly. For example, generating a time-ordered unique identifier:
   ```csharp
   using Nerosoft.Euonia.Core;

   var snowflakeId = SnowflakeId.Create();
   var objectId = ObjectId.NewObjectId();
   ```

3. **Explore the Utilities**: Use the built-in helpers in your code. For example, guarding invalid arguments:
   ```csharp
   Check.ArgumentNotNull(customer, nameof(customer));
   ```

4. **Handle Exceptions**: Rely on the built-in HTTP-aware exceptions in service code:
   ```csharp
   throw new NotFoundException("Customer not found.");
   ```

5. **Run Your Application**: Build and run your application—Euonia.Core is now available to all layers of your solution.

# Documentation

For more detailed information and advanced usage, refer to the official documentation on the [Euonia GitHub repository](https://github.com/NerooftDev/Euonia).