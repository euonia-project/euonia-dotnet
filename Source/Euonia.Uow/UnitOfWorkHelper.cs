using System.Reflection;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 提供工作单元相关判断的辅助方法，用于识别类型与方法的 <see cref="UnitOfWorkAttribute"/> 配置。
/// </summary>
internal static class UnitOfWorkHelper
{
	/// <summary>
	/// 判断指定的实现类型是否应参与工作单元。
	/// </summary>
	/// <param name="implementationType">要判断的实现类型。</param>
	/// <returns>
	/// 若该类型标注了 <see cref="UnitOfWorkAttribute"/>、其任一方法标注了该特性，
	/// 或该类型实现了 <see cref="IUnitOfWorkEnabled"/>，则返回 <c>true</c>。
	/// </returns>
	public static bool IsUnitOfWorkType(Type implementationType)
	{
		//Explicitly defined UnitOfWorkAttribute
		if (HasUnitOfWorkAttribute(implementationType) || AnyMethodHasUnitOfWorkAttribute(implementationType))
		{
			return true;
		}

		//Conventional classes
		if (typeof(IUnitOfWorkEnabled).GetTypeInfo().IsAssignableFrom(implementationType))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// 判断指定的方法是否应通过工作单元执行，并输出其工作单元配置。
	/// </summary>
	/// <param name="methodInfo">要判断的方法。</param>
	/// <param name="unitOfWorkAttribute">
	/// 输出参数：方法或声明类型上的 <see cref="UnitOfWorkAttribute"/>；
	/// 若通过 <see cref="IUnitOfWorkEnabled"/> 约定识别则为 <c>null</c>。
	/// </param>
	/// <returns>若该方法应通过工作单元执行则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="methodInfo"/> 为 <c>null</c> 时抛出。</exception>
	/// <remarks>
	/// 判断顺序为：方法上的 <see cref="UnitOfWorkAttribute"/> → 声明类型上的 <see cref="UnitOfWorkAttribute"/> →
	/// 声明类型是否实现 <see cref="IUnitOfWorkEnabled"/>。目标特性将 <see cref="UnitOfWorkAttribute.IsDisabled"/> 设为 <c>true</c> 时返回 <c>false</c>。
	/// </remarks>
	public static bool IsUnitOfWorkMethod(MethodInfo methodInfo, out UnitOfWorkAttribute unitOfWorkAttribute)
	{
		ArgumentNullException.ThrowIfNull(methodInfo);

		//Method declaration
		var attrs = methodInfo.GetCustomAttributes(true).OfType<UnitOfWorkAttribute>().ToArray();
		if (attrs.Any())
		{
			unitOfWorkAttribute = attrs.First();
			return !unitOfWorkAttribute.IsDisabled;
		}

		if (methodInfo.DeclaringType != null)
		{
			//Class declaration
			attrs = methodInfo.DeclaringType.GetTypeInfo().GetCustomAttributes(true).OfType<UnitOfWorkAttribute>().ToArray();
			if (attrs.Any())
			{
				unitOfWorkAttribute = attrs.First();
				return !unitOfWorkAttribute.IsDisabled;
			}

			//Conventional classes
			if (typeof(IUnitOfWorkEnabled).GetTypeInfo().IsAssignableFrom(methodInfo.DeclaringType))
			{
				unitOfWorkAttribute = null;
				return true;
			}
		}

		unitOfWorkAttribute = null;
		return false;
	}

	/// <summary>
	/// 获取指定方法或声明类型上的工作单元配置。
	/// </summary>
	/// <param name="methodInfo">要查询的方法。</param>
	/// <returns>方法上的 <see cref="UnitOfWorkAttribute"/>；若方法上未标注则回退到声明类型上的特性；均未标注时返回 <c>null</c>。</returns>
	public static UnitOfWorkAttribute GetUnitOfWorkAttribute(MethodInfo methodInfo)
	{
		var attrs = methodInfo.GetCustomAttributes(true).OfType<UnitOfWorkAttribute>().ToArray();
		if (attrs.Length > 0)
		{
			return attrs[0];
		}

		if (methodInfo.DeclaringType != null)
		{
			attrs = methodInfo.DeclaringType.GetTypeInfo().GetCustomAttributes(true).OfType<UnitOfWorkAttribute>().ToArray();
			if (attrs.Length > 0)
			{
				return attrs[0];
			}
		}

		return null;
	}

	/// <summary>
	/// 判断指定类型的任一实例方法（含公开与非公开）是否标注了 <see cref="UnitOfWorkAttribute"/>。
	/// </summary>
	/// <param name="implementationType">要检查的类型。</param>
	/// <returns>若存在标注该特性的方法则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	private static bool AnyMethodHasUnitOfWorkAttribute(Type implementationType)
	{
		return implementationType
		       .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
		       .Any(HasUnitOfWorkAttribute);
	}

	/// <summary>
	/// 判断指定成员（含继承链）是否定义了 <see cref="UnitOfWorkAttribute"/>。
	/// </summary>
	/// <param name="methodInfo">要检查的成员。</param>
	/// <returns>若定义该特性则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
	private static bool HasUnitOfWorkAttribute(MemberInfo methodInfo)
	{
		return methodInfo.IsDefined(typeof(UnitOfWorkAttribute), true);
	}
}