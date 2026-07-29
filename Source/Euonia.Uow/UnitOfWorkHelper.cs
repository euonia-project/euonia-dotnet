using System.Reflection;

namespace Nerosoft.Euonia.Uow;

internal static class UnitOfWorkHelper
{
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

	private static bool AnyMethodHasUnitOfWorkAttribute(Type implementationType)
	{
		return implementationType
		       .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
		       .Any(HasUnitOfWorkAttribute);
	}

	private static bool HasUnitOfWorkAttribute(MemberInfo methodInfo)
	{
		return methodInfo.IsDefined(typeof(UnitOfWorkAttribute), true);
	}
}