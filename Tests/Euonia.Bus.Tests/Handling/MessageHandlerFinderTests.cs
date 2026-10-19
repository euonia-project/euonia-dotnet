using System.Reflection;
using System.Reflection.Emit;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对处理程序发现阶段异常诊断信息的测试。
/// </summary>
/// <remarks>
/// 这些异常在应用启动时抛出，消息必须包含处理程序类型、方法名与实际参数类型，
/// 否则使用者无法从日志定位到具体的处理器。
/// 无效处理器通过 <see cref="AssemblyBuilder"/> 动态生成，避免被同程序集的自动扫描测试拾取。
/// </remarks>
public class MessageHandlerFinderTests
{
	[Fact]
	public void Find_MethodWithoutParameters_MessageContainsHandlerAndMethod()
	{
		var handlerType = DefineHandler("NoParameterHandler", []);

		var exception = Assert.Throws<InvalidOperationException>(() => Find(handlerType));

		Assert.Contains(handlerType.FullName!, exception.Message, StringComparison.Ordinal);
		Assert.Contains("HandleAsync", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Find_FirstParameterIsContext_MessageContainsActualParameterTypes()
	{
		var handlerType = DefineHandler("ContextAsFirstParameterHandler", [typeof(IMessageContext)]);

		var exception = Assert.Throws<InvalidOperationException>(() => Find(handlerType));

		Assert.Contains(handlerType.FullName!, exception.Message, StringComparison.Ordinal);
		Assert.Contains("HandleAsync", exception.Message, StringComparison.Ordinal);
		Assert.Contains(typeof(IMessageContext).FullName!, exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Find_SecondParameterIsNotContextOrToken_MessageContainsActualParameterTypes()
	{
		var handlerType = DefineHandler("InvalidSecondParameterHandler", [typeof(string), typeof(string)]);

		var exception = Assert.Throws<InvalidOperationException>(() => Find(handlerType));

		Assert.Contains(handlerType.FullName!, exception.Message, StringComparison.Ordinal);
		Assert.Contains("HandleAsync", exception.Message, StringComparison.Ordinal);
		Assert.Contains(typeof(string).FullName!, exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Find_SubscribeAttributeWithoutName_MessageContainsHandlerAndMethod()
	{
		var handlerType = DefineHandler("MissingChannelNameHandler", [typeof(string)], channelName: null);

		var exception = Assert.Throws<InvalidOperationException>(() => Find(handlerType));

		Assert.Contains(handlerType.FullName!, exception.Message, StringComparison.Ordinal);
		Assert.Contains("HandleAsync", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void Find_ExplicitInterfaceImplementation_IsDiscovered()
	{
		var channels = new List<string>();

		MessageHandlerFinder.Find((channel, _, _) => channels.Add(channel), type => type.FullName!, typeof(ExplicitImplementationHandler));

		Assert.Contains(typeof(ExplicitImplementationMessage).FullName!, channels);
	}

	private static void Find(Type handlerType)
	{
		MessageHandlerFinder.Find((_, _, _) => { }, type => type.FullName!, handlerType);
	}

	private sealed class ExplicitImplementationMessage
	{
	}

	private sealed class ExplicitImplementationHandler : IHandler<ExplicitImplementationMessage, int>
	{
		Task<int> IHandler<ExplicitImplementationMessage, int>.HandleAsync(ExplicitImplementationMessage message, IMessageContext context, CancellationToken cancellationToken)
		{
			return Task.FromResult(1);
		}
	}

	/// <summary>
	/// 生成一个带 <see cref="SubscribeAttribute"/> 方法的公开类。
	/// </summary>
	/// <param name="typeName">动态类型名称。</param>
	/// <param name="parameterTypes">处理方法参数类型。</param>
	/// <param name="channelName">订阅的通道名称；为 <c>null</c> 时使用无参构造（名称留空）。</param>
	private static Type DefineHandler(string typeName, Type[] parameterTypes, string channelName = "test")
	{
		var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"DynamicHandlers.{typeName}.{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
		var module = assembly.DefineDynamicModule("DynamicHandlers");
		var type = module.DefineType($"DynamicHandlers.{typeName}", TypeAttributes.Public | TypeAttributes.Class);
		var method = type.DefineMethod("HandleAsync", MethodAttributes.Public, typeof(Task), parameterTypes);

		var constructor = channelName == null
			? typeof(SubscribeAttribute).GetConstructor(Type.EmptyTypes)!
			: typeof(SubscribeAttribute).GetConstructor([typeof(string)])!;
		var arguments = channelName == null ? Array.Empty<object>() : new object[] { channelName };
		method.SetCustomAttribute(new CustomAttributeBuilder(constructor, arguments));

		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldnull);
		il.Emit(OpCodes.Ret);

		return type.CreateType()!;
	}
}
