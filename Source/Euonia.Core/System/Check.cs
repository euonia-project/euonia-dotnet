using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;

/// <summary>
/// 断言工具类，提供验证条件并在断言失败时抛出异常的方法。
/// </summary>
[DebuggerStepThrough]
public static class Check
{
	/// <summary>
	/// 断言给定的函数表达式计算结果为 true。如果为 false，则抛出带有指定消息的 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="condition">要评估的条件函数。</param>
	/// <param name="message">断言失败时的错误消息。</param>
	/// <param name="args">消息格式化参数。</param>
	/// <returns>始终返回 true。</returns>
	/// <exception cref="InvalidOperationException">当条件为 false 时抛出。</exception>
	public static bool Ensure(Func<bool> condition, string message, params object[] args)
	{
		if (!condition())
		{
			throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, message, args));
		}

		return true;
	}

	/// <summary>
	/// 断言给定的表达式计算结果为 true。如果为 false，则抛出带有指定消息的 <see cref="InvalidOperationException"/>。
	/// </summary>
	/// <param name="condition">要评估的条件。</param>
	/// <param name="message">断言失败时的错误消息。</param>
	/// <param name="args">消息格式化参数。</param>
	/// <returns>始终返回 true。</returns>
	/// <exception cref="InvalidOperationException">当条件为 false 时抛出。</exception>
	public static bool Ensure(bool condition, string message, params object[] args)
	{
		if (!condition)
		{
			throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, message, args));
		}

		return true;
	}

	/// <summary>
	/// 确保给定值满足条件。
	/// </summary>
	/// <typeparam name="T">值的类型。</typeparam>
	/// <param name="value">要检查的值。</param>
	/// <param name="action">用于验证值的条件函数。</param>
	/// <param name="message">验证失败时的错误消息。</param>
	/// <returns>验证通过的值。</returns>
	public static T Ensure<T>(T value, [NotNull] Func<T, bool> action, string message)
	{
		var result = action(value);
		if (result)
		{
			return value;
		}

		throw new ArgumentException(message, nameof(value));
	}

	/// <summary>
	/// 确保给定值满足条件，失败时执行回调。
	/// </summary>
	/// <typeparam name="T">值的类型。</typeparam>
	/// <param name="value">要检查的值。</param>
	/// <param name="action">用于验证值的条件函数。</param>
	/// <param name="failsAction">验证失败时执行的操作。</param>
	public static void Ensure<T>(T value, [NotNull] Func<T, bool> action, Action<T> failsAction)
	{
		var result = action(value);
		if (result)
		{
			return;
		}

		failsAction(value);
	}

	/// <summary>
	/// 确保给定值满足条件。
	/// </summary>
	/// <typeparam name="T">值的类型。</typeparam>
	/// <param name="value">要检查的值。</param>
	/// <param name="action">用于验证值的条件函数。</param>
	/// <returns>包含值和验证结果的 <see cref="CheckResult{T}"/>。</returns>
	public static CheckResult<T> Ensure<T>(T value, [NotNull] Func<T, bool> action)
	{
		var result = action(value);
		return new CheckResult<T>(value, result);
	}

	/// <summary>
	/// 确保给定值不为 null。
	/// </summary>
	/// <typeparam name="T">值的类型。</typeparam>
	/// <param name="value">要检查的值。</param>
	/// <param name="parameter">参数名称。</param>
	/// <returns>非 null 的值。</returns>
	/// <exception cref="ArgumentNullException">当值为 null 时抛出。</exception>
	public static T EnsureNotNull<T>(T value, [NotNull] string parameter)
	{
		if (value == null)
		{
			throw new ArgumentNullException(parameter);
		}

		return value;
	}

	/// <summary>
	/// 确保给定值不为 null。
	/// </summary>
	/// <typeparam name="T">值的类型。</typeparam>
	/// <param name="value">要检查的值。</param>
	/// <param name="parameter">参数名称。</param>
	/// <param name="message">错误消息。</param>
	/// <returns>非 null 的值。</returns>
	/// <exception cref="ArgumentNullException">当值为 null 时抛出。</exception>
	public static T EnsureNotNull<T>(T value, [NotNull] string parameter, string message)
	{
		if (value == null)
		{
			throw new ArgumentNullException(parameter, message);
		}

		return value;
	}

	/// <summary>
	/// 确保给定字符串值不为 null，并验证长度范围。
	/// </summary>
	/// <param name="value">要检查的字符串值。</param>
	/// <param name="parameter">参数名称。</param>
	/// <param name="maxLength">允许的最大长度。</param>
	/// <param name="minLength">允许的最小长度。</param>
	/// <returns>验证通过的字符串。</returns>
	/// <exception cref="ArgumentException">当值为 null 或长度不符合要求时抛出。</exception>
	public static string EnsureNotNull(string value, [NotNull] string parameter, int maxLength = int.MaxValue, int minLength = 0)
	{
		if (value == null)
		{
			throw new ArgumentException(string.Format(Resources.IDS_PARAMETER_CAN_NOT_BE_NULL, parameter), parameter);
		}

		if (value.Length > maxLength)
		{
			throw new ArgumentException(string.Format(Resources.IDS_PARAMETER_LENGTH_MUST_BE_EQUAL_OR_LOWER_THAN, parameter, maxLength), parameter);
		}

		if (minLength > 0 && value.Length < minLength)
		{
			throw new ArgumentException(string.Format(Resources.IDS_PARAMETER_LENGTH_MUST_BE_EQUAL_OR_GREATER_THAN, parameter, maxLength), parameter);
		}

		return value;
	}

	/// <summary>
	/// 确保给定值不为 null、空或仅由空白字符组成。
	/// </summary>
	/// <param name="value">要检查的字符串值。</param>
	/// <param name="parameter">参数名称。</param>
	/// <returns>验证通过的字符串。</returns>
	/// <exception cref="ArgumentException">当值为 null、空或仅由空白字符组成时抛出。</exception>
	public static string EnsureNotNullOrWhiteSpace(string value, [NotNull] string parameter)
	{
		if (value.IsNullOrWhiteSpace())
		{
			throw new ArgumentException(string.Format(Resources.IDS_PARAMETER_CANNOT_NULL_OR_WHITE_SPACE, parameter), parameter);
		}

		return value;
	}

	/// <summary>
	/// 确保指定字符串值不为 null、空或仅由空白字符组成，失败时执行回调。
	/// </summary>
	/// <param name="value">要检查的字符串值。</param>
	/// <param name="parameter">正在检查的参数名称。</param>
	/// <param name="failsAction">检查失败时执行的操作。</param>
	public static void EnsureNotNullOrWhiteSpace(string value, [NotNull] string parameter, Action<string> failsAction)
	{
		if (value.IsNullOrWhiteSpace())
		{
			failsAction(parameter);
		}
	}

	/// <summary>
	/// 确保指定字符串值不为 null 或空。
	/// </summary>
	/// <param name="value">要检查的值。</param>
	/// <param name="parameter">正在检查的参数名称。</param>
	/// <returns>如果非 null 或空，则返回传入的相同字符串值。</returns>
	/// <exception cref="ArgumentException">当字符串为 null 或空时抛出。</exception>
	public static string EnsureNotNullOrEmpty(string value, [NotNull] string parameter)
	{
		if (value.IsNullOrEmpty())
		{
			throw new ArgumentException(string.Format(Resources.IDS_PARAMETER_CANNOT_NULL_OR_EMPTY, parameter), parameter);
		}

		return value;
	}

	/// <summary>
	/// 检查输入字符串是否为 null 或空，如果是则调用失败回调。
	/// </summary>
	/// <param name="value">要检查的字符串值。</param>
	/// <param name="parameter">参数名称。</param>
	/// <param name="failsAction">检查失败时执行的回调。</param>
	public static void EnsureNotNullOrEmpty(string value, [NotNull] string parameter, Action<string> failsAction)
	{
		if (value.IsNullOrEmpty())
		{
			failsAction(parameter);
		}
	}

	/// <summary>
	/// 确保给定字符串值匹配正则表达式模式。
	/// </summary>
	/// <param name="value">输入值。</param>
	/// <param name="parameter">给定值的参数名称。</param>
	/// <param name="pattern">正则表达式模式。</param>
	/// <param name="options">正则表达式选项。</param>
	/// <returns>匹配的字符串值。</returns>
	/// <exception cref="ArgumentException">当值不匹配模式时抛出。</exception>
	public static string EnsureIsMatch(string value, [NotNull] string parameter, string pattern, RegexOptions options = RegexOptions.None)
	{
		if (!Regex.IsMatch(value, pattern, options))
		{
			throw new ArgumentException(string.Format(Resources.IDS_PARAMETER_IS_NOT_MATCH_WITH_PATTERN, parameter, pattern), parameter);
		}

		return value;
	}

	/// <summary>
	/// 确保给定字符串值匹配正则表达式模式，失败时执行回调。
	/// </summary>
	/// <param name="value">输入值。</param>
	/// <param name="parameter">给定值的参数名称。</param>
	/// <param name="pattern">正则表达式模式。</param>
	/// <param name="failsAction">给定值不匹配时执行的回调函数。</param>
	/// <param name="options">正则表达式选项。</param>
	public static void EnsureIsMatch(string value, [NotNull] string parameter, string pattern, Action<string> failsAction, RegexOptions options = RegexOptions.None)
	{
		if (!Regex.IsMatch(value, pattern, options))
		{
			failsAction(parameter);
		}
	}

	/// <summary>
	/// 确保集合不为 null 或空。
	/// </summary>
	/// <typeparam name="T">集合元素类型。</typeparam>
	/// <param name="collection">要检查的集合。</param>
	/// <param name="parameter">参数名称。</param>
	/// <returns>非 null 且非空的集合。</returns>
	/// <exception cref="ArgumentException">当集合为 null 或空时抛出。</exception>
	public static IEnumerable<T> EnsureNotNullOrEmpty<T>(IEnumerable<T> collection, [NotNull] string parameter)
	{
		if (collection.IsNullOrEmpty())
		{
			throw new ArgumentException(string.Format(Resources.IDS_PARAMETER_CANNOT_NULL_OR_EMPTY, parameter), parameter);
		}

		return collection;
	}

	/// <summary>
	/// 确保给定类型可赋值给指定的基类型。
	/// </summary>
	/// <typeparam name="TBaseType">给定类型应可赋值给的基类型。</typeparam>
	/// <param name="type">要检查是否可赋值给指定基类型的类型。</param>
	/// <param name="parameter">传递给方法的参数名称。</param>
	/// <returns>如果可赋值给 TBaseType，则返回原始输入类型；否则抛出 <see cref="ArgumentException"/>。</returns>
	/// <exception cref="ArgumentException">当类型不可赋值时抛出。</exception>
	public static Type EnsureAssignableTo<TBaseType>(Type type, [NotNull] string parameter)
	{
		EnsureNotNull(type, parameter);

		if (!type.IsAssignableTo<TBaseType>())
		{
			throw new ArgumentException($"{parameter} (type of {type.AssemblyQualifiedName}) should be assignable to the {typeof(TBaseType).GetFullNameWithAssemblyName()}!");
		}

		return type;
	}

	/// <summary>
	/// 确保给定输入字符串的长度在指定范围内。
	/// </summary>
	/// <param name="value">要检查的输入字符串。</param>
	/// <param name="parameter">与输入字符串对应的参数名称。</param>
	/// <param name="maxLength">输入字符串的最大长度。</param>
	/// <param name="minLength">输入字符串的最小长度，默认为 0。</param>
	/// <returns>输入字符串。</returns>
	/// <exception cref="ArgumentException">当长度不符合要求时抛出。</exception>
	public static string EnsureLengthInRange(string value, [NotNull] string parameter, int maxLength, int minLength = 0)
	{
		if (minLength > 0)
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException(string.Format(Resources.IDS_PARAMETER_CANNOT_NULL_OR_EMPTY, parameter), parameter);
			}

			if (value.Length < minLength)
			{
				throw new ArgumentException(string.Format(Resources.IDS_PARAMETER_LENGTH_MUST_BE_EQUAL_OR_GREATER_THAN, parameter, minLength), parameter);
			}
		}

		if (value != null && value.Length > maxLength)
		{
			throw new ArgumentException(string.Format(Resources.IDS_PARAMETER_LENGTH_MUST_BE_EQUAL_OR_LOWER_THAN, parameter, maxLength), parameter);
		}

		return value;
	}
}