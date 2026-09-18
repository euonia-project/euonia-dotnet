using Microsoft.EntityFrameworkCore;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 定义使用连接字符串配置 <see cref="DbContextOptionsBuilder"/> 的委托。
/// </summary>
/// <param name="builder">要配置的 <see cref="DbContextOptionsBuilder"/>。</param>
/// <param name="connectionString">连接字符串（已去除提供程序前缀部分）。</param>
/// <returns>配置完成后的 <see cref="DbContextOptionsBuilder"/>。</returns>
public delegate DbContextOptionsBuilder ConnectionConfigurator(DbContextOptionsBuilder builder, string connectionString);