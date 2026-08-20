using System.Diagnostics.CodeAnalysis;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 提供针对 <see cref="Task{TResult}"/> 的链式扩展方法，用于在执行器流程中传递和转换对象。
/// </summary>
public static class ActuatorExtensions
{
	/// <param name="source">包含目标对象的异步任务。</param>
	/// <typeparam name="TTarget">源对象类型。</typeparam>
	extension<TTarget>(Task<TTarget> source)
	{
		/// <summary>
		/// 在目标对象获取完成后将其转换为指定类型的结果。
		/// </summary>
		/// <typeparam name="TResult">转换后的结果类型。</typeparam>
		/// <param name="selector">从目标对象到结果类型的转换委托。</param>
		/// <returns>包含转换结果的 <see cref="Task{TResult}"/>。</returns>
		public async Task<TResult> ReturnAsync<TResult>([NotNull] Func<TTarget, TResult> selector)
		{
			var result = await source;
			return selector(result);
		}

		/// <summary>
		/// 在目标对象获取完成后对其执行转换和后续操作。
		/// </summary>
		/// <typeparam name="TResult">转换后的中间类型。</typeparam>
		/// <param name="selector">从目标对象到中间结果的转换委托。</param>
		/// <param name="action">对转换结果执行的后续操作。</param>
		/// <returns>表示操作完成的 <see cref="Task"/>。</returns>
		public async Task NextAsync<TResult>([NotNull] Func<TTarget, TResult> selector, [NotNull] Action<TResult> action)
		{
			var result = await source;
			action(selector(result));
		}

		/// <summary>
		/// 在目标对象获取完成后对其执行后续操作。
		/// </summary>
		/// <param name="action">对目标对象执行的后续操作。</param>
		/// <returns>表示操作完成的 <see cref="Task"/>。</returns>
		public async Task NextAsync([NotNull] Action<TTarget> action)
		{
			var result = await source;
			action(result);
		}
	}
}