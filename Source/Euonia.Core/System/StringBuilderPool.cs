using System.Collections.Concurrent;

namespace System;

/// <summary>
/// 定义 <see cref="StringBuilder"/> 实例的池。
/// </summary>
internal class StringBuilderPool
{
	private readonly ConcurrentBag<StringBuilder> _builders = [];

	/// <summary>
	/// 从池中获取 <see cref="StringBuilder"/> 实例，如果池为空则创建新实例。
	/// </summary>
	public StringBuilder Get() => _builders.TryTake(out var builder) ? builder : new StringBuilder();

	/// <summary>
	/// 将 <see cref="StringBuilder"/> 实例归还到池中。
	/// </summary>
	/// <param name="builder">要归还的 <see cref="StringBuilder"/> 实例。</param>
	public void Return(StringBuilder builder)
	{
		builder.Clear();
		_builders.Add(builder);
	}
}