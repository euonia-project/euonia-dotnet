using Nerosoft.Euonia.Mapping.Tests.Models;

namespace Nerosoft.Euonia.Mapping.Tests
{
	/// <summary>
	/// 验证自定义值解析器拼接全名映射行为的测试。
	/// </summary>
	public class Test3
	{
		/// <summary>
		/// 值解析器应将源对象的名与姓拼接为目标的完整姓名。
		/// </summary>
		[Fact]
		public void Test()
		{
			var source = new SourceObject3
			{
				FirstName = "FirstName",
				LastName = "LastName"
			};

			var destination = TypeAdapter.ProjectedAs<DestinationObject3>(source);
			Assert.Equal($"{source.FirstName} {source.LastName}", destination.FullName);
		}
	}
}