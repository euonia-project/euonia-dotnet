using static Nerosoft.Euonia.Caching.Internal.BackplaneAction;

namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 实现一个可发送到服务器的简单消息。
/// </summary>
public sealed class BackplaneMessage
{
    private BackplaneMessage(byte[] owner, BackplaneAction action)
    {
        Check.EnsureNotNull(owner, nameof(owner));

        OwnerIdentity = owner;
        Action = action;
    }

    private BackplaneMessage(byte[] owner, BackplaneAction action, string key)
        : this(owner, action)
    {
        Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

        Key = key;
    }

    private BackplaneMessage(byte[] owner, BackplaneAction action, string key, string region)
        : this(owner, action, key)
    {
        Check.EnsureNotNullOrWhiteSpace(region, nameof(region));

        Region = region;
    }

    private BackplaneMessage(byte[] owner, BackplaneAction action, string key, CacheItemChangedEventAction changeAction)
        : this(owner, action, key)
    {
        ChangeAction = changeAction;
    }

    private BackplaneMessage(byte[] owner, BackplaneAction action, string key, string region, CacheItemChangedEventAction changeAction)
        : this(owner, action, key, region)
    {
        ChangeAction = changeAction;
    }

    /// <summary>
    /// 获取或设置操作。
    /// </summary>
    /// <value>操作。</value>
    public BackplaneAction Action { get; }

    /// <summary>
    /// 获取或设置键。
    /// </summary>
    /// <value>键。</value>
    public string Key { get; }

    /// <summary>
    /// 获取或设置所有者标识。
    /// </summary>
    /// <value>所有者标识。</value>
    public byte[] OwnerIdentity { get; }

    /// <summary>
    /// 获取或设置区域。
    /// </summary>
    /// <value>区域。</value>
    public string Region { get; private set; }

    /// <summary>
    /// 获取或设置缓存操作。
    /// </summary>
    public CacheItemChangedEventAction ChangeAction { get; }

    /// <inheritdoc />
    public override string ToString()
    {
		switch (Action)
		{
			case Changed:
				return $"{Action} {Region}:{Key} {ChangeAction}";

			case Removed:
				return $"{Action} {Region}:{Key}";

			case ClearRegion:
				return $"{Action} {Region}";

			case Clear:
				return $"{Action}";
			case Invalid:
				break;
		}

		return string.Empty;
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        if (obj is not BackplaneMessage objCast)
        {
            return false;
        }

        return Action == objCast.Action
            && Key == objCast.Key
            && ChangeAction == objCast.ChangeAction
            && Region == objCast.Region;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;

            hash = hash * 23 + Action.GetHashCode();
            hash = hash * 23 + ChangeAction.GetHashCode();
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            hash = hash * 23 + (Region?.GetHashCode() ?? 17);
            hash = hash * 23 + (Key?.GetHashCode() ?? 17);
            return hash;
        }
    }

    /// <summary>
    /// 为更改操作创建新的 <see cref="BackplaneMessage"/>。
    /// </summary>
    /// <param name="owner">所有者。</param>
    /// <param name="key">键。</param>
    /// <param name="changeAction">缓存更改操作。</param>
    /// <returns>新的 <see cref="BackplaneMessage"/> 实例。</returns>
    public static BackplaneMessage ForChanged(byte[] owner, string key, CacheItemChangedEventAction changeAction) =>
        new(owner, Changed, key, changeAction);

    /// <summary>
    /// 为更改操作创建新的 <see cref="BackplaneMessage"/>。
    /// </summary>
    /// <param name="owner">所有者。</param>
    /// <param name="key">键。</param>
    /// <param name="region">区域。</param>
    /// <param name="changeAction">缓存更改操作。</param>
    /// <returns>新的 <see cref="BackplaneMessage"/> 实例。</returns>
    public static BackplaneMessage ForChanged(byte[] owner, string key, string region, CacheItemChangedEventAction changeAction) =>
        new(owner, Changed, key, region, changeAction);

    /// <summary>
    /// 为清空操作创建新的 <see cref="BackplaneMessage"/>。
    /// </summary>
    /// <param name="owner">所有者。</param>
    /// <returns>新的 <see cref="BackplaneMessage"/> 实例。</returns>
    public static BackplaneMessage ForClear(byte[] owner) =>
        new(owner, Clear);

    /// <summary>
    /// 为清空区域操作创建新的 <see cref="BackplaneMessage"/>。
    /// </summary>
    /// <param name="owner">所有者。</param>
    /// <param name="region">区域。</param>
    /// <returns>新的 <see cref="BackplaneMessage"/> 实例。</returns>
    /// <exception cref="ArgumentNullException">当 <c>region</c> 为 <c>null</c> 时抛出。</exception>
    public static BackplaneMessage ForClearRegion(byte[] owner, string region)
    {
        Check.EnsureNotNullOrWhiteSpace(region, nameof(region));

        return new BackplaneMessage(owner, ClearRegion)
        {
            Region = region
        };
    }

    /// <summary>
    /// 为移除操作创建新的 <see cref="BackplaneMessage"/>。
    /// </summary>
    /// <param name="owner">所有者。</param>
    /// <param name="key">键。</param>
    /// <returns>新的 <see cref="BackplaneMessage"/> 实例。</returns>
    public static BackplaneMessage ForRemoved(byte[] owner, string key) =>
        new(owner, Removed, key);

    /// <summary>
    /// 为移除操作创建新的 <see cref="BackplaneMessage"/>。
    /// </summary>
    /// <param name="owner">所有者。</param>
    /// <param name="key">键。</param>
    /// <param name="region">区域。</param>
    /// <returns>新的 <see cref="BackplaneMessage"/> 实例。</returns>
    public static BackplaneMessage ForRemoved(byte[] owner, string key, string region) =>
        new(owner, Removed, key, region);

    /// <summary>
    /// 序列化此实例。
    /// </summary>
    /// <returns>表示此消息的字符串。</returns>
    public static byte[] Serialize(params BackplaneMessage[] messages)
    {
        Check.EnsureNotNullOrEmpty(messages, nameof(messages));

        // 计算大小
        var size = 0;
        for (var i = 0; i < messages.Length; i++)
        {
            size += MessageWriter.GetEstimatedSize(messages[i], i != 0);
        }

        var writer = new MessageWriter(size);

        for (var i = 0; i < messages.Length; i++)
        {
            SerializeMessage(writer, messages[i], i != 0);
        }

        return writer.GetBytes();
    }

    private static void SerializeMessage(MessageWriter writer, BackplaneMessage message, bool skipOwner)
    {
        if (!skipOwner)
        {
            writer.WriteInt(message.OwnerIdentity.Length);
            writer.WriteBytes(message.OwnerIdentity);
        }

        writer.WriteByte((byte)message.Action);
        switch (message.Action)
        {
            case Changed:
                writer.WriteByte((byte)message.ChangeAction);
                if (!string.IsNullOrEmpty(message.Region))
                {
                    writer.WriteByte(2);
                    writer.WriteString(message.Region);
                }
                else
                {
                    writer.WriteByte(1);
                }
                writer.WriteString(message.Key);

                break;

            case Removed:
                if (!string.IsNullOrEmpty(message.Region))
                {
                    writer.WriteByte(2);
                    writer.WriteString(message.Region);
                }
                else
                {
                    writer.WriteByte(1);
                }
                writer.WriteString(message.Key);

                break;

            case ClearRegion:
                writer.WriteString(message.Region);
                break;

            case Clear:
                break;
        }
    }

    /// <summary>
    /// 反序列化 <paramref name="message"/>。
    /// </summary>
    /// <param name="message">消息。</param>
    /// <param name="skipOwner">如果指定，且接收到的第一条消息具有相同的所有者，则跳过所有消息。</param>
    /// <returns>
    /// 新的 <see cref="BackplaneMessage" /> 实例。
    /// </returns>
    /// <exception cref="ArgumentException">当 <paramref name="message"/> 为 <c>null</c> 时抛出。</exception>
    /// <exception cref="ArgumentException">当消息无效时抛出。</exception>
    public static IEnumerable<BackplaneMessage> Deserialize(byte[] message, byte[] skipOwner = null)
    {
        Check.EnsureNotNull(message, nameof(message));
        if (message.Length < 5)
        {
            throw new ArgumentException("Invalid message");
        }
        var reader = new MessageReader(message);

        var first = DeserializeMessage(reader, null);

        if (skipOwner != null)
        {
            if (first.OwnerIdentity.SequenceEqual(skipOwner))
            {
                yield break;
            }
        }

        yield return first;

        while (reader.HasMore())
        {
            yield return DeserializeMessage(reader, first.OwnerIdentity);
        }
    }

    private static BackplaneMessage DeserializeMessage(MessageReader reader, byte[] existingOwner)
    {
        var owner = existingOwner ?? reader.ReadBytes(reader.ReadInt());
        var action = (BackplaneAction)reader.ReadByte();

        switch (action)
        {
            case Changed:
                var changeAction = (CacheItemChangedEventAction)reader.ReadByte();
                if (reader.ReadByte() == 2)
                {
                    var r = reader.ReadString();
                    return ForChanged(owner, reader.ReadString(), r, changeAction);
                }

                return ForChanged(owner, reader.ReadString(), changeAction);

            case Removed:
                if (reader.ReadByte() == 2)
                {
                    var r = reader.ReadString();
                    return ForRemoved(owner, reader.ReadString(), r);
                }

                return ForRemoved(owner, reader.ReadString());

            case ClearRegion:
                return ForClearRegion(owner, reader.ReadString());

            case Clear:
                return ForClear(owner);

            default:
                throw new ArgumentException("Invalid message type");
        }
    }

    private class MessageWriter
    {
        private static readonly Encoding _encoding = Encoding.UTF8;
        private readonly byte[] _buffer;
        private int _position;

        public MessageWriter(int size)
        {
            _buffer = new byte[size + 4];
            _position = 4;

            // v2 头部
            _buffer[0] = 0;
            _buffer[1] = 118;
            _buffer[2] = 50;
            _buffer[3] = 0;
        }

        public byte[] GetBytes()
        {
            var result = new byte[_position];
            Buffer.BlockCopy(_buffer, 0, result, 0, _position);
            return result;
        }

        public void WriteInt(int number)
        {
            var bytes = BitConverter.GetBytes(number);
            WriteBytes(bytes);
        }

        public void WriteString(string value)
        {
            var len = _encoding.GetByteCount(value);
            WriteInt(len);

            _encoding.GetBytes(value, 0, value.Length, _buffer, _position);
            _position += len;
        }

        public void WriteBytes(byte[] bytes)
        {
            Buffer.BlockCopy(bytes, 0, _buffer, _position, bytes.Length);
            _position += bytes.Length;
        }

        public void WriteByte(byte b)
        {
            _buffer[_position] = b;
            _position++;
        }

        public static int GetEstimatedSize(BackplaneMessage msg, bool skipOwner)
        {
            // 这只是粗略大小，乘以 2 以获得大致合适的缓冲区大小
            int size = 2; // 两个枚举
            if (!skipOwner)
            {
                size += msg.OwnerIdentity.Length * 4;
            }

            size += msg.Key?.Length * 4 ?? 0;
            size += msg.Region?.Length * 4 ?? 0;
            return size * 2;
        }
    }

    private class MessageReader
    {
        private static readonly Encoding _encoding = Encoding.UTF8;
        private readonly byte[] _data;
        private int _position;

        public MessageReader(byte[] bytes)
        {
            _data = bytes;
            _position = 4;

            // 检查 v2 头部
            if (_data.Length < 4
             || _data[0] != 0 || _data[1] != 118 || _data[2] != 50 || _data[3] != 0)
            {
                throw new InvalidOperationException("Invalid v2 backplane message");
            }
        }

        public bool HasMore()
        {
            return _data.Length > _position;
        }

        public int ReadInt()
        {
            var pos = (_position += 4);
            if (pos > _data.Length)
            {
                throw new IndexOutOfRangeException("Cannot read INT32, no additional bytes available.");
            }

            return BitConverter.ToInt32(_data, pos - 4);
        }

        public byte ReadByte()
        {
            if (_position >= _data.Length)
            {
                throw new IndexOutOfRangeException("Cannot read byte, no additional bytes available.");
            }

            return _data[_position++];
        }

        public byte[] ReadBytes(int length)
        {
            var pos = (_position += length);
            if (pos > _data.Length)
            {
                throw new IndexOutOfRangeException("Cannot read bytes, no additional bytes available.");
            }

            // 修复：在分配前进行长度检查
            var result = new byte[length];
            Buffer.BlockCopy(_data, pos - length, result, 0, length);
            return result;
        }

        public string ReadString()
        {
            var len = ReadInt();
            if (len <= 0)
            {
                throw new IndexOutOfRangeException("Invalid length for string");
            }

            var pos = (_position += len);
            if (pos > _data.Length)
            {
                throw new IndexOutOfRangeException("Cannot read string, no additional bytes available.");
            }

            return _encoding.GetString(_data, pos - len, len);
        }
    }
}