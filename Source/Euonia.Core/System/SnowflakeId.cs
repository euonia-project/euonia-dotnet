namespace System;

internal class SnowflakeId
{
    private static long _machineId; //机器ID
    private static long _datacenterId; //数据ID
    private static long _sequence; //计数从零开始

    // ReSharper disable once IdentifierTypo
    private const long TWEPOCH = 687888001020L; //唯一时间随机量

    private const long MACHINE_ID_BITS = 5L; //机器码字节数
    private const long DATACENTER_ID_BITS = 5L; //数据字节数
    private const long MAX_MACHINE_ID = -1L ^ -1L << (int)MACHINE_ID_BITS; //最大机器ID
    private const long MAX_DATACENTER_ID = -1L ^ (-1L << (int)DATACENTER_ID_BITS); //最大数据ID

    private const long SEQUENCE_BITS = 12L; //计数器字节数，12个字节用来保存计数码
    private const long MACHINE_ID_SHIFT = SEQUENCE_BITS; //机器码数据左移位数，就是后面计数器占用的位数
    private const long DATACENTER_ID_SHIFT = SEQUENCE_BITS + MACHINE_ID_BITS;
    private const long TIMESTAMP_LEFT_SHIFT = SEQUENCE_BITS + MACHINE_ID_BITS + DATACENTER_ID_BITS; //时间戳左移动位数就是机器码+计数器总字节数+数据字节数
    private const long SEQUENCE_MASK = -1L ^ -1L << (int)SEQUENCE_BITS; //一微秒内可以产生计数，如果达到该值则等到下一微妙在进行生成
    private static long _lastTimestamp = -1L; //最后时间戳

    private static readonly object _lockObject = new(); //加锁对象

    private static readonly Lazy<SnowflakeId> _instance = new();

    /// <summary>
    /// 获取 <see cref="SnowflakeId"/> 的默认实例。
    /// </summary>
    /// <returns><see cref="SnowflakeId"/> 实例。</returns>
    public static SnowflakeId GetInstance()
    {
        return _instance.Value;
    }

    /// <summary>
    /// 获取 <see cref="SnowflakeId"/> 的单例实例。
    /// </summary>
    public static SnowflakeId Instance => _instance.Value;

    /// <summary>
    /// 初始化 <see cref="SnowflakeId"/> 类的新实例。
    /// </summary>
    public SnowflakeId()
    {
        Snowflakes(0L, -1);
    }

    /// <summary>
    /// 使用指定的机器 ID 初始化 <see cref="SnowflakeId"/> 类的新实例。
    /// </summary>
    /// <param name="machineId">机器 ID。</param>
    public SnowflakeId(long machineId)
    {
        Snowflakes(machineId, -1);
    }

    /// <summary>
    /// 使用指定的机器 ID 和数据中心 ID 初始化 <see cref="SnowflakeId"/> 类的新实例。
    /// </summary>
    /// <param name="machineId">机器 ID。</param>
    /// <param name="datacenterId">数据中心 ID。</param>
    public SnowflakeId(long machineId, long datacenterId)
    {
        Snowflakes(machineId, datacenterId);
    }

    private static void Snowflakes(long machineId, long datacenterId)
    {
        if (machineId >= 0)
        {
            if (machineId > MAX_MACHINE_ID)
            {
                throw new Exception("机器码ID非法");
            }

            _machineId = machineId;
        }

        if (datacenterId >= 0)
        {
            if (datacenterId > MAX_DATACENTER_ID)
            {
                throw new Exception("数据中心ID非法");
            }

            _datacenterId = datacenterId;
        }
    }

    /// <summary>
    /// 生成当前时间戳
    /// </summary>
    /// <returns>毫秒</returns>
    private static long GetTimestamp()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }

    /// <summary>
    /// 获取下一微秒时间戳
    /// </summary>
    /// <param name="lastTimestamp">上一个时间戳。</param>
    /// <returns>下一个时间戳（毫秒）。</returns>
    private static long GetNextTimestamp(long lastTimestamp)
    {
        var timestamp = GetTimestamp();
        if (timestamp <= lastTimestamp)
        {
            timestamp = GetTimestamp();
        }

        return timestamp;
    }

    /// <summary>
    /// 获取长整形的ID
    /// </summary>
    /// <returns>生成的雪花 ID。</returns>
    public long Next()
    {
        lock (_lockObject)
        {
            var timestamp = GetTimestamp();
            if (_lastTimestamp == timestamp)
            {
                //同一微妙中生成ID
                _sequence = (_sequence + 1) & SEQUENCE_MASK; //用&运算计算该微秒内产生的计数是否已经到达上限
                if (_sequence == 0)
                {
                    //一微妙内产生的ID计数已达上限，等待下一微妙
                    timestamp = GetNextTimestamp(_lastTimestamp);
                }
            }
            else
            {
                //不同微秒生成ID
                _sequence = 0L;
            }

            if (timestamp < _lastTimestamp)
            {
                throw new Exception("时间戳比上一次生成ID时时间戳还小，故异常");
            }

            _lastTimestamp = timestamp; //把当前时间戳保存为最后生成ID的时间戳
            long id = ((timestamp - TWEPOCH) << (int)TIMESTAMP_LEFT_SHIFT)
                      | (_datacenterId << (int)DATACENTER_ID_SHIFT)
                      | (_machineId << (int)MACHINE_ID_SHIFT)
                      | _sequence;
            return id;
        }
    }
}
