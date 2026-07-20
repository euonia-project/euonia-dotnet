using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Nerosoft.Euonia.Reflection;

/// <summary>
/// 提供加载程序集和类型的静态方法。
/// </summary>
public static class AssemblyHelper
{
	private static readonly ConcurrentDictionary<Assembly, IReadOnlyList<Type>> _typeCache = new();
	private static readonly ConcurrentDictionary<Assembly, IReadOnlyList<TypeInfo>> _definedTypeCache = new();

	/// <summary>
	/// 从 <paramref name="directory"/> 目录中的可执行文件加载程序集。
	/// </summary>
	/// <param name="directory">目录路径。</param>
	/// <param name="searchOption">指定搜索操作是仅包含当前目录还是包含所有子目录。</param>
	/// <returns>找到的程序集列表。</returns>
	public static List<Assembly> LoadAssemblies(string directory, SearchOption searchOption)
	{
		return GetAssemblyFiles(directory, searchOption).Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
		                                                .ToList();
	}

	/// <summary>
	/// 获取 <paramref name="directory"/> 目录中的程序集文件。
	/// </summary>
	/// <param name="directory">目录路径。</param>
	/// <param name="searchOption">指定搜索操作是仅包含当前目录还是包含所有子目录。</param>
	/// <returns>找到的程序集文件路径集合。</returns>
	public static IEnumerable<string> GetAssemblyFiles(string directory, SearchOption searchOption)
	{
		return Directory.EnumerateFiles(directory, "*.*", searchOption)
		                .Where(s => s.EndsWith(".dll") || s.EndsWith(".exe"));
	}

	/// <summary>
	/// 获取 <paramref name="assembly"/> 中的所有类型。
	/// </summary>
	/// <param name="assembly">要获取类型的程序集。</param>
	/// <returns>在程序集中找到的类型列表。</returns>
	public static IReadOnlyList<Type> GetAllTypes(Assembly assembly)
	{
		try
		{
			return _typeCache.GetOrAdd(assembly, assembly.GetTypes());
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types;
		}
	}

	/*
	public static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return _loadableTypeCache.GetOrAdd(assembly, assembly.GetTypes());
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where(t => t != null).ToList();
		}
	}

	public static IEnumerable<Type> GetTypesImplementingInterface(Assembly assembly, Type interfaceType)
	{
		var types = GetLoadableTypes(assembly);
		return types.Where(t => t.GetInterfaces().Contains(interfaceType));
	}
	*/

	/// <summary>
	/// 获取 <paramref name="assembly"/> 中所有已定义的类型。
	/// </summary>
	/// <param name="assembly">要获取已定义类型的程序集。</param>
	/// <returns>在程序集中找到的 <see cref="TypeInfo"/> 列表。</returns>
	public static IReadOnlyList<TypeInfo> GetDefinedTypes(Assembly assembly)
	{
		return _definedTypeCache.GetOrAdd(assembly, assembly.DefinedTypes.ToList);
	}
}