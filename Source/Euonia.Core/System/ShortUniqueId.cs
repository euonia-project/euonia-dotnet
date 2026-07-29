using System.Buffers;
using System.Text.RegularExpressions;

namespace System;

/// <summary>
/// 定义用于生成短唯一 ID 的类。
/// </summary>
public sealed class ShortUniqueId
{
	private const string DEFAULT_ALPHABET = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
	private const string DEFAULT_SEPS = "cfhistuCFHISTU";
	private const int MIN_ALPHABET_LENGTH = 16;
	private const int MAX_STACKALLOC_SIZE = 512;

	private const double SEP_DIV = 3.5;
	private const double GUARD_DIV = 12.0;

	private readonly char[] _alphabet;
	private readonly char[] _seps;
	private readonly char[] _guards;
	private readonly char[] _salt;
	private readonly int _minHashLength;
	private readonly int _minBufferSize;

	// 首次使用时创建 Regex，加速非十六进制方法的首次调用
	private static readonly Lazy<Regex> _hexValidator = new(() => new Regex("^[0-9a-fA-F]+$", RegexOptions.Compiled));
	private static readonly Lazy<Regex> _hexSplitter = new(() => new Regex(@"[\w\W]{1,12}", RegexOptions.Compiled));
	private readonly StringBuilderPool _stringBuilderPool = new();

	private static readonly Lazy<ShortUniqueId> _default = new(() => new ShortUniqueId(), isThreadSafe: true);

	/// <summary>
	/// 获取带有默认参数的 <see cref="ShortUniqueId"/> 默认实例。
	/// </summary>
	/// <remarks>
	/// 当您不需要自定义配置时，可以使用此实例。
	/// </remarks>
	public static ShortUniqueId Default => _default.Value;

	/// <summary>
	/// 使用默认参数实例化新的 Hashids 编码器/解码器。
	/// </summary>
	public ShortUniqueId()
		: this(salt: string.Empty, minHashLength: 0, alphabet: DEFAULT_ALPHABET, seps: DEFAULT_SEPS)
	{
		// 需要带有默认值的空构造函数，以允许对公共方法进行模拟
	}

	/// <summary>
	/// 实例化新的 Hashids 编码器/解码器。
	/// 所有参数都是可选的，除非另有说明，否则将使用默认值。
	/// </summary>
	/// <param name="salt">盐值。</param>
	/// <param name="minHashLength">最小哈希长度。</param>
	/// <param name="alphabet">字符集。</param>
	/// <param name="seps">分隔符集合。</param>
	public ShortUniqueId(string salt = "", int minHashLength = 0, string alphabet = DEFAULT_ALPHABET, string seps = DEFAULT_SEPS)
	{
		if (salt == null)
			throw new ArgumentNullException(nameof(salt));
		if (minHashLength < 0)
			throw new ArgumentOutOfRangeException(nameof(minHashLength), "值必须为零或大于零。");
		if (string.IsNullOrWhiteSpace(alphabet))
			throw new ArgumentNullException(nameof(alphabet));
		if (string.IsNullOrWhiteSpace(seps))
			throw new ArgumentNullException(nameof(seps));

		_salt = salt.Trim().ToCharArray();
		_minHashLength = minHashLength;
		_alphabet = alphabet.ToCharArray().Distinct().ToArray();
		_seps = seps.ToCharArray();

		// 使用最小缓冲区大小 20，比最大 64 位整数（long.MaxValue = 9223372036854775807）多 1 位
		_minBufferSize = Math.Max(20, minHashLength);

		if (_alphabet.Length < MIN_ALPHABET_LENGTH)
			throw new ArgumentException($"字符集必须至少包含 {MIN_ALPHABET_LENGTH:N0} 个唯一字符。", paramName: nameof(alphabet));

		// 分隔符只能从字符集中的字符中选择
		if (_seps.Length > 0)
			_seps = _seps.Intersect(_alphabet).ToArray();

		// 选定分隔符后，必须将它们从可用于哈希生成的字符集中移除
		if (_seps.Length > 0)
			_alphabet = _alphabet.Except(_seps).ToArray();

		if (_alphabet.Length < (MIN_ALPHABET_LENGTH - 6))
			throw new ArgumentException($"字符集必须至少包含 {MIN_ALPHABET_LENGTH:N0} 个不在分隔符中的唯一字符。", paramName: nameof(alphabet));

		ConsistentShuffle(alphabet: _seps, salt: _salt);

		if (_seps.Length == 0 || ((float)_alphabet.Length / _seps.Length) > SEP_DIV)
		{
			var sepsLength = (int)Math.Ceiling((float)_alphabet.Length / SEP_DIV);

			if (sepsLength == 1)
				sepsLength = 2;

			if (sepsLength > _seps.Length)
			{
				var diff = sepsLength - _seps.Length;
				_seps = Append(_seps, _alphabet, 0, diff);
				_alphabet = SubArray(_alphabet, diff);
			}
			else
			{
				_seps = SubArray(_seps, 0, sepsLength);
			}
		}

		ConsistentShuffle(alphabet: _alphabet, salt: _salt);

		var guardCount = (int)Math.Ceiling(_alphabet.Length / GUARD_DIV);

		if (_alphabet.Length < 3)
		{
			_guards = SubArray(_seps, index: 0, length: guardCount);
			_seps = SubArray(_seps, index: guardCount);
		}

		else
		{
			_guards = SubArray(_alphabet, index: 0, length: guardCount);
			_alphabet = SubArray(_alphabet, index: guardCount);
		}
	}

	/// <summary>
	/// 将提供的数字编码为哈希字符串。
	/// </summary>
	/// <param name="number">要编码的数字。</param>
	/// <returns>哈希字符串。</returns>
	public string Encode(int number) => EncodeInt64(number);

	/// <summary>
	/// 将提供的数字编码为哈希字符串。
	/// </summary>
	/// <param name="numbers">整数列表。</param>
	/// <returns>编码后的哈希字符串。</returns>
	public string Encode(params int[] numbers)
	{
		return EncodeInt64(Array.ConvertAll(numbers, n => (long)n));
	}

	/// <summary>
	/// 将提供的数字编码为哈希字符串。
	/// </summary>
	/// <param name="numbers">可枚举的整数列表。</param>
	/// <returns>编码后的哈希字符串。</returns>
	public string Encode(IEnumerable<int> numbers)
	{
		return Encode(numbers.ToArray());
	}

	/// <summary>
	/// 将提供的数字编码为哈希字符串。
	/// </summary>
	/// <param name="number">要编码的数字。</param>
	/// <returns>哈希字符串。</returns>
	public string EncodeInt64(long number)
	{
		var numberLength = _minBufferSize;
		var result = numberLength < MAX_STACKALLOC_SIZE ? stackalloc char[numberLength] : new char[numberLength];
		var length = GenerateHashFrom(number, ref result);
		return length == -1 ? string.Empty : result.Slice(0, length).ToString();
	}

	/// <summary>
	/// 将提供的数字编码为哈希字符串。
	/// </summary>
	/// <param name="numbers">64 位整数列表。</param>
	/// <returns>编码后的哈希字符串。</returns>
	public string EncodeInt64(params long[] numbers)
	{
		var numbersLength = _minBufferSize * numbers.Length;
		var result = numbersLength < MAX_STACKALLOC_SIZE ? stackalloc char[numbersLength] : new char[numbersLength];
		var length = GenerateHashFrom(numbers, ref result);
		return length == -1 ? string.Empty : result[..length].ToString();
	}

	/// <summary>
	/// 将提供的数字编码为哈希字符串。
	/// </summary>
	/// <param name="numbers">可枚举的 64 位整数列表。</param>
	/// <returns>编码后的哈希字符串。</returns>
	public string EncodeInt64(IEnumerable<long> numbers) => EncodeInt64([.. numbers]);

	/// <summary>
	/// 将提供的哈希解码为整数数组。
	/// </summary>
	/// <param name="hash">要解码的哈希字符串。</param>
	/// <returns>整数数组。</returns>
	/// <exception cref="T:System.OverflowException">如果解码的数字溢出整数范围。</exception>
	public int[] Decode(string hash) => Array.ConvertAll(GetNumbersFrom(hash), n => (int)n);

	/// <summary>
	/// 将提供的哈希解码为 64 位整数数组。
	/// </summary>
	/// <param name="hash">要解码的哈希字符串。</param>
	/// <returns>64 位整数数组。</returns>
	public long[] DecodeInt64(string hash) => GetNumbersFrom(hash);

	/// <summary>
	/// 将提供的哈希解码为单个 64 位整数。
	/// </summary>
	/// <param name="hash">要解码的哈希字符串。</param>
	/// <returns>解码后的 64 位整数。</returns>
	/// <exception cref="Exception">当提供的哈希没有产生任何结果时抛出。</exception>
	public long DecodeSingleInt64(string hash)
	{
		var number = GetNumberFrom(hash);

		return number switch
		{
			-1 => throw new Exception("提供的哈希没有产生任何结果。"),
			_ => number,
		};
	}

	/// <summary>
	/// 尝试将提供的哈希解码为单个 64 位整数。
	/// </summary>
	/// <param name="hash">要解码的哈希字符串。</param>
	/// <param name="id">解码成功时输出的 64 位整数。</param>
	/// <returns>如果解码成功则为 true；否则为 false。</returns>
	public bool TryDecodeSingleInt64(string hash, out long id)
	{
		var number = GetNumberFrom(hash);

		if (number >= 0)
		{
			id = number;
			return true;
		}

		id = 0L;
		return false;
	}

	/// <summary>
	/// 将提供的哈希解码为单个整数。
	/// </summary>
	/// <param name="hash">要解码的哈希字符串。</param>
	/// <returns>解码后的整数。</returns>
	/// <exception cref="Exception">当提供的哈希没有产生任何结果时抛出。</exception>
	public int DecodeSingle(string hash)
	{
		var number = GetNumberFrom(hash);

		return number switch
		{
			-1 => throw new Exception("提供的哈希没有产生任何结果。"),
			_ => (int)number,
		};
	}

	/// <summary>
	/// 尝试将提供的哈希解码为单个整数。
	/// </summary>
	/// <param name="hash">要解码的哈希字符串。</param>
	/// <param name="id">解码成功时输出的整数。</param>
	/// <returns>如果解码成功则为 true；否则为 false。</returns>
	public bool TryDecodeSingle(string hash, out int id)
	{
		var number = GetNumberFrom(hash);

		if (number >= 0)
		{
			id = (int)number;
			return true;
		}

		id = 0;
		return false;
	}

	/// <summary>
	/// 将提供的十六进制字符串编码为哈希字符串。
	/// </summary>
	/// <param name="hex">要编码的十六进制字符串。</param>
	/// <returns>编码后的哈希字符串。</returns>
	public string EncodeHex(string hex)
	{
		if (string.IsNullOrWhiteSpace(hex) || !_hexValidator.Value.IsMatch(hex))
			return string.Empty;

		var matches = _hexSplitter.Value.Matches(hex);
		if (matches.Count == 0)
			return string.Empty;

		var numbers = new long[matches.Count];
		for (int i = 0; i < numbers.Length; i++)
		{
			var match = matches[i];
			var concat = string.Concat("1", match.Value);
			var number = Convert.ToInt64(concat, fromBase: 16);
			numbers[i] = number;
		}

		return EncodeInt64(numbers);
	}

	/// <summary>
	/// 将提供的哈希解码为十六进制字符串。
	/// </summary>
	/// <param name="hash">要解码的哈希字符串。</param>
	/// <returns>解码后的十六进制字符串。</returns>
	public string DecodeHex(string hash)
	{
		var builder = _stringBuilderPool.Get();
		var numbers = DecodeInt64(hash);

		foreach (var number in numbers)
		{
			var s = number.ToString("X");
			for (var i = 1; i < s.Length; i++)
				builder.Append(s[i]);
		}

		var result = builder.ToString();
		_stringBuilderPool.Return(builder);
		return result;
	}

	private int GenerateHashFrom(long number, ref Span<char> result)
	{
		if (number < 0)
			return 0;

		var numberHashInt = number % 100;

		var alphabet = _alphabet.Length < MAX_STACKALLOC_SIZE ? stackalloc char[_alphabet.Length] : new char[_alphabet.Length];
		_alphabet.CopyTo(alphabet);

		var lottery = alphabet[(int)(numberHashInt % _alphabet.Length)];
		result[0] = lottery;

		var shuffleBuffer = _alphabet.Length < MAX_STACKALLOC_SIZE ? stackalloc char[_alphabet.Length] : new char[_alphabet.Length];
		shuffleBuffer[0] = lottery;
		_salt.AsSpan().Slice(0, Math.Min(_salt.Length, _alphabet.Length - 1)).CopyTo(shuffleBuffer.Slice(1));

		var startIndex = 1 + _salt.Length;
		var length = _alphabet.Length - startIndex;

		Span<char> hashBuffer = stackalloc char[_minBufferSize];

		if (length > 0)
			alphabet.Slice(0, length).CopyTo(shuffleBuffer.Slice(startIndex));

		ConsistentShuffle(alphabet, shuffleBuffer);
		var hashLength = BuildReversedHash(number, alphabet, hashBuffer);

		// 在循环中反转 hashBuffer 并插入到 result 中
		for (var i = 0; i < hashLength; i++)
			result[i + 1] = hashBuffer[hashLength - i - 1];

		hashLength += 1;

		if (hashLength < _minHashLength)
		{
			var guardIndex = (numberHashInt + result[0]) % _guards.Length;
			var guard = _guards[guardIndex];

			result.Slice(0, hashLength).CopyTo(result.Slice(1));
			result[0] = guard;
			hashLength += 1;

			if (hashLength < _minHashLength)
			{
				guardIndex = (numberHashInt + result[2]) % _guards.Length;
				guard = _guards[guardIndex];

				result[hashLength] = guard;
				hashLength += 1;
			}
		}

		var halfLength = _alphabet.Length / 2;

		var stringBuilder = _stringBuilderPool.Get();
#if NETSTANDARD2_0
            stringBuilder.Append(result.Slice(0, hashLength).ToArray());
#else
		stringBuilder.Append(result[..hashLength]);
#endif

		while (stringBuilder.Length < _minHashLength)
		{
			alphabet.CopyTo(shuffleBuffer);
			ConsistentShuffle(alphabet, shuffleBuffer);

#if NETSTANDARD2_0
                stringBuilder.Insert(0, alphabet.Slice(halfLength, _alphabet.Length - halfLength).ToArray());
                stringBuilder.Append(alphabet.Slice(0, halfLength).ToArray());
#else
			stringBuilder.Insert(0, alphabet[halfLength.._alphabet.Length]);
			stringBuilder.Append(alphabet[..halfLength]);
#endif

			var excess = stringBuilder.Length - _minHashLength;
			if (excess > 0)
			{
				stringBuilder.Remove(0, excess / 2);
				stringBuilder.Remove(_minHashLength, stringBuilder.Length - _minHashLength);
			}
		}

		hashLength = stringBuilder.Length;

#if NETSTANDARD2_0
            for (var i = 0; i < stringBuilder.Length; i++)
                result[i] = stringBuilder[i];
#else
		stringBuilder.CopyTo(0, result, stringBuilder.Length);
#endif

		_stringBuilderPool.Return(stringBuilder);

		return hashLength;
	}

	private int GenerateHashFrom(ReadOnlySpan<long> numbers, ref Span<char> result)
	{
		if (numbers.Length == 0)
			return -1;

		foreach (var num in numbers)
			if (num < 0)
				return -1;

		long numbersHashInt = 0;
		for (var i = 0; i < numbers.Length; i++)
			numbersHashInt += numbers[i] % (i + 100);

		var stringBuilder = _stringBuilderPool.Get();

		Span<char> alphabet = _alphabet.Length < MAX_STACKALLOC_SIZE ? stackalloc char[_alphabet.Length] : new char[_alphabet.Length];
		_alphabet.CopyTo(alphabet);

		var lottery = alphabet[(int)(numbersHashInt % _alphabet.Length)];
		stringBuilder.Append(lottery);

		Span<char> shuffleBuffer = _alphabet.Length < MAX_STACKALLOC_SIZE ? stackalloc char[_alphabet.Length] : new char[_alphabet.Length];
		shuffleBuffer[0] = lottery;
		_salt.AsSpan().Slice(0, Math.Min(_salt.Length, _alphabet.Length - 1)).CopyTo(shuffleBuffer.Slice(1));

		var startIndex = 1 + _salt.Length;
		var length = _alphabet.Length - startIndex;

		Span<char> hashBuffer = stackalloc char[_minBufferSize];

		for (var i = 0; i < numbers.Length; i++)
		{
			var number = numbers[i];

			if (length > 0)
				alphabet.Slice(0, length).CopyTo(shuffleBuffer.Slice(startIndex));

			ConsistentShuffle(alphabet, shuffleBuffer);
			var hashLength = BuildReversedHash(number, alphabet, hashBuffer);

			for (var j = hashLength - 1; j > -1; j--)
				stringBuilder.Append(hashBuffer[j]);

			if (i + 1 < numbers.Length)
			{
				number %= hashBuffer[hashLength - 1] + i;
				var sepsIndex = number % _seps.Length;

				stringBuilder.Append(_seps[sepsIndex]);
			}
		}

		if (stringBuilder.Length < _minHashLength)
		{
			var guardIndex = (numbersHashInt + stringBuilder[0]) % _guards.Length;
			var guard = _guards[guardIndex];

			stringBuilder.Insert(0, guard);

			if (stringBuilder.Length < _minHashLength)
			{
				guardIndex = (numbersHashInt + stringBuilder[2]) % _guards.Length;
				guard = _guards[guardIndex];

				stringBuilder.Append(guard);
			}
		}

		var halfLength = _alphabet.Length / 2;

		while (stringBuilder.Length < _minHashLength)
		{
			alphabet.CopyTo(shuffleBuffer);
			ConsistentShuffle(alphabet, shuffleBuffer);

#if NETSTANDARD2_0
                stringBuilder.Insert(0, alphabet.Slice(halfLength, _alphabet.Length - halfLength).ToArray());
                stringBuilder.Append(alphabet.Slice(0, halfLength).ToArray());
#else
			stringBuilder.Insert(0, alphabet[halfLength.._alphabet.Length]);
			stringBuilder.Append(alphabet[..halfLength]);
#endif

			var excess = stringBuilder.Length - _minHashLength;
			if (excess > 0)
			{
				stringBuilder.Remove(0, excess / 2);
				stringBuilder.Remove(_minHashLength, stringBuilder.Length - _minHashLength);
			}
		}

		var resultLength = stringBuilder.Length;

#if NETSTANDARD2_0
            for (var i = 0; i < stringBuilder.Length; i++)
                result[i] = stringBuilder[i];
#else
		stringBuilder.CopyTo(0, result, stringBuilder.Length);
#endif

		_stringBuilderPool.Return(stringBuilder);
		return resultLength;
	}

	private int BuildReversedHash(long input, ReadOnlySpan<char> alphabet, Span<char> hashBuffer)
	{
		var length = 0;
		do
		{
			int idx = (int)(input % _alphabet.Length);
			hashBuffer[length] = alphabet[idx];
			length += 1;
			input /= _alphabet.Length;
		}
		while (input > 0);

		return length;
	}

	private long Unhash(ReadOnlySpan<char> input, ReadOnlySpan<char> alphabet)
	{
		long number = 0;

		for (var i = 0; i < input.Length; i++)
		{
			var pos = alphabet.IndexOf(input[i]);
			number = (number * _alphabet.Length) + pos;
		}

		return number;
	}

	private long GetNumberFrom(string hash)
	{
		if (string.IsNullOrWhiteSpace(hash))
			return -1;

		var guardedHash = hash.AsSpan();
		var (count, ranges) = Split(guardedHash, _guards);

		var unguardedIndex = count is 3 or 2 ? 1 : 0;
		var (start, offset) = ranges[unguardedIndex];
		var hashBreakdown = guardedHash.Slice(start, offset);

		ArrayPool<(int, int)>.Shared.Return(ranges);

		var lottery = hashBreakdown[0];
		if (lottery == '\0')
			return -1;

		var hashBuffer = hashBreakdown.Slice(1);

		Span<char> alphabet = _alphabet.Length < MAX_STACKALLOC_SIZE ? stackalloc char[_alphabet.Length] : new char[_alphabet.Length];
		_alphabet.CopyTo(alphabet);

		Span<char> buffer = _alphabet.Length < MAX_STACKALLOC_SIZE ? stackalloc char[_alphabet.Length] : new char[_alphabet.Length];
		buffer[0] = lottery;
		_salt.AsSpan().Slice(0, Math.Min(_salt.Length, _alphabet.Length - 1)).CopyTo(buffer.Slice(1));

		var startIndex = 1 + _salt.Length;
		var length = _alphabet.Length - startIndex;

		if (length > 0)
			alphabet.Slice(0, length).CopyTo(buffer.Slice(startIndex));

		ConsistentShuffle(alphabet, buffer);
		var result = Unhash(hashBuffer, alphabet);

		// 从数字重新生成哈希并与给定的哈希进行比较，以确保使用了正确的参数
		// 确保缓冲区足够大，基于生成的内容
		var bufferSize = Math.Max(_minBufferSize, guardedHash.Length);
		Span<char> resultBuffer = stackalloc char[bufferSize];
		var hashLength = GenerateHashFrom(result, ref resultBuffer);
		ReadOnlySpan<char> rehash = resultBuffer.Slice(0, hashLength);
		if (guardedHash.Equals(rehash, StringComparison.Ordinal))
			return result;

		return -1;
	}

	private long[] GetNumbersFrom(string hash)
	{
		var result = NumbersFrom(hash);

		int bufferSizeToAllocate = Math.Max(hash.Length, _minHashLength);
		Span<char> hashBuffer = bufferSizeToAllocate < MAX_STACKALLOC_SIZE ? stackalloc char[bufferSizeToAllocate] : new char[bufferSizeToAllocate];
		var hashLength = GenerateHashFrom(result, ref hashBuffer);
		if (hashLength == -1)
		{
			return [];
		}

		ReadOnlySpan<char> rehash = hashBuffer[..hashLength];
		// 从数字重新生成哈希并与给定的哈希进行比较，以确保使用了正确的参数
		if (hash.AsSpan().Equals(rehash, StringComparison.Ordinal))
		{
			return result;
		}

		return [];
	}

	private long[] NumbersFrom(string hash)
	{
		if (string.IsNullOrWhiteSpace(hash))
		{
			return Array.Empty<long>();
		}

		var guardedHash = hash.AsSpan();
		var (count, ranges) = Split(guardedHash, _guards);

		if (count == 0)
		{
			return Array.Empty<long>();
		}

		var unguardedIndex = count is 3 or 2 ? 1 : 0;
		var (start, offset) = ranges[unguardedIndex];
		var hashBreakdown = guardedHash.Slice(start, offset);

		ArrayPool<(int, int)>.Shared.Return(ranges);

		var lottery = hashBreakdown[0];
		if (lottery == '\0') // default(char) 是 '\0'
		{
			return [];
		}

		var hashBuffer = hashBreakdown[1..];
		(count, ranges) = Split(hashBuffer, _seps);

		var result = new long[count];

		Span<char> alphabet = _alphabet.Length < MAX_STACKALLOC_SIZE ? stackalloc char[_alphabet.Length] : new char[_alphabet.Length];
		_alphabet.CopyTo(alphabet);

		Span<char> buffer = _alphabet.Length < MAX_STACKALLOC_SIZE ? stackalloc char[_alphabet.Length] : new char[_alphabet.Length];
		buffer[0] = lottery;
		_salt.AsSpan()[..Math.Min(_salt.Length, _alphabet.Length - 1)].CopyTo(buffer[1..]);

		var startIndex = 1 + _salt.Length;
		var length = _alphabet.Length - startIndex;

		for (var index = 0; index < count; index++)
		{
			(start, offset) = ranges[index];
			var subHash = hashBuffer.Slice(start, offset);

			if (length > 0)
			{
				alphabet[..length].CopyTo(buffer[startIndex..]);
			}

			ConsistentShuffle(alphabet, buffer);
			result[index] = Unhash(subHash, alphabet);
		}

		ArrayPool<(int, int)>.Shared.Return(ranges);
		return result;
	}

	/// <summary>
	/// 注意：此方法会就地修改 <paramref name="alphabet"/> 参数。
	/// </summary>
	private static void ConsistentShuffle(Span<char> alphabet, ReadOnlySpan<char> salt)
	{
		if (salt.Length == 0)
		{
			return;
		}

		// TODO: 记录或重命名这些含义模糊的变量：i, v, p。
		for (int i = alphabet.Length - 1, v = 0, p = 0; i > 0; i--, v++)
		{
			v %= salt.Length;
			int saltNum = salt[v];
			p += saltNum;
			var j = (saltNum + v + p) % i;

			// 交换位置 i 和 j 的字符：
			(alphabet[i], alphabet[j]) = (alphabet[j], alphabet[i]);
		}
	}

	private static (int count, (int, int)[] ranges) Split(ReadOnlySpan<char> line, ReadOnlySpan<char> separators)
	{
		var count = 0;
		var indexStart = 0;
		var nextSeparatorIndex = 0;
		var ranges = ArrayPool<(int, int)>.Shared.Rent(line.Length);
		var isLastLoop = false;
		while (!isLastLoop)
		{
			indexStart += nextSeparatorIndex;
			nextSeparatorIndex = line[indexStart..].IndexOfAny(separators);
			if (nextSeparatorIndex == 0)
			{
				indexStart++;
				nextSeparatorIndex = line[indexStart..].IndexOfAny(separators);
			}

			isLastLoop = nextSeparatorIndex == -1;
			if (isLastLoop)
			{
				nextSeparatorIndex = line.Length - indexStart;
			}

			var slice = line.Slice(indexStart, nextSeparatorIndex);
			if (slice.IsEmpty)
			{
				continue;
			}

			ranges[count] = (indexStart, nextSeparatorIndex);
			count++;
		}

		return (count, ranges);
	}

	private static T[] SubArray<T>(T[] array, int index)
	{
		return SubArray(array, index, array.Length - index);
	}

	private static T[] SubArray<T>(T[] array, int index, int length)
	{
		if (index == 0 && length == array.Length)
		{
			return array;
		}

		if (length == 0)
		{
			return [];
		}

		var subarray = new T[length];
		Array.Copy(array, index, subarray, 0, length);
		return subarray;
	}

	private static T[] Append<T>(T[] array, T[] appendArray, int index, int length)
	{
		if (length == 0)
			return array;

		int newLength = array.Length + length - index;
		if (newLength == 0)
			return Array.Empty<T>();

		var newArray = new T[newLength];
		Array.Copy(array, 0, newArray, 0, array.Length);
		Array.Copy(appendArray, index, newArray, array.Length, length - index);
		return newArray;
	}
}