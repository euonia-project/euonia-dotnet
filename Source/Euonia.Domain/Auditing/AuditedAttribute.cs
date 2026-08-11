namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 表示被修饰的类、方法或属性应被审计。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
public class AuditedAttribute : Attribute
{
}