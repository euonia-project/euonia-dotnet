using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块加载器。
/// </summary>
public class ModuleLoader : IModuleLoader
{
	/// <inheritdoc />
	public IModuleDescriptor[] LoadModules(IServiceCollection services, Type startupModuleType)
	{
		var modules = GetDescriptors(services, startupModuleType);

		modules = SortByDependency(modules, startupModuleType);

		return modules.ToArray();
	}

	private List<IModuleDescriptor> GetDescriptors(IServiceCollection services, Type startupModuleType)
	{
		var modules = new List<ModuleDescriptor>();

		FillModules(modules, services, startupModuleType);
		SetDependencies(modules);

		return modules.Cast<IModuleDescriptor>().ToList();
	}

	/// <summary>
	/// 填充模块。
	/// </summary>
	/// <param name="modules">用于接收模块描述符的集合。</param>
	/// <param name="services">用于注册模块实例的服务集合。</param>
	/// <param name="startupModuleType">启动模块类型，其自身及递归发现的所有依赖模块都会被加入集合。</param>
	protected virtual void FillModules(List<ModuleDescriptor> modules, IServiceCollection services, Type startupModuleType)
	{
		foreach (var moduleType in ModuleHelper.FindAllModuleTypes(startupModuleType))
		{
			modules.Add(CreateModuleDescriptor(services, moduleType));
		}
	}

	/// <summary>
	/// 设置依赖项。
	/// </summary>
	/// <param name="modules">已收集的模块描述符集合，其中每一项都会解析并关联其依赖项。</param>
	protected virtual void SetDependencies(List<ModuleDescriptor> modules)
	{
		foreach (var module in modules)
		{
			SetDependencies(modules, module);
		}
	}

	/// <summary>
	/// 按依赖关系对模块排序。
	/// </summary>
	/// <param name="modules">待排序的模块描述符集合。</param>
	/// <param name="startupModuleType">启动模块类型，排序后会被移动到结果末尾。</param>
	/// <returns>按依赖顺序排列的模块描述符列表，被依赖的模块排在依赖它的模块之前，启动模块位于最后。</returns>
	/// <exception cref="ArgumentException">模块之间存在循环依赖时抛出。</exception>
	protected virtual List<IModuleDescriptor> SortByDependency(List<IModuleDescriptor> modules, Type startupModuleType)
	{
		var sortedModules = modules.SortByDependencies(m => m.Dependencies);
		sortedModules.MoveItem(m => m.Type == startupModuleType, modules.Count - 1);
		return sortedModules;
	}

	/// <summary>
	/// 创建模块描述符。
	/// </summary>
	/// <param name="services">用于注册模块实例的服务集合。</param>
	/// <param name="moduleType">要创建描述符的模块类型。</param>
	/// <returns>包含模块类型、所属程序集与模块上下文实例的描述符。</returns>
	protected virtual ModuleDescriptor CreateModuleDescriptor(IServiceCollection services, Type moduleType)
	{
		return new ModuleDescriptor(moduleType, CreateAndRegisterModule(services, moduleType));
	}

	/// <summary>
	/// 创建并注册模块。
	/// </summary>
	/// <param name="services">用于注册模块实例的服务集合。</param>
	/// <param name="moduleType">要实例化并注册的模块类型。</param>
	/// <returns>以 <paramref name="moduleType"/> 注册为单例的模块上下文实例。</returns>
	/// <exception cref="Exception">当 <paramref name="moduleType"/> 的实例创建结果为 <c>null</c> 时抛出。</exception>
	/// <remarks>模块类型须具有公共无参构造函数，否则实例创建过程会失败。</remarks>
	protected virtual IModuleContext CreateAndRegisterModule(IServiceCollection services, Type moduleType)
	{
		var module = (IModuleContext?)Activator.CreateInstance(moduleType);
		if (module == null)
		{
			throw new Exception(string.Format(Resources.IDS_MODULE_CREATE_FAILED, moduleType.AssemblyQualifiedName));
		}

		services.AddSingleton(moduleType, module);
		return module;
	}

	/// <summary>
	/// 设置模块的依赖项。
	/// </summary>
	/// <param name="modules">已收集的模块描述符集合，用于查找依赖模块。</param>
	/// <param name="module">需要解析并关联依赖项的模块描述符。</param>
	/// <exception cref="Exception">当 <paramref name="module"/> 声明的依赖模块不在 <paramref name="modules"/> 中时抛出。</exception>
	protected virtual void SetDependencies(List<ModuleDescriptor> modules, ModuleDescriptor module)
	{
		foreach (var dependedModuleType in ModuleHelper.FindDependedModuleTypes(module.Type))
		{
			var dependedModule = modules.FirstOrDefault(m => m.Type == dependedModuleType);
			if (dependedModule == null)
			{
				throw new Exception(string.Format(Resources.IDS_DEPENDED_MODULE_NOT_FOUND, dependedModuleType.AssemblyQualifiedName, module.Type.AssemblyQualifiedName));
			}

			module.AddDependency(dependedModule);
		}
	}
}