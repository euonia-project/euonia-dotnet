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
	/// <summary>
	/// 验证无参数的处理方法会抛出 <see cref="InvalidOperationException"/>，消息包含处理器类型名与方法名。
	/// </summary>
	[Fact]
	public void Find_MethodWithoutParameters_MessageContainsHandlerAndMethod()
	{
		var handlerType = DefineHandler("NoParameterHandler", []);

		var exception = Assert.Throws<InvalidOperationException>(() => Find(handlerType));

		Assert.Contains(handlerType.FullName!, exception.Message, StringComparison.Ordinal);
		Assert.Contains("HandleAsync", exception.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// 验证首个参数为消息上下文时抛出的异常包含实际参数类型，便于定位无效处理器。
	/// </summary>
	[Fact]
	public void Find_FirstParameterIsContext_MessageContainsActualParameterTypes()
	{
		var handlerType = DefineHandler("ContextAsFirstParameterHandler", [typeof(IMessageContext)]);

		var exception = Assert.Throws<InvalidOperationException>(() => Find(handlerType));

		Assert.Contains(handlerType.FullName!, exception.Message, StringComparison.Ordinal);
		Assert.Contains("HandleAsync", exception.Message, StringComparison.Ordinal);
		Assert.Contains(typeof(IMessageContext).FullName!, exception.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// 验证第二个参数既不是上下文也不是取消令牌时，异常消息包含实际参数类型。
	/// </summary>
	[Fact]
	public void Find_SecondParameterIsNotContextOrToken_MessageContainsActualParameterTypes()
	{
		var handlerType = DefineHandler("InvalidSecondParameterHandler", [typeof(string), typeof(string)]);

		var exception = Assert.Throws<InvalidOperationException>(() => Find(handlerType));

		Assert.Contains(handlerType.FullName!, exception.Message, StringComparison.Ordinal);
		Assert.Contains("HandleAsync", exception.Message, StringComparison.Ordinal);
		Assert.Contains(typeof(string).FullName!, exception.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// 验证订阅特性未指定通道名且消息类型为基础类型时抛出异常，消息包含处理器类型名与方法名。
	/// </summary>
	[Fact]
	public void Find_SubscribeAttributeWithoutName_MessageContainsHandlerAndMethod()
	{
		var handlerType = DefineHandler("MissingChannelNameHandler", [typeof(string)], channelName: null!);

		var exception = Assert.Throws<InvalidOperationException>(() => Find(handlerType));

		Assert.Contains(handlerType.FullName!, exception.Message, StringComparison.Ordinal);
		Assert.Contains("HandleAsync", exception.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// 验证单个未命名订阅特性会以消息类型全名推断通道，并登记正确的消息类型。
	/// </summary>
	[Fact]
	public void Find_SingleSubscribeAttributeWithoutName_InfersChannelFromMessageType()
	{
		var handlerType = DefineHandler("InferredChannelHandler", [typeof(InferredChannelMessage)], channelName: null!);
		var registrations = new List<(string Channel, Type MessageType)>();

		MessageHandlerFinder.Find((channel, messageType, _) => registrations.Add((channel, messageType)), type => type.FullName!, handlerType);

		var registration = Assert.Single(registrations);
		Assert.Equal(typeof(InferredChannelMessage).FullName, registration.Channel);
		Assert.Equal(typeof(InferredChannelMessage), registration.MessageType);
	}

	/// <summary>
	/// 验证单个未命名订阅特性会使用传入的通道解析器生成通道名。
	/// </summary>
	[Fact]
	public void Find_SingleSubscribeAttributeWithoutName_UsesChannelResolver()
	{
		var handlerType = DefineHandler("InferredCustomChannelHandler", [typeof(InferredChannelMessage)], channelName: null!);
		var channels = new List<string>();

		MessageHandlerFinder.Find((channel, _, _) => channels.Add(channel), _ => "custom-channel", handlerType);

		Assert.Equal(["custom-channel"], channels);
	}

	/// <summary>
	/// 验证 <see cref="SubscribeAttribute"/> 的用法声明不允许在同一成员上多次应用。
	/// </summary>
	[Fact]
	public void SubscribeAttribute_DoesNotAllowMultipleDeclarations()
	{
		var usage = typeof(SubscribeAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.NotNull(usage);
		Assert.False(usage.AllowMultiple);
	}

	/// <summary>
	/// 验证显式接口实现的处理方法同样能被处理器发现逻辑找到。
	/// </summary>
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

	/// <summary>
	/// 显式接口实现测试用消息，用于验证查找器能识别显式实现的处理器。
	/// </summary>
	private sealed class ExplicitImplementationMessage
	{
	}

	/// <summary>
	/// 通道名由消息类型推断的测试消息，用于验证未命名订阅特性使用类型全名作为通道。
	/// </summary>
	private sealed class InferredChannelMessage
	{
	}

	/// <summary>
	/// 显式实现 <see cref="IHandler{TMessage, TResponse}"/> 的处理器，用于验证显式接口实现同样可被发现。
	/// </summary>
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
