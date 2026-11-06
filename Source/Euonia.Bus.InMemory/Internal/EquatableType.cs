using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// A simple type representing an immutable pair of types.
/// </summary>
/// <remarks>
/// This type replaces a simple <see cref="ValueTuple{T1,T2}"/> as it's faster in its
/// <see cref="GetHashCode"/> and <see cref="IEquatable{T}.Equals(T)"/> methods, and because
/// unlike a value tuple it exposes its fields as immutable. Additionally, the
/// <see cref="Message"/> and <see cref="Token"/> fields provide additional clarity reading
/// the code compared to <see cref="ValueTuple{T1,T2}.Item1"/> and <see cref="ValueTuple{T1,T2}.Item2"/>.
/// </remarks>
internal readonly struct EquatableType : IEquatable<EquatableType>
{
	/// <summary>
	/// The type of registered message.
	/// </summary>
	public readonly Type Message;

	/// <summary>
	/// The type of registration token.
	/// </summary>
	public readonly Type Token;

	/// <summary>
	/// Initializes a new instance of the <see cref="EquatableType"/> struct.
	/// </summary>
	/// <param name="message">The type of registered message.</param>
	/// <param name="token">The type of registration token.</param>
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
		// We can't just use reference equality, as that's technically not guaranteed
		// to work and might fail in very rare cases (eg. with type forwarding between
		// different assemblies). Instead, we can use the == operator to compare for
		// equality, which still avoids the callvirt overhead of calling Type.Equals,
		// and is also implemented as a JIT intrinsic on runtimes such as .NET Core.
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
		// To combine the two hashes, we can simply use the fast djb2 hash algorithm. Unfortunately we
		// can't really skip the callvirt here (eg. by using RuntimeHelpers.GetHashCode like in other
		// cases), as there are some niche cases mentioned above that might break when doing so.
		// However since this method is not generally used in a hot path (eg. the message broadcasting
		// only invokes this a handful of times when initially retrieving the target mapping), this
		// doesn't actually make a noticeable difference despite the minor overhead of the virtual call.
		int hash = Message.GetHashCode();

		hash = (hash << 5) + hash;

		hash += Token.GetHashCode();

		return hash;
	}
}