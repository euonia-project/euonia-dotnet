using System.Reflection;
using AutoMapper;
using Nerosoft.Euonia.Collections;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// AutoMapper 的选项配置。
/// </summary>
/// <remarks>
/// 通过 <see cref="AddMaps"/>、<see cref="AddProfile(Type, bool)"/>、<see cref="Configure"/> 等
/// 方法累积映射配置器与待校验的 Profile 类型，在创建 <c>Mapper</c> 时统一应用。
/// </remarks>
public class AutomapperOptions
{
	/// <summary>
	/// 获取已注册的配置器委托列表。
	/// </summary>
	/// <remarks>这些委托在构建 <c>MapperConfiguration</c> 时依次执行，用于向配置表达式添加映射。</remarks>
	internal List<Action<IServiceProvider, MapperConfigurationExpression>> Configurators { get; } = new();

	/// <summary>
	/// 获取需要校验的 Profile 类型列表。
	/// </summary>
	/// <remarks>其中每个 Profile 都会被实例化并对其配置执行有效性断言（<c>AssertConfigurationIsValid</c>）。</remarks>
	internal ITypeList<Profile> ValidatingProfiles { get; } = new TypeList<Profile>();

	/// <summary>
	/// 注册指定程序集中的所有映射配置（Profile）。
	/// </summary>
	/// <param name="assembly">要从中加载映射配置的程序集。</param>
	/// <param name="validate">是否将程序集中找到的 Profile 类型加入校验列表。</param>
	public void AddMaps(Assembly assembly, bool validate = false)
	{
		Configurators.Add((_, expression) =>
		{
			expression.AddMaps(assembly);
		});

		if (validate)
		{
			var profileTypes = assembly
			                   .DefinedTypes
			                   .Where(type => typeof(Profile).IsAssignableFrom(type) && !type.IsAbstract && !type.IsGenericType);

			foreach (var profileType in profileTypes)
			{
				ValidatingProfiles.AddIfNotContains(profileType);
			}
		}
	}

	/// <summary>
	/// 注册指定类型的 AutoMapper Profile。
	/// </summary>
	/// <param name="validate">是否将该 Profile 类型加入校验列表。</param>
	/// <typeparam name="TProfile">要注册的 Profile 类型。</typeparam>
	public void AddProfile<TProfile>(bool validate = false)
		where TProfile : Profile, new()
	{
		Configurators.Add((_, expression) =>
		{
			expression.AddProfile<TProfile>();
		});
		if (validate)
		{
			ValidatingProfiles.AddIfNotContains(typeof(TProfile));
		}
	}

	/// <summary>
	/// 注册指定的 AutoMapper Profile 实例。
	/// </summary>
	/// <typeparam name="TProfile">要注册的 Profile 类型。</typeparam>
	/// <param name="profile">要注册的 Profile 实例。</param>
	/// <param name="validate">是否将该 Profile 类型加入校验列表。</param>
	public void AddProfile<TProfile>(TProfile profile, bool validate = false)
		where TProfile : Profile
	{
		Configurators.Add((_, expression) =>
		{
			expression.AddProfile(profile);
		});
		if (validate)
		{
			ValidatingProfiles.AddIfNotContains(profile.GetType());
		}
	}

	/// <summary>
	/// 注册指定类型的 AutoMapper Profile。
	/// </summary>
	/// <param name="profileType">AutoMapper Profile 类型。</param>
	/// <param name="validate">是否将该 Profile 类型加入校验列表。</param>
	// ReSharper disable once MemberCanBePrivate.Global
	public void AddProfile(Type profileType, bool validate = false)
	{
		Configurators.Add((_, expression) =>
		{
			expression.AddProfile(profileType);
		});

		if (validate)
		{
			ValidatingProfiles.AddIfNotContains(profileType);
		}
	}

	/// <summary>
	/// 注册多个 AutoMapper Profile。
	/// </summary>
	/// <param name="profileTypes">要注册的 Profile 类型集合。</param>
	/// <param name="validate">是否将其中非空的类型加入校验列表。</param>
	/// <remarks>若集合为 <see langword="null"/>、为空或全部为 <see langword="null"/>，则直接返回，不注册任何配置；集合中的空项会被跳过。</remarks>
	public void AddProfile(ICollection<Type> profileTypes, bool validate = false)
	{
		if (profileTypes == null || !profileTypes.Any() || profileTypes.All(t => t == null))
		{
			return;
		}

		Configurators.Add((_, expression) =>
		{
			foreach (var profileType in profileTypes)
			{
				if (profileType == null)
				{
					continue;
				}

				expression.AddProfile(profileType);
			}
		});

		if (validate)
		{
			foreach (var profileType in profileTypes)
			{
				if (profileType == null)
				{
					continue;
				}

				ValidatingProfiles.AddIfNotContains(profileType);
			}
		}

		{
			// prevent code check
		}
	}

	/// <summary>
	/// 注册自定义的 AutoMapper 配置委托。
	/// </summary>
	/// <param name="config">用于配置 <c>MapperConfigurationExpression</c> 的委托。</param>
	public void Configure(Action<IServiceProvider, MapperConfigurationExpression> config)
	{
		Configurators.Add(config);
	}
}