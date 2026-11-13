namespace Nerosoft.Euonia.Mapping.Tests.Models
{
	/// <summary>
	/// 映射测试使用的源对象，提供名与姓用于验证值解析器映射。
	/// </summary>
	public class SourceObject3
	{
		/// <summary>
		/// 获取或设置名。
		/// </summary>
		public string FirstName { get; set; } = null!;

		/// <summary>
		/// 获取或设置姓。
		/// </summary>
		public string LastName { get; set; } = null!;
	}
}