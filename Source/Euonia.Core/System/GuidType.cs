namespace System;

/// <summary>
/// 描述顺序 GUID 值的类型。
/// </summary>
public enum GuidType
{
    /// <summary>
    /// 空 GUID（全零）。
    /// </summary>
    Empty,

    /// <summary>
    /// 标准随机 GUID。
    /// </summary>
    Simple,

    /// <summary>
    /// GUID 在使用 <see cref="Guid.ToString()" /> 方法格式化时应该是顺序的。
    /// 用于 MySql 和 PostgreSql。
    /// </summary>
    SequentialAsString,

    /// <summary>
    /// GUID 在使用 <see cref="Guid.ToByteArray()" /> 方法格式化时应该是顺序的。
    /// 用于 Oracle。
    /// </summary>
    SequentialAsBinary,

    /// <summary>
    /// GUID 的顺序部分应位于 Data4 块的末尾。
    /// 用于 SqlServer。
    /// </summary>
    SequentialAtEnd
}