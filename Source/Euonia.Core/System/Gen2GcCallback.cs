// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace System;

/// <summary>
/// 大致每隔一次 Gen 2 GC 调度一个回调（可能还会看到一次 Gen 0 和 Gen 1，但仅一次）。
/// 移植自 https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Gen2GcCallback.cs。
/// </summary>
public sealed class Gen2GcCallback : CriticalFinalizerObject
{
	/// <summary>
	/// 每次 GC 时要调用的回调。
	/// </summary>
	private readonly Action<object> _callback;

	/// <summary>
	/// 目标对象的弱引用 <see cref="GCHandle"/>，用于传递给 <see cref="_callback"/>。
	/// </summary>
	private GCHandle _handle;

	/// <summary>
	/// 初始化 <see cref="Gen2GcCallback"/> 类的新实例。
	/// </summary>
	/// <param name="callback">每次 GC 时要调用的回调。</param>
	/// <param name="target">作为参数传递给 <paramref name="callback"/> 的目标对象。</param>
	private Gen2GcCallback(Action<object> callback, object target)
	{
		this._callback = callback;
		this._handle = GCHandle.Alloc(target, GCHandleType.Weak);
	}

	/// <summary>
	/// 注册一个回调，使其在每次 GC 时被调用，直到目标对象被回收。
	/// </summary>
	/// <param name="callback">每次 GC 时要调用的回调。</param>
	/// <param name="target">作为参数传递给 <paramref name="callback"/> 的目标对象。</param>
	public static void Register(Action<object> callback, object target)
	{
#if NETSTANDARD2_0
        if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework"))
        {
            // 在 .NET Framework 上使用 GC 回调会导致应用程序域卸载问题，
            // 因此如果检测到该运行时，则不注册回调并忽略。
            // 如果希望在 .NET Framework 上使用，用户将需要手动清理 messenger。
            return;
        }
#endif

		_ = new Gen2GcCallback(callback, target);
	}

	/// <summary>
	/// 终结 <see cref="Gen2GcCallback"/> 类的实例。
	/// 只要目标对象仍然存活，此终结器会通过 <see cref="GC.ReRegisterForFinalize(object)"/> 重新注册，
	/// 这意味着每次触发第 2 代回收时都会再次执行（因为 <see cref="Gen2GcCallback"/> 实例本身
	/// 在第一次幸存于第 0 代和第 1 代回收后将被移至该代）。
	/// </summary>
	~Gen2GcCallback()
	{
		// ReSharper disable once ConvertTypeCheckPatternToNullCheck
		if (_handle.Target is object target)
		{
			try
			{
				_callback(target);
			}
			catch
			{
				// 忽略回调抛出的任何异常。
			}

			GC.ReRegisterForFinalize(this);
		}
		else
		{
			_handle.Free();
		}
	}
}