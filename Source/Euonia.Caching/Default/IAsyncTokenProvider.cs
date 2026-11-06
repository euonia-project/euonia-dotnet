namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 定义在异步任务完成后获取易变令牌的契约。
/// </summary>
public interface IAsyncTokenProvider
{
    /// <summary>
    /// 获取令牌。
    /// </summary>
    /// <param name="task">任务。</param>
    /// <returns>IVolatileToken。</returns>
    IVolatileToken GetToken(Action<Action<IVolatileToken>> task);
}