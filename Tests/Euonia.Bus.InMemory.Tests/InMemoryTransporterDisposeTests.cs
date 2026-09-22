using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus.InMemory;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="InMemoryTransporter.Dispose"/> 的回归测试。
/// </summary>
/// <remarks>
/// 该实现此前会在释放时调用 <c>StrongReferenceMessenger.Default.Reset()</c> 与
/// <c>WeakReferenceMessenger.Default.Reset()</c>。这两个信使是进程级单例，
/// 因此释放任意一个传输器实例都会静默清空进程内所有内存总线的注册
/// （例如测试宿主、第二个 DI 容器、或应用内重启）。
/// </remarks>
public class InMemoryTransporterDisposeTests
{
	[Fact]
	public void Dispose_DoesNotClearProcessWideMessengerRegistrations()
	{
		var recipient = new RecordingRecipient();
		const string token = "dispose-isolation-channel";

		StrongReferenceMessenger.Default.Register<RecordingRecipient, ProbeMessage, string>(recipient, token, (state, _) => state.Count++);

		try
		{
			Assert.True(StrongReferenceMessenger.Default.IsRegistered<ProbeMessage, string>(recipient, token));

			var transporter = new InMemoryTransporter(Options.Create(new InMemoryBusOptions()), NullLoggerFactory.Instance);
			transporter.Dispose();

			// 释放一个传输器不应影响共享信使中其他实例的注册。
			Assert.True(StrongReferenceMessenger.Default.IsRegistered<ProbeMessage, string>(recipient, token),
			            "disposing an in-memory transporter must not clear process-wide messenger registrations");
		}
		finally
		{
			StrongReferenceMessenger.Default.UnregisterAll(recipient);
		}
	}

	/// <summary>
	/// 用于探测信使注册是否仍然存在的探针消息。
	/// </summary>
	private sealed class ProbeMessage
	{
	}

	private sealed class RecordingRecipient
	{
		public int Count { get; set; }
	}
}
