using System.Runtime.CompilerServices;

namespace Nerosoft.Euonia.Bus.InMemory;

/// <summary>
/// 提供哈希表容量计算的辅助方法，例如根据期望容量选取不小于它的质数。
/// </summary>
internal static class HashHelpers
{
	/// <summary>
	/// 小于最大数组长度的最大质数。
	/// </summary>
	private const int MAX_PRIME_ARRAY_LENGTH = 0x7FFFFFC3;

	/// <summary>
	/// 用于 <see cref="GetPrime(int)"/> 的任意质因子。
	/// </summary>
	private const int HASH_PRIME = 101;

	/// <summary>
	/// 用作哈希表大小的质数表。
	/// </summary>
	private static readonly int[] _primes =
	{
		3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
		1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
		17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
		187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
		1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
	};

	/// <summary>
	/// 检查某个值是否为质数。
	/// </summary>
	/// <param name="candidate">要检查的值。</param>
	/// <returns><paramref name="candidate"/> 是否为质数。</returns>
	private static bool IsPrime(int candidate)
	{
		if ((candidate & 1) != 0)
		{
			int limit = (int)Math.Sqrt(candidate);

			for (int divisor = 3; divisor <= limit; divisor += 2)
			{
				if ((candidate % divisor) == 0)
				{
					return false;
				}
			}

			return true;
		}

		return candidate == 2;
	}

	/// <summary>
	/// 获取大于指定值的最小质数。
	/// </summary>
	/// <param name="min">目标最小值。</param>
	/// <returns>找到的新质数。</returns>
	public static int GetPrime(int min)
	{
		foreach (var prime in _primes)
		{
			if (prime >= min)
			{
				return prime;
			}
		}

		for (var i = min | 1; i < int.MaxValue; i += 2)
		{
			if (IsPrime(i) && ((i - 1) % HASH_PRIME != 0))
			{
				return i;
			}
		}

		return min;
	}

	/// <summary>
	/// 返回哈希表要扩展到的容量。
	/// </summary>
	/// <param name="oldSize">先前的表大小。</param>
	/// <returns>扩展后的表大小。</returns>
	public static int ExpandPrime(int oldSize)
	{
		int newSize = 2 * oldSize;

		if ((uint)newSize > MAX_PRIME_ARRAY_LENGTH && MAX_PRIME_ARRAY_LENGTH > oldSize)
		{
			return MAX_PRIME_ARRAY_LENGTH;
		}

		return GetPrime(newSize);
	}

	/// <summary>
	/// 返回除数的近似倒数：ceil(2**64 / divisor)。
	/// </summary>
	/// <remarks>仅应在 64 位平台上使用。</remarks>
	public static ulong GetFastModMultiplier(uint divisor)
	{
		return ulong.MaxValue / divisor + 1;
	}

	/// <summary>
	/// 使用由 <see cref="GetFastModMultiplier"/> 预先计算的乘数执行取模运算。
	/// </summary>
	/// <remarks>仅应在 64 位平台上使用。</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint FastMod(uint value, uint divisor, ulong multiplier)
	{
		return (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);
	}
}