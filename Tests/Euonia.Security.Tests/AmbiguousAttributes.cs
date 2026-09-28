// 同一个文件里用两段块级命名空间：这两个特性刻意<b>同短名、不同命名空间</b>，
// 用于验证「配置里的类型名短名有歧义」必须在注册期报错。除此以外它们不参与任何测试。
namespace Nerosoft.Euonia.Security.Tests.Ambiguity.One
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class SharedEntryAttribute : Attribute;
}

namespace Nerosoft.Euonia.Security.Tests.Ambiguity.Two
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class SharedEntryAttribute : Attribute;
}
