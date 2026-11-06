using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 站点根路径端点：开发环境跳转到 Swagger 界面，其他环境返回站点版权信息。
/// </summary>
[Route("{controller=Home}/{action=Index}")]
[ApiExplorerSettings(IgnoreApi = true)]
[AllowAnonymous]
public class HomeController : Controller
{
    // ReSharper disable once RouteTemplates.MethodMissingRouteParameters
    /// <summary>
    /// 返回站点入口内容：开发环境重定向到 Swagger，其他环境输出版权文本。
    /// </summary>
    /// <returns>重定向结果或文本内容结果。</returns>
    public IActionResult Index()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return environment switch
        {
            "Development" => Redirect("/swagger"),
            _ => Content($"@ {DateTime.Today.Year} Nerosoft.")
        };
    }
}
