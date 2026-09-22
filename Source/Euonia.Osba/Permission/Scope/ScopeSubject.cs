namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示一个数据权限主体：用户在某个维度上被授予的一个值。
/// </summary>
/// <param name="Dimension">维度名，例如 <see cref="ScopeDimensions.Dept"/>。</param>
/// <param name="Value">该维度上的不透明标识，通常是数据库标识；框架不做格式假设。</param>
public readonly record struct ScopeSubject(string Dimension, string Value);
