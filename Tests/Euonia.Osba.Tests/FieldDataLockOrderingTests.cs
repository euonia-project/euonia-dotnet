using Xunit;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// O-5 护栏：<see cref="FieldDataManager.ForceStaticFieldInit(Type)"/> <b>不得</b>再对目标类型加锁。
/// </summary>
/// <remarks>
/// 该方法曾在 <c>PropertyInfoManager</c> 持有 <c>_publishLock</c> 时被调用（A→B），
/// 而它一旦触发类型的静态初始化器，初始化器又会重入 <c>GetPropertyListCache</c> 去要 <c>_publishLock</c>（B→A）——
/// 再加上 <c>lock(type)</c> 就是可证明的 ABBA 锁序反转。CLR 的类型初始化锁本已保证静态初始化只跑一次，
/// 这把锁是纯冗余的。
/// </remarks>
public class FieldDataLockOrderingTests
{
	[Fact]
	public void ForceStaticFieldInit_Should_Not_Block_On_The_Type_Monitor()
	{
		var type = typeof(LockProbe);

		var worker = new Thread(() => FieldDataManager.ForceStaticFieldInit(type))
		{
			IsBackground = true,
			Name = "o5-force-static-field-init"
		};

		lock (type)
		{
			worker.Start();

			// 旧实现会在这里阻塞：worker 要等本线程释放 type 的 monitor 才能进临界区。
			// 删掉 lock(type) 后它应当立即完成。
			var finished = worker.Join(TimeSpan.FromSeconds(3));

			Assert.True(
				finished,
				$"{nameof(FieldDataManager)}.{nameof(FieldDataManager.ForceStaticFieldInit)} 不应再对 type 加锁" +
				"（O-5：与 PropertyInfoManager 的 _publishLock 构成 ABBA 锁序反转）");
		}

		worker.Join(TimeSpan.FromSeconds(5));
	}

	/// <summary>
	/// 提供一个带静态字段的探针类型，使 <see cref="FieldDataManager.ForceStaticFieldInit(Type)"/> 有事可做。
	/// </summary>
	private sealed class LockProbe
	{
		// ReSharper disable once UnusedMember.Local
		public static readonly int Marker = 42;
	}
}
