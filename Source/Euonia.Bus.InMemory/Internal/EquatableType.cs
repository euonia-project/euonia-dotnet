using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 表示一对不可变类型的简单类型。
/// </summary>
/// <remarks>
/// 此类型替代简单的 <see cref="ValueTuple{T1,T2}"/>，因为其 <see cref="GetHashCode"/> 和
/// <see cref="IEquatable{T}.Equals(T)"/> 方法更快，并且与值元组不同，它将字段公开为不可变。
/// 此外，与 <see cref="ValueTuple{T1,T2}.Item1"/> 和 <see cref="ValueTuple{T1,T2}.Item2"/> 相比，
/// <see cref="Message"/> 和 <see cref="Token"/> 字段在阅读代码时提供了更高的清晰度。
/// </remarks>
internal readonly struct EquatableType : IEquatable<EquatableType>
{
	/// <summary>
	/// 已注册消息的类型。
	/// </summary>
	public readonly Type Message;

	/// <summary>
	/// 注册令牌的类型。
	/// </summary>
	public readonly Type Token;

	/// <summary>
	/// 初始化 <see cref="EquatableType"/> 结构的新实例。
	/// </summary>
	/// <param name="message">已注册消息的类型。</param>
	/// <param name="token">注册令牌的类型。</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public EquatableType(Type message, Type token)
	{
		Message = message;
		Token = token;
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(EquatableType other)
	{
		// 我们不能只是使用引用相等，因为从技术上讲这并不保证可行，并且在极少数情况下
		// 可能失败（例如，在不同程序集之间进行类型转发时）。相反，我们可以使用 == 运算符
		// 比较相等性，这仍然避免了调用 Type.Equals 的 callvirt 开销，
		// 并且在 .NET Core 等运行时上还实现为 JIT 内联函数。
		return
			Message == other.Message &&
			Token == other.Token;
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
	{
		return obj is EquatableType other && Equals(other);
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode()
	{
		// 要合并两个哈希值，我们可以直接使用快速的 djb2 哈希算法。遗憾的是，此处
		// 无法真正跳过 callvirt（例如像其他场景那样使用 RuntimeHelpers.GetHashCode），
		// 因为上文提到的一些小众场景在这样做时可能会出错。
		// 不过，由于此方法通常不在热路径中使用（例如消息广播仅在初始化
		// 检索目标映射时调用它寥寥几次），因此尽管存在虚调用的少量开销，
		// 这实际上并不会产生明显差异。
		int hash = Message.GetHashCode();

		hash = (hash << 5) + hash;

		hash += Token.GetHashCode();

		return hash;
	}
}