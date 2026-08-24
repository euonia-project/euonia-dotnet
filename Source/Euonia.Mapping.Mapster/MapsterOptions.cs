using System.Reflection;
using Mapster;
using Nerosoft.Euonia.Collections;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// Mapster 的选项配置。
/// </summary>
/// <remarks>
/// 通过 <see cref="AddProfile{TRegister}()"/>、<see cref="AddProfiles(Assembly[])"/> 等方法
/// 累积要应用的映射注册（IRegister）与配置委托，在注册映射服务时统一应用到 <c>TypeAdapterConfig.GlobalSettings</c>。
/// </remarks>
public class MapsterOptions
{
	/// <summary>
	/// 获取配置委托列表。
	/// </summary>
	/// <remarks>这些委托在注册服务时依次执行，用于向全局配置应用扫描到的映射等配置。</remarks>
	internal List<Action<TypeAdapterConfig>> Configuration { get; } = new();

	/// <summary>
	/// 获取已注册的 Profile 映射（按注册类型索引）。
	/// </summary>
	/// <remarks>值为 <see langword="null"/> 表示该类型尚未实例化，注册服务时会通过服务提供程序创建实例。</remarks>
	internal Dictionary<Type, IRegister> Profiles { get; } = new();

	/// <summary>
	/// 将指定类型的映射注册（Profile）添加到配置中。
	/// </summary>
	/// <typeparam name="TRegister">要注册的 <see cref="IRegister"/> 类型。</typeparam>
	public void AddProfile<TRegister>()
		where TRegister : IRegister
	{
		Profiles.TryAdd(typeof(TRegister), null);
	}

	/// <summary>
	/// 将指定类型的映射注册（Profile）添加到配置中。
	/// </summary>
	/// <param name="factory">用于创建 <typeparamref name="TRegister"/> 实例的工厂委托。</param>
	/// <typeparam name="TRegister">要注册的 <see cref="IRegister"/> 类型。</typeparam>
	public void AddProfile<TRegister>(Func<TRegister> factory)
		where TRegister : IRegister
	{
		Profiles.TryAdd(typeof(TRegister), factory());
	}

	/// <summary>
	/// 将指定类型的映射注册（Profile）添加到配置中。
	/// </summary>
	/// <param name="registerType">要注册的 <see cref="IRegister"/> 类型。</param>
	/// <exception cref="ArgumentException"><paramref name="registerType"/> 不可赋值给 <see cref="IRegister"/>。</exception>
	public void AddProfile(Type registerType)
	{
		if (!typeof(IRegister).IsAssignableFrom(registerType))
		{
			throw new ArgumentException($"The register type '{registerType!.FullName}' must be assignable from IRegister");
		}

		Profiles.TryAdd(registerType, (IRegister)Activator.CreateInstance(registerType));
	}

	/// <summary>
	/// 将指定的映射注册（Profile）实例应用到配置。
	/// </summary>
	/// <param name="register">要应用的 <see cref="IRegister"/> 实例。</param>
	public void AddProfile(IRegister register)
	{
		Profiles.TryAdd(register.GetType(), register);
	}

	/// <summary>
	/// 将多个映射注册（Profile）实例应用到配置。
	/// </summary>
	/// <param name="registers">要应用的 <see cref="IRegister"/> 实例集合。</param>
	public void AddProfiles(IEnumerable<IRegister> registers)
	{
		foreach (var register in registers)
		{
			Profiles.TryAdd(register.GetType(), register);
		}
	}

	/// <summary>
	/// 扫描指定程序集并将其中发现的映射注册（Profile）应用到配置。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	public void AddProfiles(params Assembly[] assemblies)
	{
		Configuration.Add(config => config.Scan(assemblies));
	}
}