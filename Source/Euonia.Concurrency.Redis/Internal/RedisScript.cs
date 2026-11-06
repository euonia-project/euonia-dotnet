using System.Text.RegularExpressions;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

/// <summary>
/// 封装在 Redis 上执行的 Lua 脚本及其参数映射，并统一附加锁操作所需的命令标志。
/// </summary>
/// <typeparam name="TArgument">用于生成脚本参数的对象类型。</typeparam>
internal class RedisScript<TArgument>
{
    private readonly LuaScript _script;
    private readonly Func<TArgument, object> _parameters;

    /// <summary>
    /// 使用脚本正文与参数映射初始化 <see cref="RedisScript{TArgument}"/>，并压缩脚本中的多余空白。
    /// </summary>
    /// <param name="script">Lua 脚本正文，可包含 <c>@name</c> 形式的参数占位符。</param>
    /// <param name="parameters">根据参数对象生成脚本参数集合的委托。</param>
    public RedisScript(string script, Func<TArgument, object> parameters)
    {
        _script = LuaScript.Prepare(RemoveExtraneousWhitespace(script));
        _parameters = parameters;
    }

    /// <summary>
    /// 在指定数据库上同步执行脚本。
    /// </summary>
    /// <param name="database">执行脚本的 Redis 数据库。</param>
    /// <param name="argument">用于生成脚本参数的对象。</param>
    /// <param name="fireAndForget">是否以不等待结果的方式发送命令。</param>
    /// <returns>脚本的执行结果。</returns>
    public RedisResult Execute(IDatabase database, TArgument argument, bool fireAndForget = false) =>
        // 必须调用 database.ScriptEvaluate 而不是 _script.Evaluate，以便遵循数据库的键前缀
        database.ScriptEvaluate(_script, _parameters(argument), flags: RedisLockHelper.GetCommandFlags(fireAndForget));

    /// <summary>
    /// 在指定数据库上异步执行脚本。
    /// </summary>
    /// <param name="database">执行脚本的 Redis 数据库。</param>
    /// <param name="argument">用于生成脚本参数的对象。</param>
    /// <param name="fireAndForget">是否以不等待结果的方式发送命令。</param>
    /// <returns>表示脚本执行结果的任务。</returns>
    public Task<RedisResult> ExecuteAsync(IDatabaseAsync database, TArgument argument, bool fireAndForget = false) =>
        // 必须调用 database.ScriptEvaluate 而不是 _script.Evaluate，以便遵循数据库的键前缀
        database.ScriptEvaluateAsync(_script, _parameters(argument), flags: RedisLockHelper.GetCommandFlags(fireAndForget));

    // 向服务器发送尽可能小的脚本
    private static string RemoveExtraneousWhitespace(string script) => Regex.Replace(script.Trim(), @"\s+", " ");
}