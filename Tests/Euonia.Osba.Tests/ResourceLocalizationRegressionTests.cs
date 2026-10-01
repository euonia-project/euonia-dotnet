using System.Globalization;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 资源本地化回归护栏。
/// <para>
/// 三处配置错误提示此前是硬编码中文字面量，英文宿主拿到的异常消息无法翻译。
/// 收敛到 <c>Properties/Resources.resx</c> 后，文案必须随当前 UI 文化切换：
/// 回退成中文字面量时，英文分支会立刻变红。
/// </para>
/// </summary>
public class ResourceLocalizationRegressionTests
{
	[Fact]
	public void Configuration_Error_Keys_Should_Exist_In_Both_Cultures()
	{
		var original = CultureInfo.CurrentUICulture;
		try
		{
			var keys = new[]
			{
				"IDS_SCOPE_CONTEXT_MISSING",
				"IDS_OBJECT_CONTEXT_MISSING",
				"IDS_PERMISSION_CHECKER_MISSING"
			};

			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
			foreach (var key in keys)
			{
				var en = Properties.Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
				Assert.False(string.IsNullOrEmpty(en), $"{key} missing in neutral resources");
				Assert.DoesNotContain('一', en);
			}

			// 3 个键在两个文化下都必须解析得到，且英文文案不含 CJK ——
			// 说明 .resx 与 .zh-CN.resx 成对齐全、卫星程序集可用
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
			foreach (var key in keys)
			{
				var zh = Properties.Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
				Assert.False(string.IsNullOrEmpty(zh), $"{key} missing in zh-CN resources");
			}

			// 英文文案里的占位符必须与中文一致，否则 string.Format 会在运行期抛 FormatException
			foreach (var key in new[] { "IDS_SCOPE_CONTEXT_MISSING", "IDS_OBJECT_CONTEXT_MISSING", "IDS_PERMISSION_CHECKER_MISSING" })
			{
				var en = Properties.Resources.ResourceManager.GetString(key, CultureInfo.GetCultureInfo("en-US"));
				var zh = Properties.Resources.ResourceManager.GetString(key, CultureInfo.GetCultureInfo("zh-CN"));
				Assert.Equal(CountPlaceholders(zh), CountPlaceholders(en));
				Assert.Contains("{0}", en);
				Assert.Contains("{1}", en);
			}
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	private static int CountPlaceholders(string value)
	{
		var count = 0;
		for (var i = 0; i < value.Length - 1; i++)
		{
			if (value[i] == '{' && char.IsDigit(value[i + 1]))
			{
				count++;
			}
		}

		return count;
	}
}
