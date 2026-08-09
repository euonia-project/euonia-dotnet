using System.Text.RegularExpressions;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Concurrency.Redis.Internal;

internal class RedisScript<TArgument>
{
    private readonly LuaScript _script;
    private readonly Func<TArgument, object> _parameters;

    public RedisScript(string script, Func<TArgument, object> parameters)
    {
        _script = LuaScript.Prepare(RemoveExtraneousWhitespace(script));
        _parameters = parameters;
    }

    public RedisResult Execute(IDatabase database, TArgument argument, bool fireAndForget = false) =>
        // database.ScriptEvaluate must be called instead of _script.Evaluate in order to respect the database's key prefix
        database.ScriptEvaluate(_script, _parameters(argument), flags: RedisLockHelper.GetCommandFlags(fireAndForget));

    public Task<RedisResult> ExecuteAsync(IDatabaseAsync database, TArgument argument, bool fireAndForget = false) =>
        // database.ScriptEvaluate must be called instead of _script.Evaluate in order to respect the database's key prefix
        database.ScriptEvaluateAsync(_script, _parameters(argument), flags: RedisLockHelper.GetCommandFlags(fireAndForget));

    // send the smallest possible script to the server
    private static string RemoveExtraneousWhitespace(string script) => Regex.Replace(script.Trim(), @"\s+", " ");
}