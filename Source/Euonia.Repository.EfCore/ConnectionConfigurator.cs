using Microsoft.EntityFrameworkCore;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// The delegate to configure the <see cref="DbContextOptionsBuilder"/> with connection string.
/// </summary>
public delegate DbContextOptionsBuilder ConnectionConfigurator(DbContextOptionsBuilder builder, string connectionString);