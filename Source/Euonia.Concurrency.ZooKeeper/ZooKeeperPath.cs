namespace Nerosoft.Euonia.Concurrency.ZooKeeper;

/// <summary>
/// 表示 ZooKeeper 节点的路径。构造函数会验证输入是否为有效路径。
/// 调用 <see cref="ToString"/> 可获取路径值。
/// </summary>
public readonly struct ZooKeeperPath : IEquatable<ZooKeeperPath>
{
	/// <summary>
	/// ZooKeeper 路径使用的分隔符字符。
	/// </summary>
	internal const char Separator = '/';

	/// <summary>
	/// 获取表示 ZooKeeper 根节点的路径。
	/// </summary>
	internal static ZooKeeperPath Root { get; } = new("/");

	private readonly string _path;

	/// <summary>
	/// 基于给定的 <paramref name="path"/> 字符串构造一个新的 <see cref="ZooKeeperPath"/>。
	/// </summary>
	public ZooKeeperPath(string path) : this(path, checkPath: true) { }

	private ZooKeeperPath(string path, bool checkPath, string? paramName = null)
	{
		if (path == null) { throw new ArgumentNullException(paramName ?? nameof(path)); }
		if (checkPath && ValidatePath(path) is { } error)
		{
			throw new FormatException(string.Format(Resources.IDS_PATH_VALIDATION_ERROR, paramName ?? nameof(path), error.Reason, error.Index.HasValue ? string.Format(Resources.IDS_PATH_VALIDATION_ERROR_INDEX, error.Index.Value) : string.Empty));
		}
		_path = path;
	}

	/// <summary>
	/// 获取当前路径的父目录路径。
	/// </summary>
	/// <returns>父目录路径；当前路径为根节点时返回 <see langword="null"/>。</returns>
	internal ZooKeeperPath? GetDirectory()
	{
		if (this == Root) { return null; }
		var lastSeparatorIndex = _path.LastIndexOf(Separator);
		return lastSeparatorIndex == 0 ? Root : new ZooKeeperPath(_path[..lastSeparatorIndex], checkPath: false);
	}

	/// <summary>
	/// 将路径值作为字符串返回
	/// </summary>
	public override string ToString() => _path;

	/// <summary>
	/// 基于路径字符串实现相等性比较
	/// </summary>
	public override bool Equals(object? obj) => obj is ZooKeeperPath that && Equals(that);

	/// <summary>
	/// 基于路径字符串实现相等性比较
	/// </summary>
	public bool Equals(ZooKeeperPath that) => _path == that._path;

	/// <summary>
	/// 基于路径字符串实现哈希计算
	/// </summary>
	public override int GetHashCode() => _path?.GetHashCode() ?? 0;

	/// <summary>
	/// 基于路径字符串实现相等性比较
	/// </summary>
	public static bool operator ==(ZooKeeperPath @this, ZooKeeperPath that) => @this.Equals(that);

	/// <summary>
	/// 基于路径字符串实现不等性比较
	/// </summary>
	public static bool operator !=(ZooKeeperPath @this, ZooKeeperPath that) => !(@this == that);

	/// <summary>
	/// 将指定名称转换为 ZooKeeper 安全的节点名，并拼接为当前路径的子节点路径。
	/// </summary>
	/// <param name="name">子节点名称，可能包含分隔符或非法字符，必要时会被替换并附加哈希。</param>
	/// <returns>当前路径下的子节点路径。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/>。</exception>
	internal ZooKeeperPath GetChildNodePathWithSafeName(string name)
	{
		ArgumentNullException.ThrowIfNull(name);
		
		var isRoot = this == Root;
		var safeName = Helpers.ToSafeName(
				name,
				maxNameLength: int.MaxValue, // 无上限
				convertToValidName: ConvertToValidNodeName
			)
			// 如果 ToSafeName 添加了哈希，它会使用 Base64 编码，而编码结果可能包含分隔符字符。我们将其替换为
			// '_'（它不在 Base64 字符集中），这样输出名称既能保持安全，又不会削弱哈希
			.Replace(Separator, '_');
		return new ZooKeeperPath((this == Root ? _path : (_path + Separator)) + safeName, checkPath: false);

		string ConvertToValidNodeName(string nodeName)
		{
			// 要成为有效的节点名称：

			// 不能为空（对此特殊处理，因为我们的通用转换方法会将空映射为自身）
			if (nodeName.Length == 0) { return "EMPTY"; }

			// 不能是 .、..，或者（当本路径为根时）保留路径 "zookeeper"
			// （参见 https://zookeeper.apache.org/doc/current/zookeeperProgrammers.html#ch_zkDataModel）
			switch (nodeName)
			{
				case ".":
				case "..":
				case "zookeeper" when isRoot:
					return nodeName + "_";
			}

			if (nodeName.IndexOf(Separator) < 0 // 不能包含路径分隔符
				&& !ValidatePath(Separator + nodeName).HasValue) // "/name" 必须是有效路径
			{
				return nodeName;
			}

			var converted = nodeName.ToCharArray();
			for (var i = 0; i < nodeName.Length; ++i)
			{
				switch (nodeName[i])
				{
					// 注意：无需替换 '.'，因为只有当 '.' 或 '..' 作为完整路径段时才无效。
					// 由于我们会追加哈希，这一点无关紧要
					case Separator: // 分隔符不能出现在名称中，只能出现在路径中
					case '\0':
					case var @char when IsNonNullInvalidPathChar(@char):
						converted[i] = '_'; // 替换为占位符
						break;
				}
			}

			return new string(converted);
		}
	}

	private static (string Reason, int? Index)? ValidatePath(string path)
	{
		// 逻辑基于 https://github.com/apache/zookeeper/blob/master/zookeeper-server/src/main/java/org/apache/zookeeper/common/PathUtils.java#L43
		// （引用自 https://stackoverflow.com/questions/55463167/node-name-limitations-in-zookeeper）

		if (path.Length == 0) { return ("may not be empty", null); }
		if (path[0] != Separator) { return ("must start with the '/' character", null); }
		if (path.Length == 1) { return null; }
		if (path[^1] == Separator) { return ("must not end with the '/' character", path.Length - 1); }

		for (var i = 1; i < path.Length; ++i)
		{
			// 与 ConvertToValidNodeName() 保持同步

			switch (path[i])
			{
				case '\0':
					return ("may not contain the null character", i);
				case Separator when path[i - 1] == Separator:
					return ("may not contain empty segments", i);
				case '.' when path[i - (path[i - 1] == '.' ? 2 : 1)] == Separator && ((i + 1 == path.Length) || path[i + 1] == Separator):
					return ("may not be a relative path", i);
				case var @char when IsNonNullInvalidPathChar(@char):
					return ("invalid character", i);
			}
		}

		return null;
	}

	private static bool IsNonNullInvalidPathChar(char @char) =>
		@char > '\u0000' && @char <= '\u001f'
		|| @char >= '\u007f' && @char <= '\u009F'
		|| @char >= '\ud800' && @char <= '\uf8ff'
		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
		|| @char >= '\ufff0' && @char <= '\uffff';
}