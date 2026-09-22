using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 提供 <see cref="DbContext"/> 相关的命名约定与软删除查询过滤器扩展方法。
/// </summary>
public static class DataContextExtensions
{
    /// <summary>
    /// 获取指定实体 CLR 类型对应的数据库表名。
    /// </summary>
    /// <param name="type">实体的 CLR 类型。</param>
    /// <returns>按命名约定转换后的表名（蛇形小写命名）。</returns>
    public static string GetTableName(Type type)
    {
        return GetTableName(type.Name);
    }

    /// <summary>
    /// 获取指定实体名称对应的数据库表名。
    /// </summary>
    /// <param name="entityName">实体名称，通常为 Pascal 命名（如 <c>OrderDetail</c>）。</param>
    /// <returns>按命名约定转换后的表名，各单词以 <c>_</c> 连接并转为小写（如 <c>order_detail</c>）。</returns>
    public static string GetTableName(string entityName)
    {
        var words = Regex.Matches(entityName, @"((?:[A-Z])?[a-z0-9]+)").Select(t => t.Value.ToLower(CultureInfo.InvariantCulture));
        return string.Join("_", words);
    }

    /// <summary>
    /// 获取指定实体名称对应的外键属性名。
    /// </summary>
    /// <param name="entityName">实体名称。</param>
    /// <returns>外键属性名，格式为 <c>{实体名}Id</c>。</returns>
    public static string GetForeignKey(string entityName)
    {
        return $"{entityName}Id";
    }

    /// <summary>
    /// 为模型中所有实现了 <see cref="ITombstone"/> 的实体设置软删除全局查询过滤器。
    /// </summary>
    /// <param name="modelBuilder">要处理的模型构建器。</param>
    /// <remarks>
    /// 过滤器按 <c>!IsDeleted</c> 构建，即查询结果中将自动排除已逻辑删除的记录。
    /// </remarks>
    public static void SetTombstoneQueryFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            //other automated configurations left out
            if (typeof(ITombstone).IsAssignableFrom(entityType.ClrType))
            {
                entityType.SetTombstoneQueryFilter();
            }
        }
    }

    /// <summary>
    /// 为指定的可变实体类型设置软删除全局查询过滤器。
    /// </summary>
    /// <param name="entity">要设置过滤器的实体类型元数据。</param>
    /// <remarks>
    /// https://www.thereformedprogrammer.net/ef-core-in-depth-soft-deleting-data-with-global-query-filters/
    /// </remarks>
    public static void SetTombstoneQueryFilter(this IMutableEntityType entity)
    {
        var methodToCall = typeof(DataContextExtensions)
                           .GetMethod(nameof(GetTombstoneFilter), BindingFlags.NonPublic | BindingFlags.Static)
                           ?.MakeGenericMethod(entity.ClrType);
        if (methodToCall == null)
        {
            return;
        }

        var filter = methodToCall.Invoke(null, Array.Empty<object>());
        entity.SetQueryFilter((LambdaExpression)filter!);
    }

    private static LambdaExpression GetTombstoneFilter<TEntity>()
        where TEntity : class, ITombstone
    {
        Expression<Func<TEntity, bool>> filter = x => !x.IsDeleted;
        return filter;
    }
}