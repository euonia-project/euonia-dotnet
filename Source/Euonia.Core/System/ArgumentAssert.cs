using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// <see cref="System.ArgumentNullException"/> 的内部 polyfill 实现。
/// </summary>
public sealed class ArgumentAssert
{
	/// <summary>
	/// 如果 <paramref name="argument"/> 为 <see langword="null"/>，则抛出 <see cref="System.ArgumentNullException"/>。
	/// </summary>
	/// <param name="argument">要验证为非 <see langword="null"/> 的引用类型参数。</param>
	/// <param name="paramName"><paramref name="argument"/> 对应的参数名称。</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NET5_0_OR_GREATER
	public static void ThrowIfNull([NotNull] object argument, [CallerArgumentExpression(nameof(argument))] string paramName = null)
#else
	public static void ThrowIfNull([NotNull] object argument, string paramName = null)
#endif
	{
		if (argument is null)
		{
			Throw(paramName);
		}
	}

	/// <summary>
	/// 用于泛型值的特化版本。
	/// </summary>
	/// <typeparam name="T">要检查的值的类型。</typeparam>
	/// <remarks>
	/// 需要此类型是因为如果有一个带有泛型参数的泛型重载，所有调用都会被编译器绑定到该重载而非 <see cref="object"/> 重载。
	/// </remarks>
	public static class For<T>
	{
		/// <summary>
		/// 如果 <paramref name="argument"/> 为 <see langword="null"/>，则抛出 <see cref="System.ArgumentNullException"/>。
		/// </summary>
		/// <param name="argument">要验证为非 <see langword="null"/> 的引用类型参数。</param>
		/// <param name="paramName"><paramref name="argument"/> 对应的参数名称。</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NET5_0_OR_GREATER
		public static void ThrowIfNull([NotNull] T argument, [CallerArgumentExpression(nameof(argument))] string paramName = null)
#else
		public static void ThrowIfNull([NotNull] T argument, string paramName = null)
#endif
		{
			if (argument is null)
			{
				Throw(paramName);
			}
		}
	}

	/// <summary>
	/// 抛出 <see cref="ArgumentNullException"/>。
	/// </summary>
	/// <param name="paramName">验证失败的参数名称。</param>
	[DoesNotReturn]
	private static void Throw(string paramName)
	{
		throw new ArgumentNullException(paramName);
	}
}