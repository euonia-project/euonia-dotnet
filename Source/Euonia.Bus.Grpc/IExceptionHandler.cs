using Grpc.Core;

namespace Nerosoft.Euonia.Grpc;

/// <summary>
/// 定义将应用程序异常转换为 gRPC <see cref="RpcException"/> 的处理器契约。
/// </summary>
public interface IExceptionHandler
{
    /// <summary>
    /// 处理原始异常并生成新的 <see cref="RpcException"/>。
    /// </summary>
    /// <param name="exception">原始异常。</param>
    /// <returns>用于返回给调用方的 <see cref="RpcException"/>。</returns>
    RpcException Handle(Exception exception);
}
