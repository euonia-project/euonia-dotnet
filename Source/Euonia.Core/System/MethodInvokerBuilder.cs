#nullable enable
using System.Linq.Expressions;
using System.Reflection;

namespace System;

/// <summary>
/// 构建一个可以动态调用方法的方法调用器。
/// </summary>
public static class MethodInvokerBuilder
{
    /// <summary>
    /// 为指定的方法构建方法调用器。
    /// </summary>
    /// <param name="method">要为其构建调用器的方法。</param>
    /// <returns>返回一个可用于动态调用方法的委托。</returns>
    public static Func<object, object?[], Task<object?>> Build(MethodInfo method)
    {
        var targetExp = Expression.Parameter(typeof(object), "target");
        var argsExp = Expression.Parameter(typeof(object[]), "args");

        var parameters = method.GetParameters();
        var argExps = new Expression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            argExps[i] = Expression.Convert(Expression.ArrayIndex(argsExp, Expression.Constant(i)), parameters[i].ParameterType);
        }

        Expression? instanceExp = method.IsStatic ? null : Expression.Convert(targetExp, method.DeclaringType!);

        var callExp = Expression.Call(instanceExp, method, argExps);

        var body = WrapToTaskObject(callExp, method.ReturnType);

        return Expression.Lambda<Func<object, object?[], Task<object?>>>(body, targetExp, argsExp).Compile();
    }

    /// <summary>
    /// 为指定的目标和方向构建方法调用器。
    /// </summary>
    /// <param name="target">调用方法的目标对象。</param>
    /// <param name="method">要调用的方法。</param>
    /// <param name="arguments">传递给方法的参数表达式。</param>
    /// <returns>返回一个可用于异步调用方法的委托。</returns>
    public static Func<Task<object?>> Build(object target, MethodInfo method, params Expression[] arguments)
    {
        var expression = Expression.Call(Expression.Constant(target), method, arguments);

        var body = WrapToTaskObject(expression, method.ReturnType);

        return Expression.Lambda<Func<Task<object?>>>(body).Compile();
    }

    /// <summary>
    /// 为指定的目标和方向构建调用表达式。
    /// </summary>
    /// <param name="target">调用方法的目标对象。</param>
    /// <param name="method">要调用的方法。</param>
    /// <param name="arguments">传递给方法的参数表达式。</param>
    /// <returns>返回一个表示调用的表达式。</returns>
    public static Expression BuildCallExpression(object target, MethodInfo method, params Expression[] arguments)
    {
        var expression = Expression.Call(Expression.Constant(target), method, arguments);

        var body = WrapToTaskObject(expression, method.ReturnType);
        return body;
    }

    /// <summary>
    /// 将调用表达式包装为返回 Task&lt;object&gt; 的形式。
    /// </summary>
    /// <param name="callExp">要包装的调用表达式。</param>
    /// <param name="returnType">方法的返回类型。</param>
    /// <returns>包装后的表达式。</returns>
    public static Expression WrapToTaskObject(Expression callExp, Type returnType)
    {
        // ---------- void ----------
        if (returnType == typeof(void))
        {
            return Expression.Block(callExp, Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(object)], Expression.Constant(Unit.Value, typeof(object))));
        }

        // ---------- Task ----------
        if (returnType == typeof(Task))
        {
            return Expression.Call(typeof(MethodInvokerBuilder), nameof(AwaitTask), null, callExp);
        }

        // ---------- Task<T> ----------
        if (IsGeneric(returnType, typeof(Task<>)))
        {
            var genericReturnType = returnType.GetGenericArguments()[0];
            return Expression.Call(typeof(MethodInvokerBuilder), nameof(AwaitTaskGeneric), [genericReturnType], callExp);
        }

        // ---------- ValueTask ----------
        if (returnType == typeof(ValueTask))
        {
            return Expression.Call(typeof(MethodInvokerBuilder), nameof(AwaitValueTask), null, callExp);
        }

        // ---------- ValueTask<T> ----------
        if (IsGeneric(returnType, typeof(ValueTask<>)))
        {
            var genericReturnType = returnType.GetGenericArguments()[0];
            return Expression.Call(typeof(MethodInvokerBuilder), nameof(AwaitValueTaskGeneric), [genericReturnType], callExp);
        }

        {
        }

        // ---------- 返回 T ----------
        return Expression.Call(typeof(Task), nameof(Task.FromResult), [typeof(object)], Expression.Convert(callExp, typeof(object)));
    }

    private static bool IsGeneric(Type type, Type openGeneric) => type.IsGenericType && type.GetGenericTypeDefinition() == openGeneric;

    private static async Task<object?> AwaitTask(Task task)
    {
        await task.ConfigureAwait(false);
        return Unit.Value;
    }

    private static async Task<object?> AwaitTaskGeneric<T>(Task<T> task)
    {
        return await task.ConfigureAwait(false);
    }

    private static async Task<object?> AwaitValueTask(ValueTask valueTask)
    {
        await valueTask.ConfigureAwait(false);
        return Unit.Value;
    }

    private static async Task<object?> AwaitValueTaskGeneric<T>(ValueTask<T> valueTask)
    {
        return await valueTask.ConfigureAwait(false);
    }
}
