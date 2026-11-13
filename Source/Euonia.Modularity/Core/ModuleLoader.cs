using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 模块加载器。
/// </summary>
public class ModuleLoader : IModuleLoader
{
    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">当 <paramref name="services"/> 或 <paramref name="startupModuleType"/> 为 <see langword="null"/> 时抛出。</exception>
    /// <exception cref="ArgumentException">当 <paramref name="startupModuleType"/> 不是有效的模块类型，或模块依赖关系无效时抛出。</exception>
    public IModuleDescriptor[] LoadModules(IServiceCollection services, Type startupModuleType)
    {
        return LoadModules(services, startupModuleType, null);
    }

    /// <inheritdoc />
    public IModuleDescriptor[] LoadModules(IServiceCollection services, Type startupModuleType, IEnumerable<Type>? additionalModuleTypes)
    {
        Check.EnsureNotNull(services, nameof(services));
        ModuleHelper.CheckModuleType(startupModuleType);

        var additionalTypes = additionalModuleTypes?
                              .Where(type => type != null)
                              .Distinct()
                              .ToArray();

        var modules = GetDescriptors(services, startupModuleType, additionalTypes);
        var sortedModules = SortByDependency(modules, startupModuleType);

        return sortedModules.ToArray();
    }

    private List<ModuleDescriptor> GetDescriptors(IServiceCollection services, Type startupModuleType, Type[]? additionalModuleTypes)
    {
        var modules = new List<ModuleDescriptor>();

        if (additionalModuleTypes is { Length: > 0 })
        {
            FillModules(modules, services, startupModuleType, additionalModuleTypes);
        }
        else
        {
            FillModules(modules, services, startupModuleType);
        }

        SetDependencies(modules);

        return modules;
    }

    /// <summary>
    /// 填充模块。
    /// </summary>
    /// <param name="modules">用于接收模块描述符的集合。</param>
    /// <param name="services">用于注册模块实例的服务集合。</param>
    /// <param name="startupModuleType">启动模块类型，其自身及递归发现的所有依赖模块都会被加入集合。</param>
    protected virtual void FillModules(List<ModuleDescriptor> modules, IServiceCollection services, Type startupModuleType)
    {
        FillModules(modules, services, startupModuleType, Array.Empty<Type>());
    }

    /// <summary>
    /// 填充启动模块依赖图以及额外来源提供的模块。
    /// </summary>
    /// <param name="modules">用于接收模块描述符的集合。</param>
    /// <param name="services">用于注册模块实例的服务集合。</param>
    /// <param name="startupModuleType">启动模块类型。</param>
    /// <param name="additionalModuleTypes">额外模块类型。</param>
    protected virtual void FillModules(List<ModuleDescriptor> modules, IServiceCollection services, Type startupModuleType, IEnumerable<Type> additionalModuleTypes)
    {
        var moduleTypes = new List<Type>();
        var addedTypes = new HashSet<Type>();

        foreach (var moduleType in ModuleHelper.FindAllModuleTypes(startupModuleType))
        {
            if (addedTypes.Add(moduleType))
            {
                moduleTypes.Add(moduleType);
            }
        }

        foreach (var moduleType in additionalModuleTypes)
        {
            if (moduleType == null)
            {
                continue;
            }

            foreach (var dependencyType in ModuleHelper.FindAllModuleTypes(moduleType))
            {
                if (addedTypes.Add(dependencyType))
                {
                    moduleTypes.Add(dependencyType);
                }
            }
        }

        foreach (var moduleType in moduleTypes)
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
        var moduleMap = modules.ToDictionary(module => module.Type);

        foreach (var module in modules)
        {
            SetDependencies(moduleMap, module);
        }
    }

    /// <summary>
    /// 按依赖关系对模块排序。
    /// </summary>
    /// <param name="modules">待排序的模块描述符集合。</param>
    /// <param name="startupModuleType">启动模块类型，通常在排序后位于结果末尾；若其他模块依赖启动模块，则按实际依赖顺序排列。</param>
    /// <returns>按依赖顺序排列的模块描述符列表，被依赖的模块排在依赖它的模块之前。</returns>
    /// <exception cref="ArgumentException">模块之间存在循环依赖时抛出。</exception>
    protected virtual List<IModuleDescriptor> SortByDependency(IReadOnlyList<IModuleDescriptor> modules, Type startupModuleType)
    {
        var sortedModules = modules.SortByDependencies(module => module.Dependencies);
        var startupModule = sortedModules.FirstOrDefault(module => module.Type == startupModuleType);

        if (startupModule != null && !sortedModules.Any(module => module != startupModule && module.Dependencies.Contains(startupModule)))
        {
            sortedModules.MoveItem(module => module.Type == startupModuleType, sortedModules.Count - 1);
        }

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
    /// <exception cref="Exception">当 <paramref name="moduleType"/> 的实例创建失败时抛出。</exception>
    /// <remarks>
    /// 模块类型须具有公共无参构造函数，否则实例创建过程会失败。
    /// 如果服务集合中已经注册了该模块类型的实例，则复用该实例，便于宿主替换或定制模块。
    /// </remarks>
    protected virtual IModuleContext CreateAndRegisterModule(IServiceCollection services, Type moduleType)
    {
        var registeredModule = services
            .FirstOrDefault(descriptor => descriptor.ServiceType == moduleType)
            ?.ImplementationInstance as IModuleContext;

        if (registeredModule != null)
        {
            return registeredModule;
        }

        IModuleContext? module;

        try
        {
            module = Activator.CreateInstance(moduleType) as IModuleContext;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(string.Format(Resources.IDS_MODULE_CREATE_FAILED, moduleType.AssemblyQualifiedName), exception);
        }

        if (module == null)
        {
            throw new InvalidOperationException(string.Format(Resources.IDS_MODULE_CREATE_FAILED, moduleType.AssemblyQualifiedName));
        }

        services.AddSingleton(moduleType, module);
        return module;
    }

    /// <summary>
    /// 设置模块的依赖项。
    /// </summary>
    /// <param name="modules">已收集的模块描述符字典，用于查找依赖模块。</param>
    /// <param name="module">需要解析并关联依赖项的模块描述符。</param>
    /// <exception cref="Exception">当 <paramref name="module"/> 声明的依赖模块不在 <paramref name="modules"/> 中时抛出。</exception>
    protected virtual void SetDependencies(IReadOnlyDictionary<Type, ModuleDescriptor> modules, ModuleDescriptor module)
    {
        foreach (var dependedModuleType in ModuleHelper.FindDependedModuleTypes(module.Type))
        {
            if (!modules.TryGetValue(dependedModuleType, out var dependedModule))
            {
                throw new InvalidOperationException(string.Format(Resources.IDS_DEPENDED_MODULE_NOT_FOUND, dependedModuleType.AssemblyQualifiedName, module.Type.AssemblyQualifiedName));
            }

            module.AddDependency(dependedModule);
        }
    }
}
