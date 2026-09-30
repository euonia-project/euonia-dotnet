using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// The module loader.
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
	/// Fills the modules.
	/// </summary>
	/// <param name="modules"></param>
	/// <param name="services"></param>
	/// <param name="startupModuleType"></param>
	protected virtual void FillModules(List<ModuleDescriptor> modules, IServiceCollection services, Type startupModuleType)
	{
		foreach (var moduleType in ModuleHelper.FindAllModuleTypes(startupModuleType))
		{
			modules.Add(CreateModuleDescriptor(services, moduleType));
		}
	}

	/// <summary>
	/// Sets the dependencies.
	/// </summary>
	/// <param name="modules"></param>
	protected virtual void SetDependencies(List<ModuleDescriptor> modules)
	{
		foreach (var module in modules)
		{
			SetDependencies(modules, module);
		}
	}

	/// <summary>
	/// Sorts the modules by dependencies.
	/// </summary>
	/// <param name="modules"></param>
	/// <param name="startupModuleType"></param>
	/// <returns></returns>
	protected virtual List<IModuleDescriptor> SortByDependency(List<IModuleDescriptor> modules, Type startupModuleType)
	{
		var sortedModules = modules.SortByDependencies(m => m.Dependencies);
		sortedModules.MoveItem(m => m.Type == startupModuleType, modules.Count - 1);
		return sortedModules;
	}

	/// <summary>
	/// Creates a module descriptor.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="moduleType"></param>
	/// <returns></returns>
	protected virtual ModuleDescriptor CreateModuleDescriptor(IServiceCollection services, Type moduleType)
	{
		return new ModuleDescriptor(moduleType, CreateAndRegisterModule(services, moduleType));
	}

	/// <summary>
	/// Creates and registers a module.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="moduleType"></param>
	/// <returns></returns>
	protected virtual IModuleContext CreateAndRegisterModule(IServiceCollection services, Type moduleType)
	{
		var module = (IModuleContext)Activator.CreateInstance(moduleType);
		if (module == null)
		{
			throw new Exception(string.Format(Resources.IDS_MODULE_CREATE_FAILED, moduleType.AssemblyQualifiedName));
		}

		services.AddSingleton(moduleType, module);
		return module;
	}

	/// <summary>
	/// Sets the dependencies of a module.
	/// </summary>
	/// <param name="modules"></param>
	/// <param name="module"></param>
	/// <exception cref="Exception"></exception>
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