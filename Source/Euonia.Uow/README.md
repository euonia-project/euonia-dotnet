# Overview

Euonia.Uow is a .NET library that provides a Unit of Work implementation to manage database transactions and ensure data consistency across multiple operations. It simplifies the process of handling database interactions by encapsulating them within a single transactional context.

# Features

- **Transaction Management**: Automatically manages database transactions, ensuring that all operations within a unit of work are committed or rolled back as a single unit.
- **Repository Integration**: Works seamlessly with repository patterns to provide a clean and organized way to interact with the database.
- **Support for Multiple Databases**: Compatible with various database providers, allowing developers to use their preferred database technology.
- **Easy to Use**: Simple API for starting, committing, and rolling back transactions.
- **Open Source**: Actively developed and maintained as an open-source project.

# Getting Started
To get started with Euonia.Uow, follow these steps:
1. **Install the Package**: Add the Euonia.Uow package to your project via NuGet.
   ```bash
   dotnet add package Euonia.Uow
   ```
2. **Register Services**: In your application's service registration, add the necessary services for the Unit of Work.
   ```csharp
   service.AddUnitOfWork();
   ```
   or
   ```csharp
   [DependsOn(typeof(UnitOfWorkModule))]
   public class YourModule : ModuleContextBase
   {
   }
   ```
3. **Configure the Unit of Work**: Set up the Unit of Work in your application's configuration file (e.g., appsettings.json) if needed.
4. **Implement Repositories**: Create repository classes that utilize the Unit of Work for database operations.
5. **Use the Unit of Work**: In your application logic, use the Unit of Work to manage transactions.
   ```csharp
   using (var uow = UnitOfWorkManager.Begin())
   {
         // Perform database operations using repositories
         uow.CompleteAsync();
   }
   ```
6. **Run Your Application**: Start your application, and it will now use Euonia.Uow for managing database transactions.

# Documentation
For more detailed information and advanced usage, refer to the official documentation on the [Euonia GitHub repository](https://github.com/NerooftDev/Euonia).