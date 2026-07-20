using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// 表示 void 类型，因为 <see cref="System.Void"/> 在 C# 中不是有效的返回类型。
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>, IComparable
{
	private static readonly Unit _value = new();

	/// <summary>
	/// <see cref="Unit"/> 类型的默认且唯一的值。
	/// </summary>
	public static ref readonly Unit Value => ref _value;

	/// <summary>
	/// 从 <see cref="Unit"/> 类型创建的 <see cref="Task"/>。
	/// </summary>
	public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(_value);

	/// <summary>
	/// 将当前对象与另一个同类型的对象进行比较。
	/// </summary>
	/// <param name="other">要与当前对象进行比较的对象。</param>
	/// <returns>
	/// 一个值，指示正在比较的对象的相对顺序。
	/// 返回值的含义如下：
	///  - 小于零：此对象小于 <paramref name="other" /> 参数。
	///  - 零：此对象等于 <paramref name="other" />。
	///  - 大于零：此对象大于 <paramref name="other" />。
	/// </returns>
	public int CompareTo(Unit other) => 0;

	/// <summary>
	/// 将当前实例与另一个同类型的对象进行比较，并返回一个整数，指示当前实例在排序顺序中位于另一个对象之前、之后还是相同位置。
	/// </summary>
	/// <param name="obj">要与当前实例进行比较的对象。</param>
	/// <returns>
	/// 一个值，指示正在比较的对象的相对顺序。
	/// 返回值的含义如下：
	///  - 小于零：此实例在排序顺序中位于 <paramref name="obj" /> 之前。
	///  - 零：此实例在排序顺序中与 <paramref name="obj" /> 处于相同位置。
	///  - 大于零：此实例在排序顺序中位于 <paramref name="obj" /> 之后。
	/// </returns>
	int IComparable.CompareTo(object obj) => 0;

	/// <summary>
	/// 返回此实例的哈希码。
	/// </summary>
	/// <returns>
	/// 此实例的哈希码，适用于哈希算法和哈希表等数据结构。
	/// </returns>
	public override int GetHashCode() => 0;

	/// <summary>
	/// 确定当前对象是否等于另一个同类型的对象。
	/// </summary>
	/// <param name="other">要与当前对象进行比较的对象。</param>
	/// <returns>
	/// 如果当前对象等于 <paramref name="other" /> 参数，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Unit other) => true;

	/// <summary>
	/// 确定指定的 <see cref="System.Object" /> 是否等于此实例。
	/// </summary>
	/// <param name="obj">要与当前实例进行比较的对象。</param>
	/// <returns>
	/// 如果指定的 <see cref="System.Object" /> 等于此实例，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	public override bool Equals(object obj) => obj is Unit;

	/// <summary>
	/// 确定 <paramref name="first"/> 对象是否等于 <paramref name="second"/> 对象。
	/// </summary>
	/// <param name="first">第一个对象。</param>
	/// <param name="second">第二个对象。</param>
	/// <returns><c>true</c> 如果 <paramref name="first"/> 对象等于 <paramref name="second" /> 对象；否则为 <c>false</c>。</returns>
	public static bool operator ==(Unit first, Unit second) => true;

	/// <summary>
	/// 确定 <paramref name="first"/> 对象是否不等于 <paramref name="second"/> 对象。
	/// </summary>
	/// <param name="first">第一个对象。</param>
	/// <param name="second">第二个对象。</param>
	/// <returns><c>true</c> 如果 <paramref name="first"/> 对象不等于 <paramref name="second" /> 对象；否则为 <c>false</c>。</returns>
	public static bool operator !=(Unit first, Unit second) => false;

	/// <summary>
	/// 返回表示此实例的 <see cref="System.String" />。
	/// </summary>
	/// <returns>表示此实例的 <see cref="System.String" />。</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override string ToString() => "()";
}